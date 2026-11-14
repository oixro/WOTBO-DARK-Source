# Changelog

## 2.34-beta — 2026-11-15

### Defender: правила применения

- Обязательное условие: перед отключением пользователь сам выключает
  **Tamper Protection** и **Real-time protection**. Если они включены, твик не
  применяется и открывает настройки «Безопасности Windows».
- **Windows 11** — всегда через безопасный режим; **Windows 10** — обычным
  применением, без перезагрузки.
- Пока выбран твик отключения Defender, остальные твики блокируются и
  снимаются (если были отмечены), стрелки отката скрываются, «Применить»
  выполняет только Defender. При снятии галочек показывается уведомление,
  почему они сняты.
- Убрана задержка 20 секунд перед перезагрузкой.
- После финальной перезагрузки автоматически открывается окно результата
  (`--defender-result apply|restore`): заголовок зелёный только при успехе всех
  обязательных проверок, есть счётчик «пройдено N из M», необязательные пункты
  помечаются точкой, окно отката обновляется до включения Real-time,
  кнопка «Закрыть» и Esc работают.
- Удалён пункт «вернуть защиту от эксплойтов» (`label_kalorant`) и
  `exploitscfg.reg` из regpack.
- Отключение служб идёт через SCM и повторяется после паузы: на 24H2+
  `SecurityHealthService` возвращал `WinDefend/WdFilter/WdBoot/MDCoreSvc`
  в demand start, теперь он гасится первым.
- Восстановление реестра больше не удаляет защищённые ветки: сначала импорт,
  затем удаление лишних значений и подветок (иначе `Services\WinDefend`
  мог остаться без данных). Владелец папок возвращается всегда.
- Убраны дубли Userinit/RunOnce для стадии: они перехватывали мьютекс раньше
  службы SYSTEM, и работа шла без прав. Остались служба и скрипты на рабочем
  столе.

## 2.34-beta — 2026-11-14

### Defender

- `main.defender` переписан: полное отключение вместо `.reg`-пакетов.
  Службы и драйверы (`WinDefend`, `WdNisSvc`, `WdFilter`, `WdBoot`, `WdNisDrv`,
  `MDCoreSvc`, `Sense`, `Sgrm*`, `MsSec*`, `SecurityHealthService`,
  `webthreatdef*`, `MpsSvc`) → `Start=4`; папки защитника переименовываются
  в `*.wotbo_bak` (файлы не удаляются); отключаются задачи, политики,
  брандмауэр, SmartScreen, Smart App Control и Windows Security.
- Безопасный режим автоматизирован: одноразовая служба `WotboDefender`,
  прописанная в `SafeBoot\Minimal`/`Network`, запускается SCM под SYSTEM
  (планировщик задач в safe mode задачи не исполняет). Дубли — `Userinit`,
  `RunOnce` и скрипты на рабочем столе; аварийный `C:\WOTBO-UNDO-SAFE-MODE.bat`.
- Откат: восстановление из обычного режима, при нехватке прав — та же служба
  с фазой `restore`. Реестр восстанавливается по схеме
  «удалить ветку → импортировать экспорт», владельцы папок возвращаются
  (TrustedInstaller). Одноразовая задача `\WOTBO\DefenderFinalize`
  доигрывает `Set-MpPreference`, чтобы `RealTimeProtectionEnabled`
  включался с первого ребута.
- Журнал и бэкап: `%ProgramData%\oixro\wotbo\defender\journal.json`,
  зеркало в `%LOCALAPPDATA%\oixro\wotbo\backups\main.defender`.
- Логи: `C:\Windows\Temp\wotbo-defender.log`, `...-stage.log`,
  `...-service.log`.
- Удалены старые `.reg`-пакеты Defender из `regpack.zip`,
  `restoreWINDEF10/11.reg` и пункт «Windows Defender Killer».

### Иконки

- `iconwotbo2.ico` оптимизирован (192 397 → 134 161 байт) без потери качества:
  64/128/256 хранятся как PNG, 16/32/48 остаются BMP.
- Иконка приложения, окна, шапки и пункта контекстного меню берутся из нового
  файла; дубликаты иконки из `MainForm.resx`/`MsiModeForm.resx` удалены,
  `gear.zip` ужат до 9 173 байт.

## 2.34-beta — 2026-11-12

Бета-сборка после полного рефакторинга WOTBO. Приложение переведено с одного
`Form1` на сервисы и 48 независимых твиков `ITweak`.

### Итог

- 48 твиков в `TweakCatalog`;
- `Form1.cs` 3452 строки → `MainForm.cs` 331 строка + partial-файлы;
- 52 коммита, 164 файла, +13 680 / −3 792 строки с начала P0;
- `Release|x64`: 0 warnings, 0 errors;
- registry-smoke: 17/17 твиков прошли `apply → detect → rollback`;
- интерактивный запуск проверен при 175 % и 200 % DPI.

### Добавлено

- **Инфраструктура:** `AppPaths`, `AppServices`, `AppBootstrap`,
  `StartupWorkflow`, `InstanceGuard`, `ProgramLinks`.
- **Логирование:** уровни `DEBUG/INFO/WARN/ERROR`, категории `Tweak:<id>`,
  `TweakScope`, запись `DEBUG` всегда.
- **Состояние и бэкапы:** `TweakStateStore`, `TweakBackup`,
  `TweakBackupSession`; постоянные бэкапы в
  `HKCU\Software\oixro\wotbo\Backups\<id>` и
  `%LOCALAPPDATA%\oixro\wotbo\backups\<id>`; миграция старых флагов.
- **Реестр:** `RegistryService`, `RegistryPaths`, снимки ключей/значений,
  `RegistryOperationException`.
- **Процессы:** `ProcessService` с `StartCaptured`, `StartDetached`,
  `StartPowerShell`, `RunSuper`, `ReadOutputAsync`.
- **Сеть:** `NetworkService`, `GitHubUpdateService`, `UpdateCoordinator`,
  `LatestVersionService`, таймаут DNS, TLS 1.2.
- **Система:** `WindowsVersionService`, `WmiSystemInfoService`,
  `DisplayService`, `GpuService`, `ReservedStorageService`,
  `LegacyCleanupService`, `ResourceExtractor`, `TempWorkspace`.
- **UI:** `TweakUiBinder`, `PanelNavigator`, `NavigationController`,
  `TweakHoverInfo`, `FormPainter`, `HoverAnimator`, `WindowDragBehavior`,
  `BalloonNotifier`, `NativeMethods`, `Dpi`.
- **Твики:** 48 классов `ITweak`, включая `main.mmagent`,
  `main.reservedstorage`, `main.defender`, `ui.mica`, `ui.bluefolders`,
  `pro.unlocker`, `pro.alwaysontop`.
- **Настройка:** чекбокс подсказок на вкладке «Интерфейс»; подсказки
  показывают путь, старое/новое значение и время применения.
- **CI:** GitHub Actions `Release|x64`, проверка PE `0x8664`, артефакт
  `WOTBO-x64`.
- **Релиз:** GitHub pre-release `v2.34-beta` с `WOTBO.exe`.

### Изменено

- Версия проекта — **2.34**.
- `Form1/Form2/Form4/FormShadow/Win32Priority` переименованы в
  `MainForm/EulaForm/MsiModeForm/ShadowForm/Win32PriorityForm`.
- Namespace `test1/Project → WOTBO`.
- `Form1` разложен на `MainForm.Binders/Load/Tweaks/Features/Navigation/
  Rollbacks`.
- Detect/Apply/Rollback твиков вынесены из формы в `Services/Tweaks`.
- 42 обработчика `back_*_Click` и 25 обработчиков download-кнопок удалены;
  клики подключает `TweakUiBinder`/`ProgramLinks`.
- Навигация по панелям вынесена в `NavigationController`.
- Карта подсказок строится из `TweakUiBinder.Register`, без второго
  словаря в `TweakHoverInfo`.
- Включён DPI awareness; размер окна задаётся после автоскейла
  (`390×305` × DPI), координаты в коде масштабируются через `Dpi`.
- Обновлены статические ссылки: FastStone `FSViewerSetup85.exe`,
  HWiNFO `hwi64_860.exe`.
- AnyDesk заменён на RustDesk с парсером последней версии.
- `ui.mica` обновляется из последнего релиза `Maplespe/ExplorerBlurMica`
  с fallback на `2.1.0`.
- EULA переписана, получила тёмную тему и компактный размер; пункт про
  правый клик вынесен отдельно.
- Со страницы загрузок убраны Win10 Tweaker, Epic Games Launcher,
  CrystalDiskInfo и .NET 6 Runtime.
- Сайт: `https://wotbo.pro/` → `https://oixro.github.io/wotbo-site/`.
- README сокращён до личных заметок; добавлен `CHANGELOG.md`.

### Исправлено

- пропадающие вкладки «1 / 2» после `ChangePanel`;
- маленькие стрелки отката на страницах, кроме «Основного», при DPI 175 %;
- обрезанный интерфейс при масштабировании 125–200 %: окно теперь
  использует фактические границы `PanelMain`, а не `390×305`;
- изменение масштаба на лету обрабатывается `OnDpiChanged`;
- состояние `main.reservedstorage` синхронизируется с фактическим DISM:
  выключен → твик применён и заблокирован, включён → твик снова доступен;
- переключение категорий и страниц: панель поднимается наверх
  (`BringToFront`), а устаревшие анимации отменяются по generation-токену;
- координаты навигации считаются по фактической ширине `PanelMain`, потому
  что `DeviceDpi` мог вернуть 96 при масштабе 125–200 %;
- `TweakUiBinder.Detect` синхронизирует store с фактическим `Detect`
  при старте, без полного прохода по всем твикам;
- `main.mmagent` проверяется через WMI `PS_MMAgent`, без запуска PowerShell
  на каждом старте;
- `bufferbloat`, `ui.buttons`, `main.mmagent`, `main.mousefix` и
  `pro.homegallery` теперь проверяют реальное состояние, а не только
  историю применения;
- в `main.mousefix` добавлен `RawMouseThrottleDuration=0x14`; пользователи
  старой версии снова увидят твик доступным;
- `MSI_util_v3.exe`: больше не распаковывается `cursors.zip`;
- `DefenderKiller`: убран двойной запуск и удаление архива;
- обновление: `rd` по exe заменена на `del new.exe`, ошибки запуска
  логируются;
- версии сравниваются через `System.Version` (`2.9 < 2.10`);
- удалён мёртвый `checkBox1`;
- освобождение `NotifyIcon` и `Timer` при закрытии;
- DNS-проверка получила таймаут;
- пустые `catch` заменены логированием;
- `powercfg /import` в `PowerSchemeTweak` проверяется, есть fallback на
  High performance/Balanced;
- `pro.unlocker`: проверка `File.Exists` для архива;
- null-safe чтение ключей реестра в оставшихся местах.

### Проверки

- `Release|x64` — 0 warnings, 0 errors, PE `0x8664`.
- Registry-smoke на VM — 17/17:
  `ui.jpegquality`, `extra.iconcache`, `extra.stickykeys`, `main.mpo`,
  `extra.scoobe`, `extra.dwmpriority`, `pro.csrss`, `main.dwminput`,
  `main.mitigations`, `gpu.hdcp`, `ui.buttons`, `ui.animations`,
  `extra.movetemp`, `extra.edge`, `pro.windowsupdate`, `main.mousefix`,
  `main.powerlatency`.
- Интерактивно: запуск, окно `682×534` при 175 %, окно `780×610` при 200 %,
  переход «Основное → Интерфейс», видимость вкладок и стрелок отката.

### Не проверено вживую

Требуют снапшота VM и ручного прогона:

- `main.defender`, `main.regpack`, `main.reservedstorage`;
- `gpu.nvidia`, `gpu.directplay`, `gpu.ansel`;
- `ui.ffmpeg`, `ui.mica`, `ui.bluefolders`, `ui.win11header`;
- `pro.unlocker`, `pro.alwaysontop`;
- DISM/compact/cleanup-твики.

### Известные ограничения

- Изменение масштаба Windows во время работы применяется после перезапуска.
- `pro.win32priority` выбор значения оставляет за диалогом.
- `ui.animations` управляет только анимацией панелей WOTBO.
- Часть fallback-ссылок вкладки «Скачать программы» может быть старше
  актуальной версии; основные программы используют latest-парсеры.
