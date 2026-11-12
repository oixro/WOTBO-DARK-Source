# Рабочая папка и один экземпляр

## TempWorkspace

`ResourceExtractor` распаковывает `regpack.zip`, `su.exe`, `ui.zip`,
`Audio_Lantency*.reg`, `updates.reg` и `explorer.bat`.

`Services/Storage/TempWorkspace` — единая точка правды для
`c:\Windows\oixro` и всех `path_*`.

- `Prepare()` вызывается в `MainForm_Load`:
  - если папки нет — пишет `tempfolder нету`;
  - если есть — удаляет содержимое;
  - если удалить не удалось — переключается на
    `c:\Windows\oixro_<суффикс>` и обновляет все пути.
- `Cleanup()` вызывается при закрытии. Если удалить не удалось, возвращает
  `false`, и `MainForm` завершает процесс прежним `taskkill`.
- Файлы `wotbo*.log` не удаляются: иначе теряется диагностика.
- Корень с оставшимися логами не удаляется целиком, удаляется только
  остальное содержимое.

Свойства: `RegpackZip`, `Regpack`, `Services`, `UiZip`, `Ui`, `DfKiller`,
`Scheme`, `Smartctl`, `SuExecutable`.

`MainForm` при старте копирует их в прежние статические поля `tempfolder`,
`path_*`, поэтому остальной код не менялся.

## InstanceGuard

`App/InstanceGuard` заменил `InstanceChecker`:

```csharp
if (!InstanceGuard.TryAcquire()) { ... }
InstanceGuard.Release();
```

Mutex создаётся по имени exe, как раньше (`AppDomain.CurrentDomain.FriendlyName`).
`Release()` идемпотентен: повторный вызов ничего не делает.

## ReservedStorage

`ReservedStorageService` заменил статический запуск DISM в `MainForm`.

- `GetStateAsync()` — `DISM /Online /Get-ReservedStorageState` через
  `IProcessService.ReadOutputAsync`;
- `IsDisabled(state)` — те же английская/русская строки, что и раньше.

DISM больше не запускается в инициализаторе статического поля: вызов идёт
осознанно в `MainForm_Load`, процесс освобождается после чтения вывода.
