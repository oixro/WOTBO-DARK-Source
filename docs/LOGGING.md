# Логирование

## Куда пишется

`%TEMP%\wotbo.log` — путь не менялся (`AppPaths.LogFilePath`). Его читают F12
в приложении, `build/run.ps1` и сценарий диагностики из README.

## Формат

Все записи в `MainForm` переведены на уровни и категории. Категория — короткое имя
подсистемы: `App`, `UI`, `Windows`, `Temp`, `Tweak:<id>`, `Process`, `GPU`,
`Network`, `Update`, `OneDrive`, `System`, `Registry`, `State`.

```csharp
Log.Info("Tweak:main.mpo", "MPO был применён");
Log.Warn("Temp", $"не удалось очистить {tempfolder}: {ex.Message}");
Log.Error("Process", "dism завершился с кодом 87", ex);
```

```text
2026-11-11 20:24:51.4461 - [INFO ] [Tweak:main.mpo] MPO был применён
2026-11-11 20:24:51.4462 - [ERROR] [Process] dism завершился с кодом 87
2026-11-11 20:24:51.4462 - [ERROR] [Process] System.ComponentModel.Win32Exception: ...
```

Уровни: `TRACE`, `DEBUG`, `INFO`, `WARN`, `ERROR`. Исключение (`ex`) добавляется
следующими строками целиком, со стеком.

Для совместимости в `IAppLogger` остался `Write(string)` — запись без уровня в
прежнем формате `время - текст`. В самом приложении он больше не используется;
все прежние вызовы `writelog()` заменены на `Log.*`.

### Фильтр уровня

`DEBUG` пишется всегда — отдельного переключателя нет. Подробные записи
(команды, чтение реестра, нажатия UI, старые/новые значения) нужны для разбора
проблем, поэтому не прячутся. `TRACE` остаётся зарезервированным и сейчас не
используется.

### Многострочные сообщения

Каждая физическая строка получает свой префикс — включая стек исключения:

```text
2026-11-11 21:29:58.7515 - [ERROR] [App] System.ComponentModel.Win32Exception ...
2026-11-11 21:29:58.7515 - [ERROR] [App]    at System.Windows.Forms.Form.UpdateLayered()
2026-11-11 21:29:58.7515 - [ERROR] [App]    at Project.WOTBO.<Form1_Load>d__50.MoveNext()
```

Поэтому `grep "Tweak:main.mpo"` находит и хвост многострочного вывода WMI.

### Твики

Для применения и отката используется `TweakScope`:

```csharp
using (TweakScope.Apply("main.mpo"))
{
    AppServices.Registry.WriteValue(...);
}
// [INFO ] [Tweak:main.mpo] применение начато
// [INFO ] [Tweak:main.mpo] применение завершено за 12 мс
```

Категория `Tweak:<id>` общая для detect, apply и rollback. Примеры id:
`main.defender`, `main.mpo`, `main.pagefile`, `ui.bluefolders`,
`gpu.ansel`, `extra.onedrive`, `pro.csrss`.

Бэкап и миграция старых флагов пишутся в категорию `State` — см.
`docs/STATE.md`.

## Как устроено

- `IAppLogger` — контракт, `AppLogger` — файловая реализация.
- `Log` — временный статический фасад. Настраивается в `Program.Main` через
  `Log.Initialize(...)`; если не настроен, лениво создаётся `AppLogger` с
  путём по умолчанию. При переводе форм на конструкторную инъекцию фасад уйдёт,
  а `IAppLogger` останется.
- Запись синхронная, под `lock`. Файл открывается и закрывается на каждую
  строку: так работают `Get-Content -Wait`, чтение лога после падения и
  копирование при закрытии. При крахе последние строки не теряются.
- Ошибки записи (`IOException`, `UnauthorizedAccessException`) подавляются:
  лог не должен ронять приложение.

## Что ещё не сделано

- Ротация файла (10 МБ × 3) — на этапе P3 вместе с фоновым writer'ом, если
  синхронной записи окажется мало.
