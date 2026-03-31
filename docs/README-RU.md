[![NuGet](https://img.shields.io/nuget/v/AsyncReaderWriterLock?style=for-the-badge&logo=nuget&logoColor=FFFFFF)](https://www.nuget.org/packages/AsyncReaderWriterLock)
[![Downloads](https://img.shields.io/nuget/dt/AsyncReaderWriterLock?style=for-the-badge&logo=download&logoColor=FFFFFF)](https://www.nuget.org/packages/AsyncReaderWriterLock)
[![.NET 5+](https://img.shields.io/badge/.NET-5+-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://dotnet.microsoft.com/)
[![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-2D2D2D?style=for-the-badge&logo=dotnet&logoColor=FFFFFF)](https://learn.microsoft.com/ru-ru/dotnet/standard/net-standard?tabs=net-standard-2-0)
[![MIT License](https://img.shields.io/badge/License-MIT-2D2D2D?style=for-the-badge&logo=open-source-initiative&logoColor=FFFFFF)](https://github.com/Lonewolf239/AsyncReaderWriterLock/blob/main/LICENSE)

### Languages
[![EN](https://img.shields.io/badge/README-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](./README.md)
[![RU](https://img.shields.io/badge/README-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](./README-RU.md)

# AsyncReaderWriterLock

Лёгкий, кроссплатформенный асинхронный блокировщик читателей‑писателей для .NET с поддержкой синхронного/асинхронного захвата, отмены и справедливой очереди.

```bash
dotnet add package AsyncReaderWriterLock
```

- **Пакет:** [nuget.org/packages/AsyncReaderWriterLock](https://www.nuget.org/packages/AsyncReaderWriterLock)
- **Версия:** 1.0.1 | **.NET 5+** | **.NET Standard 2.0**
- **Разработчик:** [Lonewolf239](https://github.com/Lonewolf239)

---

## Features

| | Возможность | Подробности |
|---|---------|---------|
| 📖 | **Асинхронная блокировка читателей‑писателей** | Несколько читателей могут удерживать блокировку одновременно; писатели — исключительно. |
| 🔄 | **Синхронный и асинхронный захват** | `ReadLock`/`WriteLock` и `ReadLockAsync`/`WriteLockAsync` для любых сценариев. |
| ⏸️ | **Поддержка отмены** | Ожидающие асинхронные захваты могут быть отменены через `CancellationToken`. |
| 🧵 | **Справедливая очередь FIFO** | Читатели и писатели обслуживаются в порядке поступления. |
| 🧩 | **Лёгкость** | Один файл, без внешних зависимостей. |
| 🔧 | **Кроссплатформенность** | Поддерживает .NET 5+, .NET 6-10 и .NET Standard 2.0 (включая .NET Framework 4.6.2+). |
| 🧪 | **Протестирован** | Полный набор модульных тестов гарантирует корректность при параллелизме. |

---

## Quick Start

### Захват блокировки для чтения

```csharp
using AsyncReaderWriterLock;

var rwLock = new AsyncReaderWriterLock();

using (var releaser = rwLock.ReadLock())
{
    // Несколько потоков могут читать одновременно
    Console.WriteLine("Чтение защищённых данных...");
}
```

### Захват блокировки для записи

```csharp
using (var releaser = rwLock.WriteLock())
{
    // Только один писатель за раз
    Console.WriteLine("Эксклюзивная запись...");
}
```

### Асинхронное использование

```csharp
// Асинхронный захват для чтения
using (var releaser = await rwLock.ReadLockAsync())
{
    await Task.Delay(100);
    Console.WriteLine("Асинхронная блокировка чтения получена");
}

// Асинхронный захват для записи с отменой
var cts = new CancellationTokenSource(500);
try
{
    using (var releaser = await rwLock.WriteLockAsync(cts.Token))
    {
        Console.WriteLine("Асинхронная блокировка записи получена");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Захват блокировки отменён");
}
```

### Освобождение

Блокировка реализует `IDisposable`. Вызовите `Dispose`, когда она больше не нужна, чтобы освободить все ожидающие потоки и ресурсы.

```csharp
using var rwLock = new AsyncReaderWriterLock();
// ... использование
```

---

## Advanced Usage

### Вложенные блокировки

Блокировка **не поддерживает рекурсивный захват**. Попытка захватить ту же блокировку повторно из того же потока приведёт к взаимоблокировке или `SynchronizationLockException` (для захвата писатель→писатель или писатель→читатель). Всегда проектируйте код так, чтобы избегать рекурсивных блокировок.

### Справедливость

Блокировка **справедлива**: ожидающие потоки обслуживаются в порядке запроса. Это предотвращает голодание писателей.

### Ручное освобождение

`Releaser`, возвращаемый `ReadLock()`/`WriteLock()`, является структурой, которая освобождает блокировку при вызове `Dispose`. Вы можете сохранить её и освободить позже, но убедитесь, что `Dispose` будет вызван, иначе возникнет взаимоблокировка.

---

## API Reference

Полная справка по методам — [API.md](./API-RU.md)

---

## Contributing

Приветствуются вклады! Пожалуйста, ознакомьтесь с [CONTRIBUTING-RU.md](./CONTRIBUTING-RU.md).

---

## Philosophy

**Просто, надёжно, потокобезопасно.** `AsyncReaderWriterLock` предоставляет только тот примитив синхронизации, который вам нужен — ничего лишнего. API минимален, но мощен, а реализация тщательно протестирована для гарантии корректности при любых обстоятельствах.
