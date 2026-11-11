using System;
using System.Globalization;
using System.Threading.Tasks;
using WOTBO.App.Logging;

namespace WOTBO.Services.Network
{
    /// <summary>
    /// Проверка обновлений WOTBO.
    ///
    /// Сравнение версий намеренно оставлено прежним: строки
    /// <c>ver.json</c> и локальная версия приводятся к <c>double</c>.
    /// Переход на SemVer — отдельным изменением, чтобы не менять поведение.
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

                    double local = Convert.ToDouble(localVersion, CultureInfo.InvariantCulture);
                    double remote = Convert.ToDouble(info.RemoteVersion, CultureInfo.InvariantCulture);

                    if (local == remote)
                    {
                        info.Status = UpdateStatus.Actual;
                    }
                    else if (local < remote)
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
    }
}
