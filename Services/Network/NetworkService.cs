using System;
using System.Net;
using System.Threading.Tasks;
using WOTBO.App.Logging;

namespace WOTBO.Services.Network
{
    /// <summary>
    /// Реализация <see cref="INetworkService"/> на <see cref="WebClient"/>.
    ///
    /// Проверка сети больше не может висеть бесконечно: DNS-запрос
    /// ограничен таймаутом. Загрузки логируются, ошибки не глотаются.
    /// </summary>
    public sealed class NetworkService : INetworkService
    {
        private const string Category = "Network";
        private const int DnsTimeoutMs = 3000;

        private readonly IAppLogger _log;

        public NetworkService(IAppLogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));

            // GitHub отдаёт только TLS 1.2+. На чистом .NET Framework
            // WebClient не всегда включает его сам.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public bool IsAvailable()
        {
            try
            {
                Task<IPHostEntry> lookup = Task.Run(() => Dns.GetHostEntry("github.com"));
                if (!lookup.Wait(DnsTimeoutMs))
                {
                    _log.Warn(Category, "проверка сети: таймаут DNS");
                    return false;
                }

                bool ok = lookup.Result != null;
                _log.Debug(Category, "проверка сети: " + (ok ? "есть" : "нет"));
                return ok;
            }
            catch (Exception ex)
            {
                _log.Debug(Category, "проверка сети не удалась: " + ex.Message);
                return false;
            }
        }

        public string DownloadString(string url)
        {
            try
            {
                using (var client = new WebClient())
                {
                    return client.DownloadString(url);
                }
            }
            catch (Exception ex)
            {
                _log.Error(Category, "не удалось скачать строку: " + url, ex);
                throw;
            }
        }

        public void DownloadFile(string url, string path)
        {
            try
            {
                _log.Info(Category, "скачивание: " + url);
                using (var client = new WebClient())
                {
                    client.DownloadFile(url, path);
                }
            }
            catch (Exception ex)
            {
                _log.Error(Category, "не удалось скачать файл: " + url, ex);
                throw;
            }
        }

        public async Task DownloadFileAsync(string url, string path)
        {
            try
            {
                _log.Info(Category, "скачивание: " + url);
                using (var client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(new Uri(url), path);
                }
            }
            catch (Exception ex)
            {
                _log.Error(Category, "не удалось скачать файл: " + url, ex);
                throw;
            }
        }
    }
}
