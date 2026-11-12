# Сеть

`INetworkService` (`Services/Network`) заменил `Internet.OK()` и прямые
`WebClient` в `MainForm`.

## Методы

| Метод | Назначение |
|---|---|
| `IsAvailable()` | DNS-проверка `github.com` с таймаутом 3 с |
| `DownloadString(url)` | текст (ver.json, lastchange.json) |
| `DownloadFile(url, path)` | синхронная загрузка |
| `DownloadFileAsync(url, path)` | асинхронная загрузка (ffmpeg и т.п.) |

`Internet.OK()` оставлен как тонкая обёртка:

```csharp
internal class Internet
{
    public static bool OK() => AppServices.Network.IsAvailable();
}
```

## TLS

В конструкторе `NetworkService` включается TLS 1.2:

```csharp
ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
```

Без этого на чистом .NET Framework `WebClient` иногда падает с
`Could not create SSL/TLS secure channel` на GitHub.

## Логи

- проверка сети — `DEBUG [Network]`;
- начало загрузки — `INFO [Network]`;
- ошибки — `ERROR [Network]` с исключением.

## Проверка обновлений

`GitHubUpdateService` (`IGitHubUpdateService`) вынесен из `MainForm_Load`:

```csharp
UpdateInfo update = await AppServices.Updates.CheckAsync(curver);
```

Возможные статусы:

- `NoInternet` — сеть пропала во время проверки;
- `Actual` — версии равны;
- `Newer` — локальная меньше `ver.json`, сервис также скачивает
  `lastchange.json` в `ChangeLog`;
- `Dev` — локальная больше удалённой;
- `Error` — ошибка загрузки/разбора.

Версии сравниваются через `System.Version` (`2.9` < `2.10`), строка
нормализуется: обрезаются пробелы и ведущая `v`.
UI по-прежнему показывает `MessageBox`, пишет `lastchange.json` и запускает
команду обновления.

## Последние версии программ

`LatestVersionService` (`ILatestVersionService`) ходит в GitHub API
`repos/<repo>/releases/latest` и выбирает ассет по подстроке/суффиксу.

Используется для:

```text
ffmpeg       GyanD/codexffmpeg, essentials_build .zip
7zip         ip7z/7zip, -x64.exe
notepadpp    notepad-plus-plus/notepad-plus-plus, Installer.x64.exe
sharex       ShareX/ShareX, -setup-x64.exe
obs          obsproject/obs-studio, Windows-x64-Installer.exe
qbittorrent  qbittorrent/qBittorrent, _x64_setup.exe
klite        codecguide.com, K-Lite_*_Mega.exe
wiztree      diskanalyzer.com, wiztree_*_portable.zip
dotnet6      dotnet release-metadata, dotnet-runtime-*-win-x64.exe
```

Кнопки К-Lite, WizTree и .NET 6 также используют latest с fallback.
HWiNFO, MSI Afterburner, FastStone, CentBrowser, VC++ Runtimes и
CrystalDiskInfo отдают 403/404 либо не имеют стабильного машинного
endpoint — для них остаются статические ссылки.

Кнопки вкладки «Программы» сначала пробуют получить последнюю версию, а при
ошибке открывают прежний статический URL. FFmpeg-твик использует тот же
сервис; при недоступности API остаётся fallback на 7.1.

## Ресурсы твиков

Часть твиков скачивает вспомогательные файлы из
`raw.githubusercontent.com/oixro/WOTBO/main/resources/`:

```text
Release_x64.zip       ui.mica
imageres.zip          ui.bluefolders (Windows 10)
BlueIcon_Minimal.zip  ui.bluefolders (Windows 11)
unlocker.zip          pro.unlocker
AlwaysOnTop.exe       pro.alwaysontop
smartctl.exe          main.mmagent
```

`gear.zip` для `ui.wotbocontext` и `scheme.pow` для `main.powerscheme`
остаются встроенными ресурсами. Все загрузки идут через
`INetworkService.DownloadFile`, ошибки попадают в лог с категорией
`Network`.

## Что осталось

- `DownloadService` с прогрессом и отменой — по мере необходимости.
