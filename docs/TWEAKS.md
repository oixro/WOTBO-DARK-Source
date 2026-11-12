# Модель твиков

## ITweak

```csharp
public interface ITweak
{
    string Id { get; }
    string Category { get; }
    TweakState Detect(TweakContext context);
    void Apply(TweakContext context);
    void Rollback(TweakContext context);
}
```

`TweakContext` содержит `IRegistryService` и `IAppLogger`. Твик не знает
о чекбоксах, стрелках и панелях — это остаётся в `Form1`.

`TweakCatalog` — словарь по `Id`, сейчас сорок восемь твиков.

## Пилот

| Id | Где | Значение |
|---|---|---|
| `ui.jpegquality` | `HKCU\Control Panel\Desktop` | `JPEGImportQuality=100` |
| `extra.iconcache` | `HKLM\...\CurrentVersion\Explorer` | `Max Cached Icons=4096` |
| `extra.stickykeys` | `HKCU\Control Panel\Accessibility\StickyKeys` | `Flags="506"` |
| `main.mpo` | `HKLM\SOFTWARE\Microsoft\Windows\Dwm` | `OverlayTestMode=5` |
| `extra.mobiletraffic` | `HKLM\...\Tcpip[6]\Parameters` | `DefaultTTL=0x41` |
| `extra.scoobe` | `HKCU\...\UserProfileEngagement` | `ScoobeSystemSettingEnabled=0` |
| `extra.dwmpriority` | `HKLM\...\Image File Execution Options\dwm.exe` | `CpuPriorityClass=1`, `IoPriority=0` |
| `pro.csrss` | `HKLM\...\Image File Execution Options\csrss.exe` + SystemProfile | `CpuPriorityClass=3`, `IoPriority=3`, `NoLazyMode=1`, `AlwaysOn=1` |
| `ui.contextmenu` | `HKCU\Software\Classes\CLSID\{86ca1aa0-...}\InprocServer32` | пустое значение `" "` |
| `main.dwminput` | `HKLM\...\CurrentVersion\Windows` | `DesktopHeapLogging=0`, `DwmInputUsesIoCompletionPort=0`, `EnableDwmInputProcessing=0` |
| `main.audio` | `HKLM\...\CurrentVersion\Audio` | `DisableSpatialOnComboEndpoints=1`, `DisableProtectedAudioDG=1`, `DisableProtectedAudio=1`; сам `.reg` идёт через `su.exe` |
| `main.powerlatency` | `HKLM\SYSTEM\CurrentControlSet\Control\Power` и `...\GraphicsDrivers\Power` | 17 + 37 DWORD-значений; detect по `ExitLatency=1` |
| `main.usb` | `SYSTEM\CurrentControlSet\Services\...`, `Enum\USB`, `usbflags`, `EnhancedStorageDevices` | USB power saving; `.cmd` по-прежнему запускается в Form1 |
| `main.mitigations` | `HKLM\...\Memory Management`, `...\Kernel`, `...\DeviceGuard\...` | Spectre/Meltdown; detect по `FeatureSettings=1` |
| `main.hibernate` | `powercfg -h off` | detect: отсутствует `C:\hiberfil.sys` |
| `main.pagefile` | `HKLM\...\Memory Management\PagingFiles` | `C:\pagefile.sys 16 32768` |
| `main.mousefix` | `HKCU\Control Panel\Mouse`, `HKU\.DEFAULT\Control Panel\Mouse` | кривые мыши и 1:1 без `.reg` |
| `main.powerscheme` | `powercfg -import/-setactive/-delete` | запоминает активную схему, проверяет импорт, откат возвращает её |
| `main.regpack` | `.reg`-пакеты regpack | w10/w11 AIO + foldernetwork + REG_AIO через `su.exe`/`regedit` |
| `main.defender` | `.reg`-пакеты + политики/службы 24H2+ | см. `docs/DEFENDER.md`; Tamper Protection не отключается реестром |
| `gpu.hdcp` | `HKLM\...\Class\{4d36e968-...}\0000` | `RMHdcpKeyglobZero=1` |
| `gpu.nvidia` | NVIDIA/Miracast/GraphicsDrivers/FTS | `DisplayPowerSaving`, `PlatformSupportMiracast`, `EnableTiledDisplay`, `EnableRID61684` |
| `extra.movetemp` | `HKCU\Environment` | `TEMP`/`TMP` → `%systemdrive%\Temp` |
| `gpu.directplay` | DISM | `Enable-Feature DirectPlay` / `Disable-Feature` |
| `pro.windowsupdate` | `updates.reg` | политики Windows Update |
| `pro.bufferbloat` | `netsh int tcp` | `autotuninglevel=disabled/normal` |
| `ui.buttons` | `HKCU\Control Panel\Desktop\WindowMetrics` | `CaptionHeight/Width=-270` |
| `ui.explorerrestart` | `HKCR\DesktopBackground\Shell\explorer` | пункт перезапуска проводника |
| `ui.win11header` | `HKCR\CLSID\{6480100b-...}\InProcServer32` | `FixByVlado` через `su.exe` |
| `ui.ffmpeg` | `C:\Windows\ffmpeg.exe` + SystemFileAssociations | последний essentials build через GitHub API |
| `gpu.ansel` | `NvCameraEnable.exe` | `on/off`; detect через утилиту |
| `extra.compactos` | `compact /CompactOS:always` | односторонняя |
| `extra.winsxs` | DISM StartComponentCleanup /ResetBase | односторонняя |
| `extra.updateclean` | SoftwareDistribution / DeliveryOptimization | односторонняя |
| `extra.tempclean` | `%TEMP%` | удаляет всё, кроме `wotbo*.log` |
| `extra.edge` | `HKLM\SOFTWARE\Policies\Microsoft\Edge` | 122 политики Edge + RestoreOnStartupURLs |
| `ui.explorerdetails` | `things.reg`/`un_things.reg`, `UseCompactMode` | мелкие настройки проводника; w10 — `Taskmgr_win10_*.reg` |
| `extra.onedrive` | `OneDriveSetup.exe /uninstall` | одностороннее удаление OneDrive |
| `pro.homegallery` | `HKLM\...\Explorer` + CLSID Home/Gallery | `HubMode=1`, `System.IsPinnedToNameSpaceTree=0`, CLSID через `su.exe` |
| `pro.unlocker` | `C:\Windows\unlocker`, `HKCR\*\shell\Уничтожить`, `Directory\shell\Уничтожить` | Unlocker в контекстном меню |
| `pro.alwaysontop` | `C:\Windows\AlwaysOnTop.exe`, `HKLM\...\Run` | автозапуск AlwaysOnTop |
| `ui.mica` | `C:\Windows\ExplorerBlurMica.dll`, `config.ini` | прозрачный проводник, latest `Maplespe/ExplorerBlurMica` |
| `ui.wotbocontext` | `HKCR\DesktopBackground\Shell\WOTBO`, `gear.ico`, `wotbo.exe` | пункт WOTBO в меню рабочего стола |
| `ui.bluefolders` | `imageres.dll`, `imageres.dll.mun` | синие папки; w10 — `imageres.zip`, w11 — `BlueIcon_Minimal.zip` |
| `ui.animations` | `HKCU\Software\oixro\wotbo` | `animationsEnabled=0`; анимация панелей WOTBO |
| `pro.win32priority` | `HKLM\...\PriorityControl` | значение выбирает диалог `Win32Priority`, откат → `2` |
| `main.mmagent` | prefetcher `.reg`, `smartctl`, `*-mmagent` | RAM/SSD-детект, `ApplicationPreLaunch`, `MC`/`PC`, `MOAF` |
| `main.reservedstorage` | DISM | `Set-ReservedStorageState Disabled/Enabled` |

`Form1` для них использует `TweakUiBinder` (`UI/TweakUiBinder.cs`):

```csharp
Bind("extra.edge", checkBox_edge, back_dop_edge, "Edge был применён");

// detect при старте
Binder("extra.edge").Detect();

// apply и rollback
Binder("extra.edge").Apply();
Binder("extra.edge").Rollback();
```

Binder сам вызывает `TweakScope`, `Detect/Apply/Rollback` и обновляет
`CheckBox`/`PictureBox`. Специфичные для UI шаги (диалоги, уведомления,
`animation = false`) передаются колбэками. Исключения — `gpu.ansel`
(ручная проверка `Unavailable`), `ui.contextmenu` (перезапуск проводника
с задержкой) и `main.pagefile` (try/catch вокруг apply/rollback).

Бэкапы и old → new по-прежнему делает `RegistryService` + `TweakScope`.
Бэкапы ключей/файлов, которые раньше делались в `Form1`, перенесены в
сами твики (`Audio`, `Usb`, `ContextMenu`, `Win11Header`, `ExplorerRestart`,
`DwmPriority`, `CsrssPriority`, `Edge`).

## Сверка состояния

`TweakUiBinder.Detect` при старте сверяет `Detect` с `TweakStateStore`.
Твики с фактическим detect (`bufferbloat`, `ui.buttons`, `main.mmagent`,
`main.mousefix`, `pro.homegallery`, `main.reservedstorage` и другие)
обновляют store автоматически. Store-only остаются только там, где
фактическое состояние проверить нельзя или это одноразовая операция.

Новый твик = класс `ITweak` + строка в `TweakCatalog.CreateDefault()` +
привязка UI в `MainForm.Binders.cs`.
