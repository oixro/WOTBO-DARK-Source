# Рабочая папка и один экземпляр

## TempWorkspace

`Services/Storage/TempWorkspace` — единая точка правды для
`c:\Windows\oixro` и всех `path_*`.

- `Prepare()` вызывается в `Form1_Load`:
  - если папки нет — пишет `tempfolder нету`;
  - если есть — удаляет содержимое;
  - если удалить не удалось — переключается на
    `c:\Windows\oixro_<суффикс>` и обновляет все пути.
- `Cleanup()` вызывается при закрытии. Если удалить не удалось, возвращает
  `false`, и `Form1` завершает процесс прежним `taskkill`.
- Файлы `wotbo*.log` не удаляются: иначе теряется диагностика.
- Корень с оставшимися логами не удаляется целиком, удаляется только
  остальное содержимое.

Свойства: `RegpackZip`, `Regpack`, `Services`, `UiZip`, `Ui`, `DfKiller`,
`Scheme`, `Smartctl`, `SuExecutable`.

`Form1` при старте копирует их в прежние статические поля `tempfolder`,
`path_*`, поэтому остальной код не менялся.

## InstanceGuard

`App/InstanceGuard` заменил `InstanceChecker`:

```csharp
if (!InstanceGuard.TryAcquire()) { ... }
InstanceGuard.Release();
```

Mutex создаётся по имени exe, как раньше (`AppDomain.CurrentDomain.FriendlyName`).
`Release()` идемпотентен: повторный вызов ничего не делает.
