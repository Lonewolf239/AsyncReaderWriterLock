[![EN](https://img.shields.io/badge/CHANGELOG-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./CHANGELOG.md)
[![RU](https://img.shields.io/badge/CHANGELOG-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./CHANGELOG-RU.md)

## Changelog · AsyncReaderWriterLock

<details open>
<summary><strong>1.0.3</strong> — 8 августа 2026</summary>

#### List of changes

- **Исправлена гонка с disposed-состоянием на асинхронном медленном пути**
    - `AcquireAsyncSlow` теперь повторно проверяет `Disposed` после повторного захвата gate — так же, как это уже делал синхронный путь `Acquire`.
    - Ранее гонка `Dispose()` с ожидающим вызовом `ReadLockAsync`/`WriteLockAsync` могла либо выдать блокировку уже после освобождения ресурсов, либо оставить задачу нового ожидающего зависшей навсегда (освобождённая блокировка больше никогда не разгребает очередь, поэтому её никто и никогда не завершил бы).
- **Документация**
    - В `Releaser` добавлен `<remarks>` с предупреждением не копировать и не сохранять значение вне одного `using`-выражения, так как у каждой копии независимая защита от двойного `Dispose`.

</details>

<details>
<summary><strong>1.0.2</strong> — 1 апреля 2026</summary>

#### List of changes

- **Добавлен набор xUnit-тестов**, покрывающий захват блокировки, справедливость очереди, отмену и освобождение ресурсов.
- **Обновления документации.**

</details>

<details>
<summary><strong>1.0.1</strong> — 31 марта 2026</summary>

#### List of changes

- **Переименовано пространство имён** с `NeoIni.Models` на `AsyncLocks`, отражая выделение библиотеки в отдельный пакет.

</details>

<details>
<summary><strong>1.0</strong> — 31 марта 2026</summary>

#### List of changes

- **Первый релиз** — выделен из внутренней блокировки читателей‑писателей `NeoIni` в отдельный, универсальный пакет асинхронной блокировки.

</details>
