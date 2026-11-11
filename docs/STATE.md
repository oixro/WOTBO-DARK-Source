# Состояние твиков и бэкапы

## Где лежит

Всё состояние — в реестре текущего пользователя:

```text
HKCU\Software\oixro\wotbo\Backups\<tweak-id>
    applied     REG_DWORD    1
    appliedAt   REG_SZ       ISO-8601
    version     REG_SZ       версия WOTBO
    data        REG_SZ       JSON с прежними значениями и файлами
    dataFile    REG_SZ       путь к JSON, если он больше ~900 КБ
```

Файловые копии:

```text
%LOCALAPPDATA%\oixro\wotbo\backups\<tweak-id>\
    000_imageres.dll
    001_key.reg
```

Имена `tweak-id` совпадают с категориями логов: `main.defender`,
`main.pagefile`, `ui.bluefolders`, `extra.onedrive`, `pro.csrss`.

## Как это работает

1. `TweakScope.Apply("main.mpo")` начинает сессию бэкапа.
2. `RegistryService.WriteValue/DeleteValue` перед изменением сообщает
   `TweakBackupSession` старое значение. Оно попадает в JSON.
3. Файлы сохраняются явно: `TweakScope.BackupFile(path)` и
   `TweakScope.BackupDirectory(path)`.
4. Для `.reg`-твиков вызывается `TweakScope.BackupRegFile(path)` — парсер
   находит все секции `[HKEY_...]` и делает `reg.exe export` каждой ветки.
5. На выходе из `using` состояние сохраняется, твик помечается применённым.
6. Откат: сначала выполняется штатный обработчик (например, `restore.reg`),
   затем `TweakStateStore.Restore` возвращает значения и файлы из бэкапа и
   удаляет состояние.

## Точность отката

- Значения, которых до твика не было, удаляются.
- Значения, которые были, восстанавливаются с исходным типом.
- Для полных экспортов дополнительно удаляются значения и подветки,
  появившиеся после экспорта (`reg import` сам лишнее не удаляет).
- Файл, которого не было, удаляется; существовавший — возвращается из копии.
- Каталог, которого не было, удаляется целиком.

## Старые флаги

Раньше в корне `HKCU\Software\oixro\wotbo` лежали значения `regpack`,
`mousefix`, `animations`, `usbpowersaving`, `bcdedit`, `defenderdisabled`,
`buffer` и другие. При старте `TweakStateStore.MigrateLegacyFlags()`:

- переносит каждый найденный флаг в `Backups\<id>\applied`;
- удаляет старое значение.

Это одноразовая миграция. Новые твики старых флагов не пишут.

## Ограничения

- `DISM`, `compact`, очистка WinSxS и обновлений меняют систему вне
  реестра/файловых операций и не имеют полного бэкапа. Для них в лог
  пишется предупреждение `бэкап ... пуст`.
- `tempclean` не бэкапит содержимое `%TEMP%` — это временные файлы.
- OneDrive: сохраняется `OneDriveSetup.exe`; пользовательские данные
  OneDrive не копируются (объём несопоставим с задачей).
- Бэкапы привязаны к пользователю (`HKCU`/`LOCALAPPDATA`). При смене
  пользователя состояние не переносится.
