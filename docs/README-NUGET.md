# AsyncReaderWriterLock

A lightweight, cross-platform asynchronous reader‑writer lock for .NET with sync/async support, cancellation, and fair queuing.

```bash
dotnet add package AsyncReaderWriterLock
```

- **Package:** [nuget.org/packages/AsyncReaderWriterLock](https://www.nuget.org/packages/AsyncReaderWriterLock)
- **Version:** 1.0.2 | **.NET 5+** | **.NET Standard 2.0**
- **Developer:** [Lonewolf239](https://github.com/Lonewolf239)

---

## Features

| | Feature | Details |
|---|---------|---------|
| 📖 | **Async reader‑writer lock** | Multiple readers can hold the lock simultaneously; writers are exclusive. |
| 🔄 | **Sync + async acquire** | `ReadLock`/`WriteLock` and `ReadLockAsync`/`WriteLockAsync` for all scenarios. |
| ⏸️ | **Cancellation support** | Pending async acquires can be canceled via `CancellationToken`. |
| 🧵 | **Fair FIFO queuing** | Readers and writers are queued in order of arrival. |
| 🧩 | **Lightweight** | Single file, no external dependencies. |
| 🔧 | **Cross‑platform** | Supports .NET 5+, .NET 6-10, and .NET Standard 2.0 (including .NET Framework 4.6.2+). |
| 🧪 | **Tested** | Full unit test suite ensures correctness under concurrency. |

---

## Quick Start

### Acquiring a read lock

```csharp
using AsyncLocks;

var rwLock = new AsyncReaderWriterLock();

using (var releaser = rwLock.ReadLock())
{
    // Multiple threads can read concurrently
    Console.WriteLine("Reading protected data...");
}
```

### Acquiring a write lock

```csharp
using (var releaser = rwLock.WriteLock())
{
    // Only one writer at a time
    Console.WriteLine("Writing exclusive data...");
}
```

### Asynchronous usage

```csharp
// Async read lock
using (var releaser = await rwLock.ReadLockAsync())
{
    await Task.Delay(100);
    Console.WriteLine("Async read lock acquired");
}

// Async write lock with cancellation
var cts = new CancellationTokenSource(500);
try
{
    using (var releaser = await rwLock.WriteLockAsync(cts.Token))
    {
        Console.WriteLine("Async write lock acquired");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Lock acquisition was canceled");
}
```

### Disposal

The lock implements `IDisposable`. Dispose it when no longer needed to release all waiting threads and free resources.

```csharp
using var rwLock = new AsyncReaderWriterLock();
// ... use lock
```

---

## Advanced Usage

### Nesting locks

The lock **does not support recursive acquisition**. Attempting to acquire the same lock again from the same thread will result in a deadlock or `SynchronizationLockException` (for write→write or write→read). Always design your code to avoid recursive locking.

### Fairness

The lock is **fair**: waiting threads are served in the order they requested the lock. This prevents writer starvation.

### Manual release

The `Releaser` returned by `ReadLock()`/`WriteLock()` is a `struct` that releases the lock when disposed. You can store it and release later, but ensure proper disposal to avoid deadlocks.

---

## API Reference

Full method reference — [API.md](https://github.com/Lonewolf239/AsyncReaderWriterLock/blob/main/docs/API.md)

---

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](https://github.com/Lonewolf239/AsyncReaderWriterLock/blob/main/docs/CONTRIBUTING.md) for guidelines.

---

## Philosophy

**Simple, reliable, thread‑safe.** `AsyncReaderWriterLock` provides just the synchronization primitive you need — nothing more, nothing less. The API is minimal but powerful, and the implementation is thoroughly tested to ensure correctness under all circumstances.
