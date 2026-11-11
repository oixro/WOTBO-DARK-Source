# Реестр

## Зачем

`Form1` исторически читал и писал реестр напрямую, без проверки `null` и без
единого представления. `RegistryService` — единственная точка доступа:

```csharp
using WOTBO.App;
using Microsoft.Win32;

string build = AppServices.Registry.GetValue(
    RegistryHive.LocalMachine,
    @"Software\Microsoft\Windows NT\CurrentVersion",
    "CurrentBuild") as string;

AppServices.Registry.WriteValue(
    RegistryHive.LocalMachine,
    @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
    "ClearPageFileAtShutdown",
    0,
    RegistryValueKind.DWord);
```

## Представление x64

По умолчанию используется `RegistryView.Registry64`. Это важно: приложение x64,
и без явного view часть ключей `HKLM\Software` на 64-битной системе уехала бы
в `WOW6432Node`. При необходимости view можно задать в конструкторе:

```csharp
var service = new RegistryService(log, RegistryView.Registry32);
```

## Логи

- чтение — `DEBUG` (пишется всегда; уровень по умолчанию в `AppLogger` — `Debug`);
- запись/удаление — `INFO` со старым и новым значением:

```text
2026-11-11 20:58:57.0387 - [DEBUG] [Registry] чтение LocalMachine\...\CurrentBuild (String) = "26300"
2026-11-11 20:58:57.1000 - [INFO ] [Registry] запись LocalMachine\...\OverlayTestMode (DWord) = 5 (было: <отсутствовало>)
2026-11-11 20:58:57.1234 - [INFO ] [Registry] запись LocalMachine\...\Value (String) = "bye" (было: "hello")
```

Все записи и удаления в `Form1`, `Form2` и `Win32Priority` переведены на сервис,
поэтому old → new попадает в лог. Читать через сервис пока не обязательно:
существующие чтения в `Form1` оставлены как есть.

### Открытый ключ

Где ключ уже открыт (`key`, `edge`, `targetKey`), используется перегрузка:

```csharp
AppServices.Registry.WriteValue(key, "CpuPriorityClass", "1", RegistryValueKind.DWord);
AppServices.Registry.DeleteValue(key, "Start_old");
```

`null`-ключ — тихий пропуск, как прежнее `key?.SetValue(...)`. Полное имя ключа
берётся из `RegistryKey.Name`.

## Ошибки

Все неудачи оборачиваются в `RegistryOperationException`: понятный русский текст,
полное имя ключа/значения и исходное исключение внутри. Для `UnauthorizedAccessException`
и `SecurityException` добавляется подсказка про права администратора и владельца
ключа. Исключение пишется в лог как `ERROR` со стеком.

## Снимки и откат

Для твиков, меняющих существующие значения, используется постоянный бэкап
`TweakStateStore` (см. `docs/STATE.md`): при применении старые значения
сохраняются в `HKCU\Software\oixro\wotbo\Backups\<tweak-id>`, при откате
возвращаются. Для полных веток дополнительно делается `reg.exe export`.

В памяти остаётся `Capture`/`Restore` — удобно для разовых операций:

```csharp
RegistryKeySnapshot before = AppServices.Registry.Capture(
    RegistryHive.LocalMachine,
    @"SYSTEM\CurrentControlSet\Control\...");

// ... применение твика ...

AppServices.Registry.Restore(before);
```

`Capture` рекурсивно сохраняет значения и подветки. `Restore` возвращает ветку
к состоянию снимка:

- если ветки до твика не было (`Existed == false`) — удаляет её целиком;
- восстанавливает значения с исходным типом (`REG_EXPAND_SZ` читается без
  раскрытия переменных);
- удаляет значения и подветки, появившиеся после снимка;
- отсутствие ключа при удалении не считается ошибкой.

## Что ещё не сделано

- `RegistryPaths` — вынести магические пути в константы.
- Владелец ключей `TrustedInstaller` — отдельная операция взятия владения
  (понадобится для части твиков 24H2+).
