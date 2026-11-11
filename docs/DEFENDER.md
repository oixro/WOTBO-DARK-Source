# Defender

## Что делает `DefenderTweak`

1. Запускает прежний `.reg`-пакет (`win10defenderX.reg` или
   `win11defenderX.reg`) через `su.exe`.
2. На Windows 11 проходит по всем службам `webthreatdefusersvc_*` и ставит:
   - `LaunchProtected=4`
   - `Start=4`
   - `Start_old=4`
3. Применяет современные политики:
   - `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender`
     - `DisableAntiSpyware=1`
     - `DisableAntiVirus=1`
   - `...\Real-Time Protection`
     - `DisableRealtimeMonitoring=1`
     - `DisableBehaviorMonitoring=1`
     - `DisableOnAccessProtection=1`
     - `DisableScanOnRealtimeEnable=1`
   - `...\Spynet`
     - `SpynetReporting=0`
     - `SubmitSamplesConsent=2`
   - службы `WinDefend`, `WdNisSvc`, `Sense`, `SecurityHealthService` —
     `Start=4`.
4. Запускает `!AIO_OFF.reg`.

Откат запускает restore-пакеты, возвращает `webthreatdefusersvc_*`,
удаляет современные политики и возвращает службам `Start=2`.

## Важно: Tamper Protection

`DisableAntiSpyware` и политики Defender игнорируются, если включена
**Tamper Protection**. Реестром она не отключается — только вручную в
Windows Security или политикой организации. В лог пишется явное
предупреждение:

```text
Tamper Protection реестром не отключается; если она включена,
часть ключей может игнорироваться
```

Поэтому после применения может потребоваться перезагрузка и ручная
проверка состояния.

## Бэкапы

Перед каждым `.reg` вызывается `TweakContext.BackupRegFile`, затем
`TweakScope.Restore` возвращает значения и подветки. Современные политики
и службы сохраняются через обычный `RegistryService` (old → new).

`supercmd` сохранён: все защищённые операции идут через
`su.exe /wrs cmd.exe /c ...`.

## Ограничения

- Часть ключей принадлежит `TrustedInstaller`; ошибки записи логируются и
  не валят весь твик.
- Поведение Defender зависит от версии Windows и политик организации.
- Твик не гарантирует отключение на системах с включённой Tamper
  Protection.
