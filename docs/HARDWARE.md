# Железо и экран

`IWmiSystemInfoService` (`Services/Hardware`) заменил
`GetStringFromWMI/GetDoubleFromWMI/GetRAMSize/GetRAMFrequency` из `Form1`.

- `GetString(class, property)` — первое значение WMI или `""`;
- `GetDouble(...)` — число или `-1`;
- `GetRamSize()` — `"4 GB"`;
- `GetRamFrequency()` — `"3200 MHz"` или `""`.

`IDisplayService.GetScreenInfo()` — `"1920x1200 60 Hz"` через
`Screen.PrimaryScreen` и `GetDeviceCaps(VREFRESH)`.

Ошибки WMI/экрана пишутся в `DEBUG [Hardware]`, а не глотаются.

`LoadSystemInfoAsync` в `Form1` только собирает строку для `label7` и
логирует её в `[System]`; логика запросов вынесена в сервисы.

Файлы: `Services/Hardware/WmiSystemInfoService.cs`,
`Services/Hardware/DisplayService.cs`. Namespace — `WOTBO.Services.Hardware`,
чтобы не конфликтовать с `System.*`.
