# Defender

## Что делает `DefenderTweak`

Твик заменён на [pgkt04/defender-control](https://github.com/pgkt04/defender-control)
(MIT). WOTBO не хранит бинарник внутри себя: при применении скачивается
последний `disable-defender.exe` из GitHub Releases, при откате —
`enable-defender.exe`.

Применение:

1. скачивает `disable-defender.exe` в `c:\Windows\oixro`;
2. запускает `disable-defender.exe -s`;
3. инструмент перезапускается от `TrustedInstaller`;
4. отключает политики и службы Defender;
5. переименовывает драйверы `WdFilter.sys`, `WdBoot.sys` и движок
   Defender в `.OLD`;
6. сохраняет restore-манифест:
   ```text
   C:\ProgramData\defender-control\soft-deleted.txt
   ```

Для полного эффекта нужна перезагрузка.

## Detect

`main.defender` считается применённым, если существует restore-манифест:

```text
C:\ProgramData\defender-control\soft-deleted.txt
```

Если пользователь применял старый WOTBO 2.33/2.34, где Defender
отключался `.reg`-пакетами, манифеста нет → твик снова доступен для
применения.

## Откат

1. скачивает `enable-defender.exe`;
2. запускает `enable-defender.exe -s`;
3. инструмент возвращает `.OLD`-файлы, политики, владельцев и службу
   `WinDefend`;
4. после перезагрузки Defender должен вернуться.

## Важно

- **Tamper Protection** нужно выключить вручную:
  Windows Security → Virus & threat protection → Manage settings.
- **Real-time protection** лучше временно выключить: Defender помечает
  `defender-control` как `HackTool:Win32/DefenderControl` и может удалить
  скачанный exe.
- После применения и отката нужна **перезагрузка**.
- Инструмент работает с драйверами и `TrustedInstaller`. На снапшоте VM
  это безопасно, на рабочей системе — на свой риск.
- Если Defender не вернулся:
  ```text
  DISM /Online /Cleanup-Image /RestoreHealth
  sfc /scannow
  ```
  затем перезагрузка. В крайнем случае — repair upgrade Windows.

## Что удалено из старого твика

Старые `.reg`-пакеты `win10defenderX.reg` / `win11defenderX.reg`,
`webthreatdefusersvc_*`, `!AIO_OFF.reg` и ручные политики больше не
используются для `main.defender`. Их заменяет `defender-control`.

## Бэкапы

WOTBO не делает свой бэкап файлов Defender: восстановление выполняет
`enable-defender.exe` по своему манифесту. В `TweakStateStore` для
`main.defender` остаются только `applied` и `appliedAt`.
