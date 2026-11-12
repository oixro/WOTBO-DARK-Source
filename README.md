# WOTBO

Мой инструмент для настройки Windows. WinForms, .NET Framework 4.8, x64, classic csproj.

Сайт: https://oixro.github.io/wotbo-site/

## Сборка

```bash
export WOTBO_VM_SSH=wotbo-vm
./build/remote-build.sh
```

Результат:

```text
artifacts/x64/Release/WOTBO.exe
artifacts/logs/build-Release-*.log
```

VM: Windows 11, VS 2022 Build Tools, .NET Framework 4.8 targeting pack, OpenSSH.
Настройка VM — `docs/VM-SETUP.md`.

## Заметки

- Версия: `2.34`.
- Твики: `Services/Tweaks`, каталог `TweakCatalog`, UI-связка `UI/TweakUiBinder.cs`.
- Старт: `App/AppBootstrap.cs`, `App/StartupWorkflow.cs`.
- Ссылки на программы: `App/ProgramLinks.cs`.
- Сборка на VM: `build/build.ps1`, запуск: `build/run.ps1`.
- Документация по подсистемам — в `docs/`.
- CI: `.github/workflows/build.yml`, `Release|x64`.
