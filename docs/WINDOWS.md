# Версия Windows

`IWindowsVersionService` (`Services/Windows`) читает:

```text
HKLM\Software\Microsoft\Windows NT\CurrentVersion
    CurrentBuild
    DisplayVersion
```

и возвращает `WindowsVersionInfo`:

- `Build`, `DisplayVersion`;
- `IsWindows11` — build >= 22000;
- `IsWindows10` — build < 22000;
- `IsOldWindows10` — build <= 19042;
- `IsBuildValid` — build удалось разобрать в число.

Сервис не показывает `MessageBox` и ничего не решает за UI. `Form1_Load`
получает информацию и показывает прежние сообщения/подписи.

## Почему `Services/Windows`, а не `Services/System`

Namespace `WOTBO.Services.System` конфликтует с `System.*` внутри соседних
namespace'ов: компилятор ищет `System.Collections` в `WOTBO.Services.System`.
Поэтому используется `WOTBO.Services.Windows`.
