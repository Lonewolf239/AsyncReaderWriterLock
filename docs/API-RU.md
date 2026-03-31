[![EN](https://img.shields.io/badge/API-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./API.md)
[![RU](https://img.shields.io/badge/API-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./API-RU.md)

## API Reference · AsyncReaderWriterLock

Полная справка по всем публичным методам, структурам и их версиям.

---

### Core Methods

| Метод | Описание | Асинхронная версия |
|-------|----------|-------------------|
| `ReadLock` | Захватывает блокировку для чтения синхронно. Возвращает `Releaser`. | `ReadLockAsync` |
| `WriteLock` | Захватывает блокировку для записи синхронно. Возвращает `Releaser`. | `WriteLockAsync` |
| `Dispose` | Освобождает все ресурсы и отменяет ожидающих (waiters). | – |

**Примечание:** `ReadLockAsync` и `WriteLockAsync` принимают опциональный `CancellationToken` и возвращают `ValueTask<Releaser>` на современных версиях .NET (или `Task<Releaser>` на .NET Standard 2.0).

---

### Structure `Releaser`

| Метод | Описание |
|-------|----------|
| `Dispose` | Освобождает блокировку, полученную при создании этого токена. |

---

### Remarks

- Блокировка **не рекурсивна**. Попытка повторного захвата из того же потока приведёт к взаимоблокировке или `SynchronizationLockException`.
- Ожидающие потоки ставятся в очередь в порядке **FIFO**.
- Блокировка **потокобезопасна** и может использоваться из нескольких потоков.
- Подробные примеры использования см. в [README-RU.md](./README-RU.md).
