# Defender

## Решение: defendnot

`main.defender` использует [es3n1n/defendnot](https://github.com/es3n1n/defendnot)
(Apache-2.0). Это WSC-регистрация фейкового антивируса `WOTBO.PRO`,
а не переименование драйверов.

### Требование: интернет

Без интернета чекбокс `Отключить Windows Defender` отключается:
команда загружается с `https://dnot.sh/`.

## Применение

1. WOTBO открывает окно с просьбой отключить:
   - **Real-time protection**;
   - **Tamper Protection**;
   - **Smart App Control** (если доступно).
2. Сразу открываются страницы Windows Security:
   ```text
   windowsdefender://threatsettings/
   windowsdefender://appbrowser/
   ```
3. После подтверждения выполняется:
   ```powershell
   & ([ScriptBlock]::Create((irm https://dnot.sh/))) --name "WOTBO.PRO" --firewall --silent
   ```
4. WOTBO проверяет фактическое состояние Defender через WSC/WMI.

## Откат

```powershell
& ([ScriptBlock]::Create((irm https://dnot.sh/))) --disable
```

После команды проверяется, что Defender снова активен.

## Как определяется состояние

`Detect` смотрит:

1. зарегистрирован ли в `root\SecurityCenter2\AntiVirusProduct`
   продукт `WOTBO.PRO`;
2. если да — читает `MSFT_MpComputerStatus`:
   - `AntivirusEnabled`;
   - `RealTimeProtectionEnabled`.

Твик считается применённым только если fake AV зарегистрирован и Defender
не активен. Если WMI недоступен, наличие `WOTBO.PRO` считается признаком
отключённого Defender.

## Ограничения

- Нужен интернет для apply и rollback.
- Defendnot должен оставаться на диске: он добавляет себя в автозапуск.
- На новых Windows 11 может понадобиться отключить Smart App Control.
- Defendnot не удаляет файлы Defender; он работает через WSC.
