# WOTBO

Windows optimization tool by oixro. WinForms, .NET Framework 4.8, **x64**.

## Разработка

- Редактирование: macOS + VSCode.
- Сборка и запуск: Windows 11 VM (x64 или Windows 11 ARM с эмуляцией x64).
- Подробная настройка VM: [docs/VM-SETUP.md](docs/VM-SETUP.md).

## Быстрый старт

На Mac (один раз, в `~/.ssh/config`):

```sshconfig
Host wotbo-vm
    HostName 10.211.55.6
    User wotbo
    IdentityFile ~/.ssh/wotbo_vm
    IdentitiesOnly yes
```

Дальше:

```bash
export WOTBO_VM_SSH=wotbo-vm
./build/sync.sh          # синхронизировать исходники в VM
./build/remote-build.sh  # собрать в VM, забрать логи и бинарник
```

Вместо alias можно задать `WOTBO_VM_HOST` и `WOTBO_VM_USER`.

В VM:

```powershell
cd C:\src\WOTBO
powershell -ExecutionPolicy Bypass -File .\build\build.ps1 -Configuration Release
powershell -ExecutionPolicy Bypass -File .\build\run.ps1 -Configuration Release
```

Результат сборки:

```text
artifacts\x64\Release\WOTBO.exe
artifacts\logs\build-Release-*.log
artifacts\logs\wotbo-*.log
```

## Требования к VM

- Windows 11 (26H2 и новее).
- Visual Studio 2022 Build Tools: Managed Desktop Build Tools,
  .NET Framework 4.8 targeting pack, Windows 11 SDK.
- OpenSSH Server для синхронизации с Mac.
- Снапшот чистой VM перед тестами: твики меняют реестр и системные файлы.

## Структура

```text
Project.sln / Project.csproj   # classic csproj, Debug|x64 / Release|x64
build/                         # build, run, sync, setup-vm
docs/                          # документация по окружению
UI/                            # PanelNavigator, TweakUiBinder, DPI, painters
Resources/                     # встроенные .reg/.bat/.zip/.exe
Properties/                    # Resources.resx, AssemblyInfo, Settings
```

## Архитектура

```text
App/                 AppBootstrap, StartupWorkflow, ProgramLinks, AppServices, TweakScope
Services/Tweaks/     ITweak, TweakBase, TweakContext, TweakCatalog, 48 твиков
Services/Registry/   RegistryService и RegistryPaths
Services/State/      состояние, бэкапы и восстановление
Services/Network/    загрузки, GitHub API, UpdateCoordinator, версии программ
Services/Storage/    TempWorkspace, ResourceExtractor, ReservedStorageService
Services/Windows/    определение версии Windows, LegacyCleanupService
UI/                  TweakUiBinder, PanelNavigator, NavigationController, DPI
MainForm.cs          поля, конструктор, закрытие (331 строка)
MainForm.*.cs        partial-файлы: Binders, Load, Tweaks, Features,
                     Navigation, Rollbacks
```

## Новый твик

1. Добавить класс-наследник `TweakBase` в `Services/Tweaks/`.
2. Указать файл в `Project.csproj`.
3. Зарегистрировать экземпляр в `TweakCatalog.CreateDefault()`.
4. Добавить `Bind(...)` в `MainForm.SetupTweakBinders()`.
5. Обновить `docs/TWEAKS.md`.

Detect/Apply/Rollback и обновление чекбокса/стрелки делает
`TweakUiBinder`; UI-специфичные шаги передаются колбэками.

## Статус

Идёт поэтапный рефакторинг. Завершены **P0–P6**: окружение и сборка,
инфраструктурные сервисы, 48 твиков на `ITweak`, UI-инфраструктура
(`TweakUiBinder`, `PanelNavigator`, `NavigationController`), DPI awareness
([docs/DPI.md](docs/DPI.md)), разгрузка `MainForm` (331 строка) и
переименование форм в namespace `WOTBO`.
