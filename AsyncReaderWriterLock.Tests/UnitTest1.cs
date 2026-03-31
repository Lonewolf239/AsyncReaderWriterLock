using AsyncLocks;

namespace AsyncReaderWriterLockTests
{
    public class AsyncReaderWriterLockTests
    {
        [Fact]
        public void ReadLock_AcquiredWhenFree()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.ReadLock()) { }
        }

        [Fact]
        public void WriteLock_AcquiredWhenFree()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.WriteLock()) { }
        }

        [Fact]
        public void ReadLock_Recursive_Allowed()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.ReadLock())
            using (rwLock.ReadLock()) { }
        }

        [Fact]
        public async Task WriteLock_Recursive_Deadlocks()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.WriteLock())
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                var ex = await Record.ExceptionAsync(async () =>
                {
                    await rwLock.WriteLockAsync(cts.Token).AsTask();
                });
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Fact]
        public async Task WriteLockThenReadLock_Deadlocks()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.WriteLock())
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                var ex = await Record.ExceptionAsync(async () =>
                {
                    await rwLock.ReadLockAsync(cts.Token).AsTask();
                });
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Fact]
        public async Task ReadLockThenWriteLock_Deadlocks()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.ReadLock())
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                var ex = await Record.ExceptionAsync(async () =>
                {
                    await rwLock.WriteLockAsync(cts.Token).AsTask();
                });
                Assert.IsType<OperationCanceledException>(ex);
            }
        }

        [Fact]
        public async Task WriteLock_BlocksWhenActiveReader()
        {
            var rwLock = new AsyncReaderWriterLock();
            var readerStarted = new TaskCompletionSource<bool>();
            var readerReleased = new TaskCompletionSource<bool>();

            var readerTask = Task.Run(async () =>
            {
                using (rwLock.ReadLock())
                {
                    readerStarted.SetResult(true);
                    await readerReleased.Task;
                }
            });

            await readerStarted.Task;
            var writeTask = Task.Run(() => { using (rwLock.WriteLock()) { } });
            var completedTask = await Task.WhenAny(writeTask, Task.Delay(100));
            Assert.False(completedTask == writeTask);
            readerReleased.SetResult(true);
            await readerTask;
            await writeTask;
        }

        [Fact]
        public async Task WriteLock_BlocksWhenActiveWriter()
        {
            var rwLock = new AsyncReaderWriterLock();
            var writerStarted = new TaskCompletionSource<bool>();
            var writerReleased = new TaskCompletionSource<bool>();

            var writer1 = Task.Run(async () =>
            {
                using (rwLock.WriteLock())
                {
                    writerStarted.SetResult(true);
                    await writerReleased.Task;
                }
            });

            await writerStarted.Task;
            var writer2 = Task.Run(() => { using (rwLock.WriteLock()) { } });
            var completedTask = await Task.WhenAny(writer2, Task.Delay(100));
            Assert.False(completedTask == writer2);
            writerReleased.SetResult(true);
            await writer1;
            await writer2;
        }

        [Fact]
        public async Task ReadLock_BlocksWhenActiveWriter()
        {
            var rwLock = new AsyncReaderWriterLock();
            var writerStarted = new TaskCompletionSource<bool>();
            var writerReleased = new TaskCompletionSource<bool>();

            var writerTask = Task.Run(async () =>
            {
                using (rwLock.WriteLock())
                {
                    writerStarted.SetResult(true);
                    await writerReleased.Task;
                }
            });

            await writerStarted.Task;
            var readTask = Task.Run(() => { using (rwLock.ReadLock()) { } });
            var completedTask = await Task.WhenAny(readTask, Task.Delay(100));
            Assert.False(completedTask == readTask);
            writerReleased.SetResult(true);
            await writerTask;
            await readTask;
        }

        [Fact]
        public async Task ReadLockAsync_CompletesSynchronouslyWhenFree()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (await rwLock.ReadLockAsync()) { }
        }

        [Fact]
        public async Task Fairness_FifoOrderForReadersAndWriters()
        {
            var rwLock = new AsyncReaderWriterLock();
            var order = new List<int>();
            Task reader1;
            Task writer;
            Task reader2;
            using (rwLock.WriteLock())
            {
                var reader1Queued = new TaskCompletionSource();
                reader1 = Task.Run(async () =>
                {
                    var lockTask = rwLock.ReadLockAsync();
                    reader1Queued.SetResult();
                    using (await lockTask) { order.Add(1); }
                });
                await reader1Queued.Task;

                var writerQueued = new TaskCompletionSource();
                writer = Task.Run(async () =>
                {
                    var lockTask = rwLock.WriteLockAsync();
                    writerQueued.SetResult();
                    using (await lockTask) { order.Add(2); }
                });
                await writerQueued.Task;

                var reader2Queued = new TaskCompletionSource();
                reader2 = Task.Run(async () =>
                {
                    var lockTask = rwLock.ReadLockAsync();
                    reader2Queued.SetResult();
                    using (await lockTask) { order.Add(3); }
                });
                await reader2Queued.Task;
            }

            await Task.WhenAll(reader1, writer, reader2);

            Assert.Equal(new[] { 1, 2, 3 }, order);
        }

        [Fact]
        public async Task Cancellation_BeforeAcquire_Throws()
        {
            var rwLock = new AsyncReaderWriterLock();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => rwLock.ReadLockAsync(cts.Token).AsTask());
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => rwLock.WriteLockAsync(cts.Token).AsTask());
        }

        [Fact]
        public async Task Cancellation_DuringAsyncAcquire_Throws()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.WriteLock())
            {
                using var cts = new CancellationTokenSource();
                var readTask = rwLock.ReadLockAsync(cts.Token).AsTask();
                cts.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => readTask);
            }
        }

        [Fact]
        public async Task Cancellation_OfMultipleWaiters_OnlyCancelledRemoved()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.WriteLock())
            {
                using var cts1 = new CancellationTokenSource();
                using var cts2 = new CancellationTokenSource();

                var waiter1 = rwLock.ReadLockAsync(cts1.Token).AsTask();
                var waiter2 = rwLock.ReadLockAsync(cts2.Token).AsTask();

                cts1.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => waiter1);
                Assert.False(waiter2.IsCompleted);

                cts2.Cancel();
                await Assert.ThrowsAsync<OperationCanceledException>(() => waiter2);
            }
        }

        [Fact]
        public void Dispose_WhenDisposed_ThrowsObjectDisposedException()
        {
            var rwLock = new AsyncReaderWriterLock();
            rwLock.Dispose();

            Assert.Throws<ObjectDisposedException>(() => rwLock.ReadLock());
            Assert.Throws<ObjectDisposedException>(() => rwLock.WriteLock());
        }

        [Fact]
        public async Task Dispose_CancelsPendingWaiters()
        {
            var rwLock = new AsyncReaderWriterLock();
            using (rwLock.WriteLock())
            {
                var readTask = rwLock.ReadLockAsync().AsTask();
                var writeTask = rwLock.WriteLockAsync().AsTask();

                await Task.Delay(50);
                rwLock.Dispose();

                await Assert.ThrowsAsync<ObjectDisposedException>(() => readTask);
                await Assert.ThrowsAsync<ObjectDisposedException>(() => writeTask);
            }
        }

        [Fact]
        public void Dispose_Idempotent()
        {
            var rwLock = new AsyncReaderWriterLock();
            rwLock.Dispose();
            rwLock.Dispose();
        }

        [Fact]
        public async Task AcquireAfterDispose_Throws()
        {
            var rwLock = new AsyncReaderWriterLock();
            rwLock.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(() => rwLock.ReadLockAsync().AsTask());
            await Assert.ThrowsAsync<ObjectDisposedException>(() => rwLock.WriteLockAsync().AsTask());
        }

        [Fact]
        public async Task ConcurrentReadersAndWriter()
        {
            var rwLock = new AsyncReaderWriterLock();
            const int readerCount = 10;
            var tasks = new Task[readerCount];

            var writerTask = Task.Run(async () =>
            {
                using (await rwLock.WriteLockAsync())
                {
                    await Task.Delay(200);
                }
            });

            for (int i = 0; i < readerCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    using (await rwLock.ReadLockAsync())
                    {
                        await Task.Delay(10);
                    }
                });
            }

            await Task.Delay(50);
            foreach (var t in tasks)
                Assert.False(t.IsCompleted);

            await writerTask;
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task ManyConcurrentReadLocks()
        {
            var rwLock = new AsyncReaderWriterLock();
            const int readerCount = 50;
            var tasks = new Task[readerCount];
            for (int i = 0; i < readerCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    using (await rwLock.ReadLockAsync())
                    {
                        await Task.Delay(10);
                    }
                });
            }
            await Task.WhenAll(tasks);
        }

        [Fact]
        public void Releaser_DisposeTwice_Safe()
        {
            var rwLock = new AsyncReaderWriterLock();
            var releaser = rwLock.ReadLock();
            releaser.Dispose();
            releaser.Dispose();
        }
    }
}
