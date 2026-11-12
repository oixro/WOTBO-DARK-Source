using System;
using System.Threading.Tasks;
using WOTBO.App.Logging;

namespace WOTBO.Services.Network
{
    /// <summary>
    /// Проверка обновлений WOTBO. Версии сравниваются через <see cref="Version"/>,
    /// чтобы корректно обрабатывать 2.9 → 2.10.
    /// </summary>
    public sealed class GitHubUpdateService : IGitHubUpdateService
    {
        private const string Category = "Update";
        private const string VersionUrl = "https://raw.githubusercontent.com/oixro/WOTBO/main/ver.json";
        private const string ChangeLogUrl = "https://raw.githubusercontent.com/oixro/WOTBO/main/lastchange.json";

        private readonly INetworkService _network;
        private readonly IAppLogger _log;

        public GitHubUpdateService(INetworkService network, IAppLogger log)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task<UpdateInfo> CheckAsync(string localVersion)
        {
            return Task.Run(() =>
            {
                var info = new UpdateInfo { LocalVersion = localVersion };

                if (!_network.IsAvailable())
                {
                    info.Status = UpdateStatus.NoInternet;
                    return info;
                }

                try
                {
                    info.RemoteVersion = _network.DownloadString(VersionUrl).Trim();

                    Version local = ParseVersion(localVersion);
                    Version remote = ParseVersion(info.RemoteVersion);
                    int comparison = local.CompareTo(remote);

                    if (comparison == 0)
                    {
                        info.Status = UpdateStatus.Actual;
                    }
                    else if (comparison < 0)
                    {
                        info.Status = UpdateStatus.Newer;
                        try
                        {
                            info.ChangeLog = _network.DownloadString(ChangeLogUrl);
                        }
                        catch (Exception ex)
                        {
                            _log.Warn(Category, "не удалось скачать список изменений: " + ex.Message);
                            info.ChangeLog = string.Empty;
                        }
                    }
                    else
                    {
                        info.Status = UpdateStatus.Dev;
                    }

                    return info;
                }
                catch (Exception ex)
                {
                    info.Status = UpdateStatus.Error;
                    info.Error = ex.Message;
                    _log.Error(Category, "проверка обновления не удалась", ex);
                    return info;
                }
            });
        }

        private static Version ParseVersion(string value)
        {
            string text = (value ?? string.Empty).Trim().TrimStart('v', 'V');
            Version version;
            if (!Version.TryParse(text, out version))
            {
                throw new FormatException("Некорректная версия: " + value);
            }

            return version;
        }
    }
}
