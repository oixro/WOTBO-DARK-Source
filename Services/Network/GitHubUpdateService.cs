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

        /// <summary>
        /// Откуда берём ver.json и список изменений. raw.githubusercontent.com
        /// у части провайдеров заблокирован, поэтому есть запасное зеркало
        /// jsDelivr: если первый источник не ответил или ответил мусором,
        /// пробуем следующий.
        /// </summary>
        private static readonly string[][] Sources =
        {
            new[]
            {
                "https://raw.githubusercontent.com/oixro/WOTBO/main/ver.json",
                "https://raw.githubusercontent.com/oixro/WOTBO/main/lastchange.json",
            },
            new[]
            {
                "https://cdn.jsdelivr.net/gh/oixro/WOTBO@main/ver.json",
                "https://cdn.jsdelivr.net/gh/oixro/WOTBO@main/lastchange.json",
            },
        };

        /// <summary>Источник, с которого удалось прочитать версию.</summary>
        private static string _changeLogUrl = Sources[0][1];

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
                    info.RemoteVersion = ReadRemoteVersion(out bool fromMirror);
                    info.FromMirror = fromMirror;

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
                            info.ChangeLog = _network.DownloadString(_changeLogUrl);
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

        /// <summary>Читает версию с первого ответившего источника.</summary>
        private string ReadRemoteVersion(out bool fromMirror)
        {
            Exception last = null;
            fromMirror = false;

            foreach (string[] source in Sources)
            {
                try
                {
                    string text = _network.DownloadString(source[0]).Trim();

                    // Проверяем, что это действительно версия, а не страница
                    // блокировки провайдера: тогда пробуем зеркало.
                    ParseVersion(text);

                    _changeLogUrl = source[1];
                    fromMirror = source != Sources[0];
                    return text;
                }
                catch (Exception ex)
                {
                    last = ex;
                    _log.Warn(Category, "источник обновления не ответил: " + source[0] + " (" + ex.Message + ")");
                }
            }

            throw new InvalidOperationException("ни один источник обновления не ответил", last);
        }

        private static Version ParseVersion(string value)
        {
            string text = (value ?? string.Empty).Trim().TrimStart('\uFEFF').Trim().TrimStart('v', 'V');

            // Берём только ведущие цифры и точки: "2.35-beta", "2.35 (beta)"
            // и подобное должны читаться как 2.35, а не ломать обновление.
            int length = 0;
            while (length < text.Length && (char.IsDigit(text[length]) || text[length] == '.'))
            {
                length++;
            }

            Version version;
            if (length == 0 || !Version.TryParse(text.Substring(0, length), out version))
            {
                throw new FormatException("Некорректная версия: " + value);
            }

            // Приводим к четырём компонентам: иначе "2.34" и "2.34.0.0"
            // считаются разными версиями и обновление предлагается заново
            // на каждом запуске.
            return new Version(
                version.Major,
                version.Minor,
                Math.Max(version.Build, 0),
                Math.Max(version.Revision, 0));
        }
    }
}
