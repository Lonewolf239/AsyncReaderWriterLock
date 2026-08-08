[![EN](https://img.shields.io/badge/CHANGELOG-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./CHANGELOG.md)
[![RU](https://img.shields.io/badge/CHANGELOG-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./CHANGELOG-RU.md)

## Changelog · AsyncReaderWriterLock

<details open>
<summary><strong>1.0.3</strong> — August 8, 2026</summary>

#### List of changes

- **Fixed a disposed-lock race in the async slow path**
    - `AcquireAsyncSlow` now re-checks `Disposed` after re-acquiring the gate, matching what the synchronous `Acquire` path already did.
    - Previously, a `Dispose()` racing with a pending `ReadLockAsync`/`WriteLockAsync` call could either hand out a lock after disposal, or leave the new waiter's task stuck forever (a disposed lock never drains its queue again, so nothing would ever complete it).
- **Documentation**
    - Added a `<remarks>` on `Releaser` warning against copying or storing the value outside a single `using` statement/expression, since each copy gets an independent double-dispose guard.

</details>

<details>
<summary><strong>1.0.2</strong> — April 1, 2026</summary>

#### List of changes

- **Added an xUnit test suite** covering acquisition, fairness, cancellation, and disposal behavior.
- **Documentation updates.**

</details>

<details>
<summary><strong>1.0.1</strong> — March 31, 2026</summary>

#### List of changes

- **Renamed the namespace** from `NeoIni.Models` to `AsyncLocks`, reflecting the library's extraction into a standalone package.

</details>

<details>
<summary><strong>1.0</strong> — March 31, 2026</summary>

#### List of changes

- **Initial release** — extracted from `NeoIni`'s internal reader-writer lock into a standalone, general-purpose async lock package.

</details>
