# План рефакторинга WOTBO

WinForms · .NET Framework 4.8 · C# · Windows-only · **x64**

> **Статус:** этап **P0 (окружение и сборка) завершён** — см. `docs/P0-RESULT.md`.
> P1 в работе: добавлены `AppPaths`, логгер (`docs/LOGGING.md`),
> `RegistryService` (`docs/REGISTRY.md`) и постоянные бэкапы
> (`docs/STATE.md`); все вызовы `writelog()` переведены на `Log.*` с уровнями
> и категориями `Tweak:<id>`; `DEBUG` пишется всегда; apply/rollback обёрнуты
> в `TweakScope`; записи реестра и файлы сохраняются для отката, старые флаги
> мигрируют в `HKCU\Software\oixro\wotbo\Backups`; версия — 2.34.
> Рабочий цикл: код в VSCode на macOS, сборка и запуск в Windows 11 VM
> (`docs/VM-SETUP.md`). Проект остаётся classic `.csproj` с `Debug|x64` /
> `Release|x64`; манифест — `requireAdministrator`; WOTBO собирается только под
> x64 и запускается в VM через эмуляцию на Apple Silicon.

Цель: разложить приложение на слои **UI → Services → Helpers**, сохранив текущий UI,
все твики, порядок их применения, тексты, ресурсы и поведение. Рефакторинг выполняется
поэтапно, после каждого этапа приложение должно собираться и работать.

---

## 1. Аудит текущего состояния

### 1.1. Файлы

| Файл | Строк | Роль |
|---|---:|---|
| `Form1.cs` | 3452 | God object: UI + реестр + WinAPI + WMI + сеть + процессы + логика твиков |
| `Form1.Designer.cs` | 2989 | 15 панелей, ~53 CheckBox, ~50 PictureBox-откатов, ~80 Label |
| `Form1.resx` | — | ресурсы формы |
| `Form2.cs` | 85 | EULA + миграция старого ключа реестра |
| `Form4.cs` | 32 | окно MSI Mode |
| `Win32Priority.cs` | 27 | выбор `Win32PrioritySeparation` |
| `FormShadow.cs` | 63 | P/Invoke DWM, тень, запрет разворачивания |
| `InstanceChecker.cs` | 18 | мьютекс одного экземпляра |
| `Internet.cs` | 20 | проверка сети через DNS |
| `Program.cs` | 68 | точка входа + глобальный обработчик исключений |
| `Properties/Resources.*` | — | 20 встроенных ресурсов (.reg/.bat/.zip/.exe/.pow/.nip) |
| `Project.csproj` | 195 | classic csproj, x64 Release, .NET 4.8 |

### 1.2. Главные проблемы

1. **God object.** `Form1` совмещает представление, навигацию, бизнес-логику,
   доступ к ОС и инфраструктуру. Любое изменение твика затрагивает форму.
2. **Дублирование инфраструктуры.**
   - `hcmd/scmd/powershell/supercmd` продублированы в `Form1`, `Form2`, `Form4`;
   - `tempfolder` и путь к логу захардкожены в нескольких местах;
   - `writelog` есть только в `Form1`, остальные формы пишут иначе.
3. **Тройное описание состояния твика.** Применение, откат и проверка при старте —
   три независимых куска кода с магическими строками реестра. Любое расхождение =
   «чекбокс активен, хотя твик применён» или наоборот.
4. **Синхронный I/O в UI-потоке.** `WebClient.DownloadFile`, `Process.WaitForExit`,
   `StandardOutput.ReadToEnd()` вызываются из обработчиков и `Load`.
5. **Статическая инициализация с побочным эффектом.** Поле
   `getReservedStorage = Process.Start(DISM...)` запускает DISM при первом обращении
   к типу, до `Load`, без обработки ошибок.
6. **Пустые `catch { }`** скрывают ошибки (сеть, процессы, реестр).
7. **Небезопасная работа с реестром.** Ключи не освобождаются, во многих местах нет
   проверки на `null` (например, `...\Windows\Dwm`), `Convert.ToInt32(null)`
   неявно даёт `0`.
8. **Магические строки и числа.** Пути реестра, URL, GUID, флаги, координаты панелей
   (`405/105/36`, `195/284`), задержки (`7/15/125/1000/3000`) размазаны по коду.
9. **Смешение языков и назначений.** `test1`, `Project`, `WOTBO` в namespace;
   `Form1/Form2/Form4`; имена контролов `checkBox_mtiititit`, `back_main_16`.
10. **Мёртвый код и артефакты.** `checkBox1` («Отключить снижение громкости микрофона»)
    есть в дизайнере, но не используется; пустые обработчики `back_pro_1/4/6/7/9`.
11. **Хрупкое обновление.** Скачивание нового exe, `taskkill`, `del`/`rd` и перезапуск
    одной cmd-строкой; сравнение версий через `double`.
12. **Нет тестов и контрактов.** Проверить логику можно только запуском на Windows.

---

## 2. Целевая структура папок

```text
WOTBO-DARK-Source/
├─ WOTBO.sln
├─ Project.csproj                  # пока остаётся classic csproj
├─ App.config
├─ app.manifest
├─ Program.cs                      # только composition root + глобальный обработчик
│
├─ App/                            # инфраструктура приложения
│  ├─ AppPaths.cs                  # все абсолютные пути, log, temp, contexpath, URL
│  ├─ AppBootstrap.cs              # порядок инициализации при старте
│  ├─ InstanceGuard.cs             # бывший InstanceChecker
│  └─ Logging/
│     ├─ IAppLogger.cs
│     └─ FileAppLogger.cs          # тот же формат: "yyyy-MM-dd HH:mm:ss.ffff - line"
│
├─ Models/                         # DTO/контракты между слоями (без логики)
│  ├─ WindowsVersionInfo.cs
│  ├─ SystemInfo.cs
│  ├─ GpuInfo.cs
│  ├─ TweakState.cs
│  ├─ TweakCategory.cs
│  └─ ProcessResult.cs
│
├─ Services/
│  ├─ Registry/
│  │  ├─ IRegistryService.cs
│  │  ├─ RegistryService.cs        # null-safe, using, DWord/String/MultiString
│  │  ├─ RegistryPaths.cs          # константы путей реестра
│  │  └─ AppStateStore.cs          # HKCU\Software\oixro\wotbo, типизированные флаги
│  ├─ Processes/
│  │  ├─ IProcessRunner.cs
│  │  └─ ProcessRunner.cs          # бывшие hcmd/scmd/powershell/supercmd/OpenUrl
│  ├─ System/
│  │  ├─ IWindowsVersionService.cs
│  │  ├─ WindowsVersionService.cs
│  │  ├─ ISystemInfoService.cs
│  │  ├─ WmiSystemInfoService.cs   # CPU/GPU/RAM/плата/экран/Гц
│  │  ├─ IDisplayService.cs
│  │  ├─ DisplayService.cs         # GetDC/GetDeviceCaps
│  │  ├─ IGpuService.cs
│  │  ├─ GpuService.cs             # NVIDIA, Ansel, HDCP
│  │  └─ IReservedStorageService.cs
│  │      ReservedStorageService.cs
│  ├─ Network/
│  │  ├─ IInternetChecker.cs
│  │  ├─ InternetChecker.cs        # бывший Internet
│  │  ├─ IDownloadService.cs
│  │  ├─ DownloadService.cs        # async, прогресс, отмена
│  │  └─ IUpdateService.cs
│  │      GitHubUpdateService.cs   # ver.json, lastchange.json, WOTBO.exe
│  ├─ Storage/
│  │  ├─ TempWorkspace.cs          # c:\Windows\oixro + fallback, все пути внутри
│  │  ├─ IArchiveService.cs
│  │  ├─ ArchiveService.cs         # ZipFile
│  │  └─ EmbeddedResourceProvider.cs
│  └─ Tweaks/                      # прикладная логика твиков
│     ├─ ITweak.cs
│     ├─ TweakBase.cs
│     ├─ TweakContext.cs
│     ├─ TweakCatalog.cs
│     └─ Categories/
│        ├─ MainTweaks.cs
│        ├─ GpuTweaks.cs
│        ├─ UiTweaks.cs
│        ├─ ExtraTweaks.cs
│        └─ ProTweaks.cs
│
├─ Helpers/
│  ├─ Win32/
│  │  ├─ NativeMethods.cs          # user32, gdi32, dwmapi (P/Invoke)
│  │  └─ DpiHelper.cs
│  ├─ Threading/
│  │  └─ AsyncHelper.cs            # безопасный async void на границе события
│  └─ Extensions/
│     ├─ RegistryExtensions.cs
│     └─ ControlExtensions.cs
│
├─ UI/
│  ├─ Forms/
│  │  ├─ MainForm.cs (+ .Designer.cs, .resx)      # бывший Form1
│  │  ├─ EulaForm.cs (+ .Designer.cs)             # бывший Form2
│  │  ├─ MsiModeForm.cs (+ .Designer.cs, .resx)   # бывший Form4
│  │  ├─ Win32PriorityForm.cs (+ .Designer.cs, .resx)
│  │  └─ ShadowForm.cs                            # бывший FormShadow (базовая форма)
│  ├─ Navigation/
│  │  └─ PanelNavigator.cs         # ChangePanel, видимость nav-панелей, координаты
│  ├─ Theming/
│  │  ├─ FormPainter.cs            # FormPaint
│  │  ├─ HoverAnimator.cs          # анимация цвета Label
│  │  └─ WindowDragBehavior.cs     # перетаскивание за шапку
│  └─ Notifications/
│     └─ BalloonNotifier.cs        # NotifyIcon + таймер
│
├─ Properties/                     # НЕ трогаем на первом этапе
│  ├─ AssemblyInfo.cs
│  ├─ Resources.resx
│  ├─ Resources.Designer.cs
│  ├─ Settings.settings
│  └─ Settings.Designer.cs
│
└─ Resources/                      # встроенные .reg/.bat/.zip/.exe/.pow/.nip и т.д.
```

### Почему так

- `Properties/Resources.resx` и `Resources/` остаются на месте: имена embedded-ресурсов
  (`WOTBO.Properties.Resources.regpack` и т.п.) не меняются, дизайнер не ломается.
- Папки = namespace: `WOTBO.UI.Forms`, `WOTBO.Services.Registry`, `WOTBO.Helpers.Win32`.
- `Models/` — не отдельный слой, а тонкие DTO без зависимостей.
- Правило зависимостей: `UI → Services → Helpers/Models`. Обратных зависимостей нет.
  `Services` не знает про `CheckBox`, `MessageBox`, `Panel`.

---

## 3. Слои и ответственность

### UI (формы)

| Класс | Ответственность |
|---|---|
| `MainForm` | Только `InitializeComponent`, подписка событий, жизненный цикл, вызовы presenter/services |
| `PanelNavigator` | Показ панели, скрытие остальных, позиции `(405,36) → (105,36)`, `(195,284)`, easing |
| `TweakUiBinder` | Связка `CheckBox + PictureBox-откат + ITweak`: Detect при старте, Apply, Rollback, Enable/Visible |
| `FormPainter` | Градиент формы (те же цвета и позиции ColorBlend) |
| `HoverAnimator` | Плавная смена `ForeColor` Label (те же 192/255, 15 мс) |
| `WindowDragBehavior` | Перетаскивание за `LabelHead/PanelHead/logo` |
| `BalloonNotifier` | `NotifyIcon`, 10 000 мс, скрытие через 10 500 мс |
| `EulaForm`, `MsiModeForm`, `Win32PriorityForm` | Прежнее поведение, без прямого реестра/процессов |

UI не содержит путей реестра, команд cmd/powershell, URL и бизнес-правил.
Тексты сообщений и подписи контролов остаются прежними (их можно вынести в ресурсы
позже, отдельным этапом).

### Services

| Группа | Сервисы | Что заменяют |
|---|---|---|
| Registry | `RegistryService`, `RegistryPaths`, `AppStateStore` | все `Registry.*` вызовы, `Convert.ToInt32(...GetValue(...)) == 1` |
| Processes | `ProcessRunner` | `hcmd`, `scmd`, `powershell`, `supercmd`, `OpenUrl`, `InstallCursor` |
| System | `WindowsVersionService`, `WmiSystemInfoService`, `DisplayService`, `GpuService`, `ReservedStorageService` | версия Windows, WMI, `GetDeviceCaps`, NVIDIA/Ansel/HDCP, DISM |
| Network | `InternetChecker`, `DownloadService`, `GitHubUpdateService` | `Internet.OK`, `WebClient`, проверка/скачивание обновлений |
| Storage | `TempWorkspace`, `ArchiveService`, `EmbeddedResourceProvider` | `tempfolder`, все `path_*`, `ZipFile`, `Resources.*` |
| Tweaks | `ITweak` + категории + `TweakCatalog` | ~40 блоков `if (checkBox_*.Checked)` и ~50 `back_*_Click` |
| App | `AppPaths`, `AppBootstrap`, `InstanceGuard`, `FileAppLogger` | статические поля, mutex, лог |

### Helpers

- `NativeMethods` — единственное место для `DllImport` (`user32`, `gdi32`, `dwmapi`).
- `DpiHelper` — проверка масштаба 100 % для mousefix.
- `AsyncHelper` — `SafeFireAndForget`, чтобы `async void` оставался только в событиях.
- Расширения — `TryGetInt`, `GetValueOrDefault`, `SetDoubleBuffered`.

---

## 4. Карта миграции

| Сейчас | Станет |
|---|---|
| `Form1.cs` (оркестрация) | `UI/Forms/MainForm.cs` (тонкий) + `Services/Tweaks/*` |
| `Form1.cs` (`hcmd`, `scmd`, `powershell`, `supercmd`, `OpenUrl`) | `Services/Processes/ProcessRunner.cs` |
| `Form1.cs` (`writelog`, `logLock`) | `App/Logging/FileAppLogger.cs` |
| `Form1.cs` (`SysFolder`) | `App/AppPaths.cs` |
| `Form1.cs` (`tempfolder`, `path_*`) | `Services/Storage/TempWorkspace.cs` |
| `Form1.cs` (`GetStringFromWMI`, `GetRAMSize`, `GetScreenInfo`, `GetRefreshRate`) | `Services/System/WmiSystemInfoService.cs`, `DisplayService.cs` |
| `Form1.cs` (версия Windows, `win10`) | `Services/System/WindowsVersionService.cs` + `Models/WindowsVersionInfo` |
| `Form1.cs` (`getReservedStorage`, `ReservedStorage`) | `Services/System/ReservedStorageService.cs` |
| `Form1.cs` (проверка NVIDIA/Ansel/HDCP) | `Services/System/GpuService.cs` |
| `Form1.cs` (обновление через WebClient) | `Services/Network/GitHubUpdateService.cs`, `DownloadService.cs` |
| `Form1.cs` (`ChangePanel`) | `UI/Navigation/PanelNavigator.cs` |
| `Form1.cs` (`FormPaint`, анимации Label, drag) | `UI/Theming/*`, `UI/Notifications/*` |
| `Form1.cs` (~40 apply-блоков) | `Services/Tweaks/Categories/*` (`ITweak.ApplyAsync`) |
| `Form1.cs` (~50 `back_*_Click`) | `ITweak.RollbackAsync` |
| `Form1.cs` (большой `Load`-блок проверок) | `ITweak.Detect` + `TweakUiBinder` |
| `Form2.cs` | `UI/Forms/EulaForm.cs` + `AppStateStore.IsEulaAccepted` |
| `Form4.cs` | `UI/Forms/MsiModeForm.cs` + `ProcessRunner` |
| `Win32Priority.cs` | `UI/Forms/Win32PriorityForm.cs` + `RegistryService` |
| `FormShadow.cs` | `UI/Forms/ShadowForm.cs` (P/Invoke в `Helpers/Win32/NativeMethods.cs`) |
| `InstanceChecker.cs` | `App/InstanceGuard.cs` |
| `Internet.cs` | `Services/Network/InternetChecker.cs` |
| `Program.cs` | `Program.cs` + `AppBootstrap.cs` + `FileAppLogger` |
| `Properties/*`, `Resources/*` | без изменений |

---

## 5. Ключевая модель: ITweak

Вместо трёх независимых кусков кода на каждый твик — один объект, который умеет
проверить состояние, применить и откатить.

```csharp
public enum TweakState { Unknown, Applied, NotApplied, Unavailable }

public interface ITweak
{
    string Id { get; }                       // "main.regpack", "ui.bluefolders"
    TweakCategory Category { get; }
    bool IsSupported(TweakContext ctx);      // win10/win11, NVIDIA, интернет
    TweakState Detect(TweakContext ctx);     // проверка при старте
    Task ApplyAsync(TweakContext ctx, CancellationToken ct);
    Task RollbackAsync(TweakContext ctx, CancellationToken ct);
}
```

`TweakContext` — снимок среды: `IsWindows10`, `IsNvidia`, `Workspace`, `Logger`,
`Registry`, `Processes`, `Downloads`, `IsOnline`, `AnimationsEnabled`.

Пример реализации:

```csharp
public sealed class JpegQualityTweak : TweakBase
{
    public override string Id => "ui.jpeg-quality";
    public override TweakCategory Category => TweakCategory.Ui;

    public override TweakState Detect(TweakContext ctx) =>
        ctx.Registry.ReadInt(RegistryHive.CurrentUser,
            RegistryPaths.Desktop, "JPEGImportQuality", 0) == 100
            ? TweakState.Applied : TweakState.NotApplied;

    public override Task ApplyAsync(TweakContext ctx, CancellationToken ct)
    {
        ctx.Registry.WriteInt(RegistryHive.CurrentUser,
            RegistryPaths.Desktop, "JPEGImportQuality", 100);
        return Task.CompletedTask;
    }

    public override Task RollbackAsync(TweakContext ctx, CancellationToken ct)
    {
        ctx.Registry.DeleteValue(RegistryHive.CurrentUser,
            RegistryPaths.Desktop, "JPEGImportQuality");
        return Task.CompletedTask;
    }
}
```

Связка с UI убирает ~50 повторов «выключить чекбокс + показать картинку»:

```csharp
public sealed class TweakUiBinder
{
    public TweakUiBinder(CheckBox checkBox, PictureBox rollback, ITweak tweak) { /* ... */ }

    public void Refresh(TweakContext ctx)      // Detect → Enable/Visible/Checked
    public Task ApplyAsync(TweakContext ctx)   // Apply → Refresh
    public Task RollbackAsync(TweakContext ctx)// Rollback → Refresh
}
```

`MainForm` в итоге содержит таблицу из ~50 строк «контрол → твик» вместо
тысяч строк ветвлений.

---

## 6. Поэтапный план

### Этап 0. Базовая линия

- Ветка `refactor/architecture`, тег/коммит `baseline-2.33`.
- Собрать Release x64 на Windows, прогнать smoke-тест:
  первый запуск (EULA), Win10, Win11, NVIDIA, без NVIDIA, online/offline.
- Зафиксировать эталонный `wotbo.log` после «применить всё» и «откатить всё».
- Зафиксировать скриншоты всех панелей.
- **DoD:** сборка проходит, поведение зафиксировано.

### Этап 1. Инфраструктура (без изменения поведения)

- Добавить `AppPaths`, `FileAppLogger`, `ProcessRunner`, `RegistryService`,
  `RegistryPaths`, `AppStateStore`, `DownloadService`, `TempWorkspace`,
  `InternetChecker`, `InstanceGuard`.
- Заменить дубли в `Form1`, `Form2`, `Form4`, `Win32Priority`.
- Удалить локальные `hcmd/scmd/powershell/supercmd/writelog`, статические `tempfolder`.
- Сохранить **точно** формат лога, аргументы cmd, флаги `CreateNoWindow`,
  `UseShellExecute`, порядок вызовов и fire-and-forget там, где он был.
- **DoD:** diff логов до/после совпадает (кроме времени), UI не изменился,
  все твики применяются и откатываются.

### Этап 2. Системные и сетевые сервисы

- `WindowsVersionService` → `WindowsVersionInfo` (без `MessageBox` внутри).
- `WmiSystemInfoService`, `DisplayService`, `GpuService`.
- `ReservedStorageService`: убрать запуск DISM из инициализатора поля, вызывать
  асинхронно в `Load`, логировать ошибки, сохранить прежнюю проверку строк
  (включая русскую локализацию).
- `GitHubUpdateService`: те же URL и сравнение версий; UI получает результат
  и сам показывает `MessageBox`.
- **DoD:** информация о ПК, определение Win10/11, NVIDIA/Ansel, проверка обновлений
  работают как раньше; UI не блокируется.

### Этап 3. Модель твиков (пилот + категории)

- Добавить `ITweak`, `TweakBase`, `TweakContext`, `TweakCatalog`.
- Пилот на 3 простых твиках: `JPEGImportQuality`, `Max Cached Icons`, `StickyKeys`.
- Затем перенести по категориям, сохраняя порядок применения:
  1. `MainTweaks`: defender, regpack, DISM, hibernate, power scheme, MPO, USB,
     mitigations, mmagent, pagefile, mousefix, audio, dwm input, power latency,
     Win32Priority.
  2. `GpuTweaks`: Ansel, HDCP, NVIDIA tweaks, DirectPlay.
  3. `UiTweaks`: caption buttons, explorer things, JPEG, blue folders, Win11 context
     menu, Win11 header, WOTBO context menu, explorer restart, FFmpeg, Mica, animations.
  4. `ExtraTweaks`: OneDrive, CompactOS, WinSxS, temp clean, update clean, icon cache,
     mobile traffic, SCOOBE, StickyKeys, DWM priority, Edge, move temp.
  5. `ProTweaks`: Home/Gallery, Windows Update, CSRSS, Bufferbloat, Unlocker,
     AlwaysOnTop.
- Каждый твик: `Detect`, `Apply`, `Rollback`, без ссылок на контролы.
- **DoD:** для каждого твика проверено: detect до/после, apply, rollback,
  повторный apply; состояние чекбокса и стрелки отката совпадает с реестром.

### Этап 4. UI-инфраструктура

- `PanelNavigator` — те же панели, координаты, длительности, easing.
- `TweakUiBinder` — привязка чекбоксов и PictureBox.
- `FormPainter`, `HoverAnimator`, `WindowDragBehavior`, `BalloonNotifier`.
- `Helpers/Win32/NativeMethods` — собрать все `DllImport`.
- **DoD:** визуально идентично эталонным скриншотам; анимации и перетаскивание
  работают; уведомления те же.

### Этап 5. Разгрузка MainForm

- `MainForm` содержит только: `InitializeComponent`, создание сервисов,
  подписки, `Load`-шаги через `AppBootstrap`, вызовы binder/navigator.
- `Load` разбить на упорядоченные шаги:
  1. лог/версия, 2. EULA, 3. версия Windows, 4. workspace,
  5. detect состояния твиков, 6. интернет/обновление, 7. GPU,
  8. распаковка ресурсов, 9. legacy-cleanup.
- `async void` — только обработчики событий; внутри `try/catch` + лог.
- Цель: `MainForm.cs` < 500 строк.
- **DoD:** сценарии первого запуска и повторного запуска идентичны baseline.

### Этап 6. Переименование и namespace (опционально, после стабилизации)

- `Form1 → MainForm`, `Form2 → EulaForm`, `Form4 → MsiModeForm`,
  `FormShadow → ShadowForm`, `Win32Priority → Win32PriorityForm`.
- Namespace `test1/Project/WOTBO` → `WOTBO.*`.
- Обновить `.csproj` (`Compile Include`, `DependentUpon`, `.resx`) и `Program.cs`.
- **DoD:** дизайнер открывается, сборка и запуск проходят.

### Этап 7. Качество и найденные баги (после паритета)

- Убрать пустые `catch`, добавить логирование.
- Null-safe реестр, `using` для ключей, таймауты процессам и загрузкам.
- Отдельным коммитом — баги из раздела 10.
- **DoD:** каждый баг закрыт отдельным проверяемым изменением.

### Этап 8. Сборка и проверки

- Оставить classic csproj (WinForms Designer + .NET 4.8).
- Добавить `.editorconfig`, при желании — Windows CI (сборка Release x64).
- Обновить README с архитектурой и правилами добавления твика.
- **DoD:** новый твик добавляется одним классом + одной строкой в каталоге и binder.

---

## 7. Правила сохранения UI и функционала

1. **Не менять `*.Designer.cs` и `*.resx`** на этапах 1–5, кроме переноса файлов.
   Имена контролов, координаты, размеры, порядок `Controls.Add` — как есть.
2. **Не менять тексты** сообщений, подписей, заголовков, URL и имена ресурсов.
3. **Не менять порядок** применения твиков и порядок команд: defender → regpack →
   DISM → …; explorer перезапускается там же, где и раньше.
4. **Не менять формат лога** и путь `%temp%\wotbo.log`.
5. **Не менять ветки реестра**, типы значений и флаги; только централизовать строки.
6. **Не менять поведение откатов**, включая пустые обработчики (сначала сохранить,
   потом удалять осознанно).
7. **Не менять EULA-поток** и логику единственного экземпляра.
8. Каждый этап — отдельная ветка/PR с чек-листом и diff логов.

---

## 8. Риски и ограничения

| Риск | Митигация |
|---|---|
| Сборка возможна только на Windows (.NET Framework 4.8, WinForms) | эталонная Windows VM, MSBuild/VS 2022, ручной smoke-тест |
| Перенос форм ломает `.resx`/дизайнер | переносить через `git mv` + правка namespace; переименование — отдельный этап |
| Embedded-ресурсы зависят от namespace/папки | `Properties/Resources.resx` не трогать |
| Асинхронность меняет порядок побочных эффектов | сохранять `await`/`Task.Delay` ровно там, где было; проверять лог |
| `hcmd` был fire-and-forget | на этапе 1 сохранить семантику, исправление — на этапе 7 |
| Реестр требует прав администратора | `app.manifest: requireAdministrator`; запуск только на VM со снапшотом |
| Антивирус/Defender реагирует на действия | тестовая VM со снапшотом |
| Потеря состояния между запусками | `AppStateStore` + тест detect/apply/rollback |

---

## 9. Чек-лист приёмки

- [ ] Release x64 собирается без новых warning-ов.
- [ ] Первый запуск: EULA → принятие → главное окно.
- [ ] Повторный запуск: EULA не показывается.
- [ ] Win10 и Win11: версия определяется, недоступные чекбоксы отключены.
- [ ] NVIDIA: Ansel/HDCP/NVIDIA tweaks видны; без NVIDIA — скрыты, DirectPlay сдвинут.
- [ ] Online: обновление проверяется; offline: сообщение и блокировки как раньше.
- [ ] «Применить всё» (F11) включает те же твики.
- [ ] Каждый твик: apply → detect = Applied → rollback → detect = NotApplied.
- [ ] Все 15 панелей и навигация визуально совпадают со скриншотами.
- [ ] Лог содержит те же команды/ошибки в том же формате.
- [ ] Закрытие: временная папка удаляется, mutex освобождается.
- [ ] `MainForm.cs` < 500 строк, в UI нет `Registry.*`, `Process.Start`, URL.

---

## 10. Backlog найденных багов (чинить после паритета)

1. `Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm").GetValue(...)`
   без null-check → `NullReferenceException`, если ключа нет.
2. `...\PriorityControl` и ряд других ключей открываются без проверки на null.
3. Обновление: `rd "{exepath}" /s /q` применяется к файлу, а не папке;
   удаление запущенного exe и `C:\Windows\wotbo.exe` одной строкой — хрупко.
4. `label_msimode_Click`: при отсутствии `MSI_util_v3.exe` распаковывается
   `cursors.zip` вместо архива MSI-утилиты.
5. `label_delete_defender_Click`: ветка «zip уже есть» удаляет архив и запускает
   bat, после чего bat запускается ещё раз — двойной запуск.
6. `checkBox_ffmpeg`: скачивается `ffmpeg.reg`, а распаковывается `ffmpeg.zip`.
7. `checkBox_pro_13`: проверяется `Directory.Exists("...unlocker.zip")` вместо
   `File.Exists`, поэтому архив качается каждый раз.
8. `Form1_Load`: `winver` может быть null; `GetValue` без проверки.
9. `hcmd`: `using (Process)` завершается сразу после `BeginOutputReadLine`,
   процесс может быть освобождён во время чтения вывода.
10. `getReservedStorage` — запуск DISM в инициализаторе статического поля,
    процесс не освобождается, исключение ломает инициализацию типа.
11. `notifyIcon` и `Timer` не освобождаются при закрытии формы.
12. `checkBox1` («Отключить снижение громкости микрофона») — мёртвый контрол.
13. Пустые обработчики `back_pro_1/4/6/7/9` — либо реализовать, либо убрать.
14. `checkBox_animation` отключает только анимации приложения, не Windows.
15. ~~`Program.Exception`: лог открывается параллельно с `FileAppLogger`.~~
    Решено на P1: единый `IAppLogger` под `lock`.
16. Версии сравниваются как `double`; `2.4` и `2.40` неотличимы — перейти на
    `Version`/`SemVer` (отдельным изменением, с сохранением поведения).
17. `Internet.OK()` через `Dns.GetHostEntry("github.com")` может висеть;
    нужен таймаут/`CancellationToken`.

---

## 11. Первый шаг

Начать с **этапа 1**: добавить инфраструктурные сервисы и заменить дубли,
не трогая ни дизайнер, ни порядок твиков. Это даёт максимальный эффект при
минимальном риске и создаёт фундамент для этапов 2–5.

---

## 12. Backlog (зафиксировано, не потерять)

- **Windows Defender и 26H2.** Твик отключения Defender нужно обновить под
  24H2/25H2/26H2: `DisableAntiSpyware` игнорируется, обязателен учёт Tamper
  Protection и новых служб (`WinDefend`, `WdNisSvc`, `Sense`,
  `SecurityHealthService`, `webthreatdefusersvc_*`). Делать на P4–P5, не раньше.
- **`supercmd` сохраняется.** Запуск через `{tempfolder}\su.exe /wrs` нужен для
  Defender, поэтому в `IProcessService` он остаётся отдельным методом
  (`RunSuperAsync`), а не сливается с обычным `RunAsync`.
- **Очистка temp** должна пропускать `wotbo*.log`, иначе диагностика теряется.

