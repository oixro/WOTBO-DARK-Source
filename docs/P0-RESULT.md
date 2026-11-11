# P0 — результат проверки

Дата: 2026-11-11.
Хост: macOS (Apple Silicon), Parallels Desktop.
VM: Windows 11 IoT Enterprise, build 26300, ARM64, пользователь `wotbo`, `10.211.55.6`.

## Проверено

| Проверка | Результат | Чем подтверждено |
|---|---|---|
| Классический `.csproj`, .NET Framework 4.8 | OK | `TargetFrameworkVersion=v4.8` |
| Только x64 | OK | `.sln`/`.csproj`: `Debug\|x64`, `Release\|x64` |
| PE image | OK | `machine=0x8664`, `opt magic=0x20B` (PE32+), `subsystem=2` (GUI), `LARGE_ADDRESS_AWARE` |
| Манифест | OK | В бинарнике: `requireAdministrator`, `longPathAware`, `supportedOS` Win10/11 |
| Сборка в VM по SSH с Mac | OK | `build/remote-build.sh` → `artifacts\x64\Release\WOTBO.exe` |
| Ошибки/warnings компиляции | OK | 0 warning, 0 error в build-логе |
| OpenSSH по ключу | OK | `ssh wotbo-vm "echo ok"` → `ok` |
| Build Tools и MSBuild | OK | MSBuild 17.14.60, .NET Framework 4.8.1 (`Release=533509`) |
| `artifacts/` не попадает в git | OK | `.gitignore:64` |
| Запуск от администратора | OK | `run.ps1` → `Exit code: 0` |
| `%TEMP%\wotbo.log` пишется и копируется | OK | `artifacts\logs\wotbo-20261111-202451.log`, 20+ строк |
| Навигация по панелям | OK | В логе: Основное, Интерфейс, GPU, Дополнительно, Скачать проги, PC Info |
| Baseline UI | OK | `docs/baseline/` — 9 скриншотов (3 панели по 2 страницы + 3 одностраничные) |
| Определение железа и ОС | OK | `OS: Windows 11 26H2 Build:26300`, GPU/RAM/Display определены |
| Корректное закрытие | OK | Лог: `closing`, `удаляем tempfolder` |

## Наблюдения из первого запуска

- VM выделено **4 ГБ RAM** (рекомендовали 8). Приложение определило `ОЗУ: 4 GB`.
  Для тяжёлых операций (WMI, распаковка, поиск по диску) стоит поднять до 8 ГБ.
- Экран `1920x1200 60 Hz` — проверять UI на 100 % и 125 % масштабировании.
- Temp-папка приложения создаётся в `C:\Windows\oixro` (процесс elevated),
  а не в `%TEMP%` пользователя — это поведение надо сохранить при рефакторинге
  путей в `AppPaths`.

## Не проверено

- [x] Снапшот `P0-clean` сделан после настройки VM (2026-11-11).

## Известные отклонения

- **MSBuild установлен в `C:\Program\...`** вместо `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools`.
  Причина — неэкранированный `--installPath` в старой версии `setup-vm.ps1`.
  Исправлено в скрипте; переустановка не выполнена, сборке не мешает
  (`build.ps1` находит MSBuild через `vswhere`).
- **Windows 11 IoT Enterprise, build 26300** — 26H2-поколение, предварительная сборка
  и редакция без Store/OneDrive/Copilot. Для P4/P5 это значит, что
  `CheckAvailability` должен корректно возвращать `Unavailable`, а не падать.
- **ARM64-хост.** WOTBO x64 и MSBuild идут через эмуляцию. Сборка IL от этого
  не зависит, но поведение `DISM`/`bcdedit`/`powercfg` может отличаться от
  нативного x64 — проверять на этапах P4/P5.

## Как воспроизвести

```bash
# на Mac
export WOTBO_VM_SSH=wotbo-vm
./build/remote-build.sh          # sync + build + pull logs and binary
```

```powershell
# в VM, elevated PowerShell на рабочем столе
cd C:\src\WOTBO
.\build\doctor.ps1
powershell -ExecutionPolicy Bypass -File .\build\run.ps1 -Configuration Release
```
