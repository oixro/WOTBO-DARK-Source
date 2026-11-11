# Железо и экран

`IWmiSystemInfoService` (`Services/Hardware`) заменил
`GetStringFromWMI/GetDoubleFromWMI/GetRAMSize/GetRAMFrequency` из `Form1`.

- `GetString(class, property)` — первое значение WMI или `""`;
- `GetDouble(...)` — число или `-1`;
- `GetRamSize()` — `"4 GB"`;
- `GetRamFrequency()` — `"3200 MHz"` или `""`.

`IDisplayService.GetScreenInfo()` — `"1920x1200 60 Hz"` через
`Screen.PrimaryScreen` и `GetDeviceCaps(VREFRESH)`.

`GetSnapshotAsync()` собирает CPU/GPU/плату/RAM параллельно и запрашивает
только нужные поля (`SELECT Name, MaxClockSpeed FROM Win32_Processor` и т.д.),
а не `SELECT *`. На VM это занимает ~0.3 с вместо ~10 с; `LoadSystemInfoAsync`
обновляет `label7` без блокировки загрузки формы.

Ошибки WMI/экрана пишутся в `DEBUG [Hardware]`, а не глотаются.

`LoadSystemInfoAsync` в `Form1` только собирает строку для `label7` и
логирует её в `[System]`; логика запросов вынесена в сервисы.

Файлы: `Services/Hardware/WmiSystemInfoService.cs`,
`Services/Hardware/DisplayService.cs`. Namespace — `WOTBO.Services.Hardware`,
чтобы не конфликтовать с `System.*`.

## GPU

`IGpuService`:

- `GetName()` — имя GPU из WMI;
- `IsNvidia(name)` — содержит `NVIDIA`;
- `IsHdcpDisabled()` — наличие `RMHdcpKeyglobZero`;
- `IsNvidiaTweaksApplied()` — наличие `DisplayPowerSaving`;
- `IsAnselDisabledAsync()` — запускает `NvCameraEnable.exe` и читает состояние
  (`true`/`false`/`null`, если утилита не найдена);
- `SetAnsel(bool)` — `on`/`off` через прежний fire-and-forget запуск.

Определение Ansel больше не дублируется в `Form1` и откате: обе ветки
вызывают `AppServices.Gpu`.
