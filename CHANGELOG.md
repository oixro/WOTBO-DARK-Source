# Changelog

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
- Сайт: `https://wotbo.pro/` → `https://oixro.github.io/wotbo-site/`.
- README сокращён до личных заметок; добавлен `CHANGELOG.md`.

### Исправлено

- пропадающие вкладки «1 / 2» после `ChangePanel`;
- маленькие стрелки отката на страницах, кроме «Основного», при DPI 175 %;
- обрезанный интерфейс при масштабировании 125–200 %: окно теперь
  использует фактические границы `PanelMain`, а не `390×305`;
- изменение масштаба на лету обрабатывается `OnDpiChanged`;
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
