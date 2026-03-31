[![EN](https://img.shields.io/badge/API-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./API.md)
[![RU](https://img.shields.io/badge/API-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./API-RU.md)

## API Reference · AsyncReaderWriterLock

Complete reference for all public methods, structures, and their versions.

---

### Core Methods

| Method | Description | Async Version |
|--------|-------------|---------------|
| `ReadLock` | Acquires the lock for reading synchronously. Returns a `Releaser`. | `ReadLockAsync` |
| `WriteLock` | Acquires the lock for writing synchronously. Returns a `Releaser`. | `WriteLockAsync` |
| `Dispose` | Releases all resources and cancels any pending waiters. | – |

**Note:** `ReadLockAsync` and `WriteLockAsync` accept an optional `CancellationToken` and return `ValueTask<Releaser>` on modern .NET (or `Task<Releaser>` on .NET Standard 2.0).

---

### Structure `Releaser`

| Method | Description |
|--------|-------------|
| `Dispose` | Releases the lock that was acquired when this token was created. |

---

### Remarks

- The lock is **not recursive**. Attempting to acquire it again from the same thread will cause a deadlock or `SynchronizationLockException`.
- Waiting threads are queued in **FIFO** order.
- The lock is **thread-safe** and can be used across multiple threads.
- For detailed usage examples, see the [README](./README.md).
