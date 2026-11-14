# Defender

## Что делает твик `main.defender`

Полностью отключает Windows Defender и связанные защиты. Файлы **не удаляются**,
а переименовываются в `*.wotbo_bak`, поэтому откат возвращает систему в исходное
состояние.

## Что отключается

| Блок | Действие |
|---|---|
| Службы `WinDefend`, `WdNisSvc`, `Sense`, `MDCoreSvc`, `MpDefenderCoreService`, `SgrmBroker`, `SgrmAgent`, `SecurityHealthService`, `webthreatdefsvc`, `webthreatdefusersvc_*` | `Start=4`, `LaunchProtected=4` |
| Драйверы `WdBoot`, `WdFilter`, `WdNisDrv`, `MsSecFlt`, `MsSecCore`, `MsSecWfp` | `Start=4` |
| Папки `Windows Defender`, `Windows Defender ATP`, `Windows Security Health` | переименование в `*.wotbo_bak` |
| Задачи `\Microsoft\Windows\Windows Defender\*`, `AppID\SmartScreenSpecific` | XML в журнал + disable |
| Политики `Policies\Microsoft\Windows Defender*` | `DisableAntiSpyware`, realtime/behavior/on-access, spynet, consent |
| Брандмауэр | `netsh advfirewall set allprofiles state off` + `MpsSvc` `Start=4` (BFE не трогаем) |
| SmartScreen | `EnableSmartScreen=0`, `SmartScreenEnabled=Off`, Edge `EnabledV9=0` |
| Smart App Control | `CI\Policy\VerifiedAndReputablePolicyState=0` (best effort, обратно не включается) |
| Windows Security | `SecurityHealthService` `Start=4`, `Run\SecurityHealth` удаляется |
| Контекстное меню «Сканировать» | ключи `EPP` удаляются |

Инвентаризация служб делается по факту (включая `webthreatdefusersvc_*`), поэтому
список работает и на старых сборках, и на 24H2/25H2/26H2.

## Порядок работы

1. Пользователь видит предупреждение: античиты, Windows Update, BitLocker,
   необратимость Smart App Control, способ отката.
2. Проверяется BitLocker и Tamper Protection.
3. `prepare`: экспорт реестра (31 ветка), папки, XML задач, журнал
   `%ProgramData%\oixro\wotbo\defender\journal.json`.
4. Быстрый путь: исключение в Defender, `takeown`/`icacls`, stop служб,
   `Start=4`, задачи, политики, переименования.
5. Если службы защищены (`Access denied`, типично для 24H2+) — автоматический
   безопасный режим.

## Безопасный режим

Планировщик задач в safe mode задачи **не запускает**, поэтому основной триггер —
одноразовая служба:

```text
WotboDefender  →  WotboDefender.exe --defender-service
HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal\WotboDefender
HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Network\WotboDefender
```

Сервисы из `SafeBoot\Minimal` SCM запускает и в safe mode, и сразу под SYSTEM.
Это важно: только SYSTEM может писать защищённые ключи служб Defender
(Administrators получают «unauthorized operation»).

Дубли на случай, если служба не стартовала:

- `Winlogon\Userinit` (запускается при любом входе, в т.ч. в safe mode);
- `HKLM\...\RunOnce`;
- скрипты на общем рабочем столе: `WOTBO - Defender off (safe mode).cmd`,
  `WOTBO - Defender restore (safe mode).cmd`, `WOTBO - undo safe mode.cmd`;
- аварийный `C:\WOTBO-UNDO-SAFE-MODE.bat`.

Все они берут фазу из `service-phase.txt` (`apply` или `restore`), поэтому после
взвода отката не применят твик повторно.

После успешного применения/отката движок сам снимает `safeboot`, удаляет службу,
хуки и задачи и перезагружает в обычный режим. При фатальной ошибке — тоже
снимает `safeboot` и возвращает в обычную загрузку.

## Откат

1. Пробует восстановить из обычного режима: переименовать папки обратно, импортировать
   реестр, включить задачи и брандмауэр, запустить службы.
2. Проверяет реальное состояние (`MSFT_MpComputerStatus`). Если `AntivirusEnabled`
   всё ещё 0 — взводит безопасный режим с фазой `restore`.
3. В safe mode под SYSTEM: ветки реестра восстанавливаются по схеме
   «удалить ветку → импортировать экспорт» (иначе политики, записанные твиком,
   остаются), папки переименовываются обратно, владелец возвращается
   (TrustedInstaller по SID).
4. После возврата в обычный режим создаётся одноразовая задача
   `\WOTBO\DefenderFinalize` (запуск через минуту, SYSTEM), которая ждёт
   `WinDefend`, включает `Set-MpPreference` и удаляет себя. Без неё
   `RealTimeProtectionEnabled` остаётся 0 до второй перезагрузки.

## Логи

```text
C:\Windows\Temp\wotbo-defender.log          — движок (виден и в safe mode)
C:\Windows\Temp\wotbo-defender-stage.log    — раннеры (start/exit)
C:\Windows\Temp\wotbo-defender-service.log  — служба безопасного режима
%ProgramData%\oixro\wotbo\defender\logs\    — пофазные логи
```

## Как определяется состояние

`Detect` (быстро, без WMI): `WinDefend.Start = 4` и `WdFilter.Start = 4`, служба не
запущена, либо журнал в состоянии `prepared/blocked` (взведён безопасный режим).

Проверка после apply/rollback: службы, процессы, `MSFT_MpComputerStatus`,
`AntivirusEnabled`, `RealTimeProtectionEnabled`, `AMServiceEnabled`.

## Ограничения

- `Tamper Protection` программно не отключается — на новых сборках всегда нужен
  безопасный режим.
- Windows Update может принести обновление платформы; службы и драйверы останутся
  `Start=4`, но после крупного апгрейда твик лучше проверить.
- `Smart App Control` после отключения повторно не включается (ограничение Windows).
- Папка `%ProgramData%\Microsoft\Windows Defender` может не переименоваться
  (её держит handle) — на отключение это не влияет, службы и драйверы уже выключены.
- BitLocker: перезагрузка в safe mode может запросить ключ восстановления.
- Брандмауэр после отключения выключен; BFE не останавливается, чтобы не ломать
  VPN/ICS.
