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

## Сверка с фактическим состоянием

`TweakUiBinder.Detect` при каждом старте сразу сверяет результат `Detect`
с `TweakStateStore`:

- `Detect == Applied`, а состояния нет → `MarkApplied`;
- `Detect != Applied`, а состояние есть → `Remove`.

Отдельного полного прохода по всем 48 твикам нет: дорогие проверки
(`gpu.directplay`, `gpu.ansel`) не запускаются, пока не нужны.

Это синхронизирует чекбокс, стрелку отката и `TweakHoverInfo`.
Для одноразовых твиков (`tempclean`, `updateclean`, `winsxs`, `compactos`)
состояние по-прежнему означает «запускался», а не фактическое состояние
системы.

## Старые флаги

Раньше в корне `HKCU\Software\oixro\wotbo` лежали значения `regpack`,
`mousefix`, `animations`, `usbpowersaving`, `bcdedit`, `defenderdisabled`,
`buffer` и другие. При старте `TweakStateStore.MigrateLegacyFlags()`:

- переносит каждый найденный флаг в `Backups\<id>\applied`;
- удаляет старое значение.

Это одноразовая миграция. Новые твики старых флагов не пишут.

## Ограничения

- `DISM`, `compact`, очистка WinSxS и обновлений меняют систему вне
  реестра/файловых операций. Для твиков без бэкапа (список ниже) сохраняется
  только время применения; для остальных при пустом бэкапе пишется
  предупреждение `бэкап ... пуст`.
- `tempclean` не бэкапит содержимое `%TEMP%` — это временные файлы.
- OneDrive: сохраняется `OneDriveSetup.exe`; пользовательские данные
  OneDrive не копируются (объём несопоставим с задачей).
- Бэкапы привязаны к пользователю (`HKCU`/`LOCALAPPDATA`). При смене
  пользователя состояние не переносится.

### Defender

`main.defender` делает бэкап сам (движок): журнал
`%ProgramData%\oixro\wotbo\defender\journal.json` и экспорт реестра
зеркалируются в `%LOCALAPPDATA%\oixro\wotbo\backups\main.defender`.
Штатный `TweakBackup` дополнительно хранит верхнеуровневые ветки Defender.

### Твики без бэкапа

Для твиков, где бэкап невозможен или бессмысленен, сохраняются только
`applied` и `appliedAt`, без `data`:

```text
main.reservedstorage, main.hibernate, main.powerscheme,
gpu.ansel, gpu.directplay, ui.animations,
extra.compactos, extra.winsxs, extra.tempclean,
extra.updateclean, pro.bufferbloat
```

Их список — `TweakStateStore.SupportsBackup`. В подсказке при наведении
для них показывается только время применения.

## Подсказка при наведении

`UI/TweakHoverInfo` кэширует готовый текст на 30 секунд, поэтому движение
мыши внутри того же твика не читает реестр. Кэш сбрасывается событием
`TweakStateStore.Changed` после apply/rollback. Отдельно показываются путь,
старое и текущее значение; для `.reg`-экспортов значения парсятся из
сохранённого файла.

Подсказки по умолчанию **выключены**. Включение — на вкладке «Интерфейс»
(вторая страница), чекбокс «Показывать подсказки о применённых твиках».
Настройка работает как твик: отметить → «Применить», после чего чекбокс
сбрасывается, появляется стрелка отката; стрелка выключает подсказки.
Состояние хранится в `HKCU\Software\oixro\wotbo\tweakTooltips` (`0`/`1`),
а факт применения — в `Backups\ui.tweakTooltips`.
