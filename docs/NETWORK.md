# Сеть

`INetworkService` (`Services/Network`) заменил `Internet.OK()` и прямые
`WebClient` в `Form1`.

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

## Что осталось

- `GitHubUpdateService`: вынести сравнение версий и `lastchange.json` из
  `Form1_Load`. Сейчас UI-логика осталась на месте, но загрузки уже идут
  через сервис.
- `DownloadService` с прогрессом и отменой — по мере необходимости.
