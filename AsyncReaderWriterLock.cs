using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncLocks
{
    /// <summary>
    /// Asynchronous reader‑writer lock with support for sync and async acquire, cancellation, and fair queuing.
    /// <br/>
    /// Developer: <a href="https://github.com/Lonewolf239">Lonewolf239</a>
    /// <br/>
    /// <b>Target Frameworks: .NET 5+ and .NET Standard 2.0</b>
    /// <br/>
    /// <b>Version: 1.0.2</b>
    /// <br/>
    /// <b>Design:</b> Multiple readers can hold the lock simultaneously. Writers are exclusive.
    /// Waiting readers and writers are queued in FIFO order; writers are granted only when no readers are active.
    /// Supports cancellation of pending async acquires.
    /// </summary>
    public sealed class AsyncReaderWriterLock : IDisposable
    {
        /// <summary>
        /// Represents a disposable token that releases the lock when disposed.
        /// Returned by <see cref="ReadLock"/>, <see cref="WriteLock"/> and their async counterparts.
        /// </summary>
        public struct Releaser : IDisposable
        {
            private AsyncReaderWriterLock? Owner;
            private readonly bool IsWrite;

            internal Releaser(AsyncReaderWriterLock owner, bool isWrite)
            {
                Owner = owner;
                IsWrite = isWrite;
            }

            /// <summary>Releases the lock that was acquired when this token was created.</summary>
            public void Dispose()
            {
                var owner = Interlocked.Exchange(ref Owner, null);
                owner?.Release(IsWrite);
            }
        }

        private abstract class Waiter
        {
            internal bool IsWrite { get; }
            internal LinkedListNode<Waiter>? Node { get; set; }

            protected Waiter(bool isWrite) => IsWrite = isWrite;

            internal abstract void Complete(Releaser releaser);
            internal abstract void Fail(Exception ex);
        }

        private sealed class AsyncWaiter : Waiter
        {
            internal AsyncReaderWriterLock Owner { get; }
            internal CancellationToken Ct { get; }
            internal TaskCompletionSource<Releaser> Tcs { get; }

            internal AsyncWaiter(AsyncReaderWriterLock owner, bool isWrite, CancellationToken ct) : base(isWrite)
            {
                Owner = owner;
                Ct = ct;
                Tcs = new TaskCompletionSource<Releaser>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            internal override void Complete(Releaser releaser) => Tcs.TrySetResult(releaser);
            internal override void Fail(Exception ex) => Tcs.TrySetException(ex);
        }

        private sealed class SyncWaiter : Waiter
        {
            internal ManualResetEventSlim Event { get; } = new ManualResetEventSlim(false);
            internal Exception? Error { get; private set; }

            internal SyncWaiter(bool isWrite) : base(isWrite) { }

            internal override void Complete(Releaser releaser) => Event.Set();

            internal override void Fail(Exception ex)
            {
                Error = ex;
                Event.Set();
            }
        }

#if NET9_0_OR_GREATER
    private readonly Lock Gate = new();
#else
        private readonly object Gate = new object();
#endif
        private readonly LinkedList<Waiter> Waiters = new LinkedList<Waiter>();

        private int ActiveReaders;
        private bool WriterActive;
        private bool Disposed;

        private static readonly Action<object?> CancelWaiterAction = state =>
        {
            var waiter = (AsyncWaiter)state!;
            waiter.Owner.CancelWaiter(waiter);
        };

        /// <summary>Acquires the lock for reading synchronously.</summary>
        /// <returns>A <see cref="Releaser"/> that releases the lock when disposed.</returns>
        public Releaser ReadLock() => Acquire(false);

        /// <summary>Acquires the lock for reading asynchronously.</summary>
        /// <param name="ct">Cancellation token that can be used to cancel the pending acquisition.</param>
        /// <returns>A task that resolves to a <see cref="Releaser"/> when the lock is acquired.</returns>
#if NETSTANDARD2_0
        public async Task<Releaser> ReadLockAsync(CancellationToken ct = default) => await AcquireAsync(false, ct);
#else
        public ValueTask<Releaser> ReadLockAsync(CancellationToken ct = default) => AcquireAsync(false, ct);
#endif

        /// <summary>Acquires the lock for writing synchronously.</summary>
        /// <returns>A <see cref="Releaser"/> that releases the lock when disposed.</returns>
        public Releaser WriteLock() => Acquire(true);

        /// <summary>Acquires the lock for writing asynchronously.</summary>
        /// <param name="ct">Cancellation token that can be used to cancel the pending acquisition.</param>
        /// <returns>A task that resolves to a <see cref="Releaser"/> when the lock is acquired.</returns>
#if NETSTANDARD2_0
        public async Task<Releaser> WriteLockAsync(CancellationToken ct = default) => await AcquireAsync(true, ct);
#else
        public ValueTask<Releaser> WriteLockAsync(CancellationToken ct = default) => AcquireAsync(true, ct);
#endif

        private Releaser Acquire(bool isWrite)
        {
            SyncWaiter waiter;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
            lock (Gate)
#endif
            {
                ThrowIfDisposed();
                if (CanAcquireImmediately(isWrite))
                {
                    GrantImmediate(isWrite);
                    return new Releaser(this, isWrite);
                }
                waiter = new SyncWaiter(isWrite);
                waiter.Node = Waiters.AddLast(waiter);
            }
            waiter.Event.Wait();
            waiter.Event.Dispose();
#if NETSTANDARD2_0
            if (!(waiter.Error is null)) throw waiter.Error;
#else
            if (waiter.Error is not null) throw waiter.Error;
#endif
            return new Releaser(this, isWrite);
        }

#if NETSTANDARD2_0
        private async Task<Releaser> AcquireAsync(bool isWrite, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            lock (Gate)
            {
                ThrowIfDisposed();
                if (CanAcquireImmediately(isWrite))
                {
                    GrantImmediate(isWrite);
                    return new Releaser(this, isWrite);
                }
            }

            return await AcquireAsyncSlow(isWrite, ct).ConfigureAwait(false);
        }
#else
        private ValueTask<Releaser> AcquireAsync(bool isWrite, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
            lock (Gate)
#endif
            {
                ThrowIfDisposed();
                if (CanAcquireImmediately(isWrite))
                {
                    GrantImmediate(isWrite);
                    return new ValueTask<Releaser>(new Releaser(this, isWrite));
                }
            }
            return new ValueTask<Releaser>(AcquireAsyncSlow(isWrite, ct));
        }
#endif

        private async Task<Releaser> AcquireAsyncSlow(bool isWrite, CancellationToken ct)
        {
            AsyncWaiter waiter;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
            lock (Gate)
#endif
            {
                if (CanAcquireImmediately(isWrite))
                {
                    GrantImmediate(isWrite);
                    return new Releaser(this, isWrite);
                }
                waiter = new AsyncWaiter(this, isWrite, ct);
                waiter.Node = Waiters.AddLast(waiter);
            }
            using (ct.CanBeCanceled ? ct.Register(CancelWaiterAction, waiter) : default)
                return await waiter.Tcs.Task.ConfigureAwait(false);
        }

        private void CancelWaiter(AsyncWaiter waiter)
        {
            List<Waiter>? toWake = null;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
            lock (Gate)
#endif
            {
#if NETSTANDARD2_0
                if (!(waiter.Node is null))
#else
                if (waiter.Node is not null)
#endif
                {
                    Waiters.Remove(waiter.Node);
                    waiter.Node = null;
                    waiter.Fail(new OperationCanceledException(waiter.Ct));
                    toWake = DrainReadyWaiters();
                }
            }
#if NETSTANDARD2_0
            if (!(toWake is null))
#else
            if (toWake is not null)
#endif
            {
                foreach (var w in toWake)
                    w.Complete(new Releaser(this, w.IsWrite));
            }
        }

        private void Release(bool isWrite)
        {
            List<Waiter>? toWake;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
            lock (Gate)
#endif
            {
                if (isWrite)
                {
                    if (!WriterActive) throw new SynchronizationLockException("No writer lock is currently held.");
                    WriterActive = false;
                }
                else
                {
                    if (ActiveReaders <= 0) throw new SynchronizationLockException("No reader lock is currently held.");
                    ActiveReaders--;
                }
                toWake = DrainReadyWaiters();
            }
#if NETSTANDARD2_0
            if (!(toWake is null))
#else
            if (toWake is not null)
#endif
            {
                foreach (var waiter in toWake)
                    waiter.Complete(new Releaser(this, waiter.IsWrite));
            }
        }

        private bool CanAcquireImmediately(bool isWrite)
        {
            if (Waiters.Count > 0 || WriterActive) return false;
            if (isWrite && ActiveReaders > 0) return false;
            return true;
        }

        private void GrantImmediate(bool isWrite)
        {
            if (isWrite) WriterActive = true;
            else ActiveReaders++;
        }

        private List<Waiter>? DrainReadyWaiters()
        {
            if (Disposed || WriterActive) return null;
            if (Waiters.Count == 0) return null;
            var first = Waiters.First!.Value;
            if (first.IsWrite)
            {
                if (ActiveReaders > 0) return null;
                Waiters.RemoveFirst();
                first.Node = null;
                WriterActive = true;
                return new List<Waiter> { first };
            }
            else
            {
                var readers = new List<Waiter>();
#if NETSTANDARD2_0
                while (!(Waiters.First is null) && !Waiters.First.Value.IsWrite)
#else
                while (Waiters.First is not null && !Waiters.First.Value.IsWrite)
#endif
                {
                    var reader = Waiters.First.Value;
                    Waiters.RemoveFirst();
                    reader.Node = null;
                    ActiveReaders++;
                    readers.Add(reader);
                }
                return readers;
            }
        }

        private void ThrowIfDisposed()
        {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(Disposed, nameof(AsyncReaderWriterLock));
#else
            if (Disposed) throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));
#endif
        }

        /// <summary>Releases all resources used by the lock. Any pending waiters are cancelled.</summary>
        public void Dispose()
        {
            List<Waiter>? toFail = null;
#if NET9_0_OR_GREATER
        using (var _ = Gate.EnterScope())
#else
            lock (Gate)
#endif
            {
                if (Disposed) return;
                Disposed = true;
                if (Waiters.Count > 0)
                {
                    toFail = new List<Waiter>(Waiters.Count);
                    foreach (var waiter in Waiters)
                    {
                        waiter.Node = null;
                        toFail.Add(waiter);
                    }
                    Waiters.Clear();
                }
            }
#if NETSTANDARD2_0
            if (!(toFail is null))
#else
            if (toFail is not null)
#endif
            {
                var ex = new ObjectDisposedException(nameof(AsyncReaderWriterLock));
                foreach (var waiter in toFail) waiter.Fail(ex);
            }
        }
    }
}
