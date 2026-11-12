# DPI и масштабирование

## Проблема

При масштабировании Windows 125/150 % окно WOTBO выглядело мыльным:
процесс был DPI-unaware, поэтому Windows растягивала готовый растровый
кадр, а не перерисовывала интерфейс в физических пикселях.

## Решение

В `app.manifest` включён DPI awareness:

```xml
<application xmlns="urn:schemas-microsoft-com:asm.v3">
  <windowsSettings>
    <dpiAware xmlns="http://schemas.microsoft.com/SMI/2005/WindowsSettings">true</dpiAware>
    <longPathAware xmlns="http://schemas.microsoft.com/SMI/2016/WindowsSettings">true</longPathAware>
  </windowsSettings>
</application>
```

В `App.config` включено автоматическое масштабирование WinForms:

```xml
<appSettings>
  <add key="EnableWindowsFormsHighDpiAutoResizing" value="true"/>
</appSettings>
```

Дизайнерские контролы масштабируются через `AutoScaleMode.Dpi`
(`AutoScaleDimensions = 96x96`). Координаты и размер окна, которые
задаются в коде, пересчитываются через `WOTBO.UI.Dpi`:

```csharp
Location = Dpi.Scale(this, 20, 85);
Size = Dpi.ScaleSize(this, 15, 15);
```

`Dpi.Factor` берёт `Control.DeviceDpi / 96`. Это используется в:

- размере окна: `ApplyWindowSize()` берёт фактические границы `PanelMain`;
- `PanelNavigator` — `405 → 105`, верх `36`;
- дополнительных элементах вкладки «Интерфейс»;
- расположении `DirectPlay` без NVIDIA;
- кнопках навигации `195,284`.

Размер окна задаётся в `Load` и по `DpiChanged`, после автоскейла WinForms:
`ClientSize = (PanelMain.Right, PanelMain.Bottom)`. Это исключает обрезку
правой колонки, потому что `PanelMain` шире базовых `390×305`.

## Ограничения

- Масштаб, изменённый **во время работы** приложения, применяется после
  перезапуска WOTBO. Это поведение DPI-aware WinForms-приложения.
- При переносе окна на монитор с другим масштабом WinForms пересчитывает
  контролы; координаты `PanelNavigator` берутся из `DeviceDpi` в момент
  перехода.
- Проверено на 100 % и 200 %: при 200 % окно становится `790×612`,
  интерфейс полностью помещается и остаётся чётким.
- На 125/150 % действует та же формула `390×factor` / `305×factor`.
