# Процессы

`IProcessService` (`Services/Processes`) заменяет локальные
`hcmd/scmd/powershell/supercmd/OpenUrl` из `MainForm`, `Form2`, `Form4`.

## Методы

| Метод | Бывший | Семантика |
|---|---|---|
| `StartCaptured(command)` | `hcmd` | `cmd.exe /c chcp 65001 & command`, скрытое окно, stdout/stderr в лог, **без ожидания** |
| `StartCapturedAsync(command)` | `hcmdAsync` | то же через `Task.Run`; не ждёт завершения команды |
| `StartDetached(command, hidden)` | `scmd` | `cmd.exe /c command`; `hidden` — для `Form2/Form4` |
| `StartPowerShell(command)` | `powershell` | `powershell /command command`, скрытое окно |
| `RunSuper(command, suPath)` | `supercmd` | `su.exe /wrs cmd.exe /c command`, ждёт, читает stdout+stderr |
| `OpenUrl(url)` | `OpenUrl` | открывает ссылку; `false`, если браузер не найден |

`RunSuper` — **отдельный** метод. Это не «ещё один `RunAsync`»: через
`su.exe /wrs` работают операции Defender, и смешивать их с обычным запуском
нельзя.

## Совместимость

Локальные обёртки `hcmd/hcmdAsync/scmd/powershell/supercmd` в `MainForm`
удалены: все вызовы идут напрямую в `AppServices.Processes`.
`supercmd` заменён на `RunSuper(command, tempfolder + @"\su.exe")`.

`OpenUrl` остался тонкой UI-обёрткой: при неудаче показывает прежний
`MessageBox`.

`Form2` и `Form4` уже делегируют в `StartDetached(line, true)`.

## Логи

- команда — `INFO [Process]`;
- stdout — `DEBUG [Process]`;
- stderr и неудачный запуск — `WARN/ERROR [Process]`;
- вывод `supercmd` — `DEBUG [Process] [SUPER] ...`.

`StartCaptured` держит ссылку на процесс до `Exited`, поэтому объект не
собирается GC и последние строки вывода не теряются. При этом вызывающий
поток не блокируется.
