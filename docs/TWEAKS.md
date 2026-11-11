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

`TweakCatalog` — словарь по `Id`, сейчас шесть твиков.

## Пилот

| Id | Где | Значение |
|---|---|---|
| `ui.jpegquality` | `HKCU\Control Panel\Desktop` | `JPEGImportQuality=100` |
| `extra.iconcache` | `HKLM\...\CurrentVersion\Explorer` | `Max Cached Icons=4096` |
| `extra.stickykeys` | `HKCU\Control Panel\Accessibility\StickyKeys` | `Flags="506"` |
| `main.mpo` | `HKLM\SOFTWARE\Microsoft\Windows\Dwm` | `OverlayTestMode=5` |
| `extra.mobiletraffic` | `HKLM\...\Tcpip[6]\Parameters` | `DefaultTTL=0x41` |
| `extra.scoobe` | `HKCU\...\UserProfileEngagement` | `ScoobeSystemSettingEnabled=0` |

`Form1` для них:

- detect: `TweakCatalog.Get(id).Detect(TweakContext)`;
- apply: внутри `TweakScope.Apply(id)` — `tweak.Apply(...)`;
- rollback: внутри `TweakScope.Rollback(id)` — `tweak.Rollback(...)`.

Бэкапы и old → new по-прежнему делает `RegistryService` + `TweakScope`.

## Дальше

Переносить остальные твики по категориям, сохраняя порядок применения:

1. Main
2. GPU
3. UI
4. Extra
5. Pro

Новый твик = класс `ITweak` + строка в `TweakCatalog.CreateDefault()` +
привязка UI в `Form1` (позже — `TweakUiBinder`).
