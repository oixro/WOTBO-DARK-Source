using System;
using System.Net;
using System.Text;
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

        // WebClient сам таймаутов не ставит: зависший сервер держал запрос
        // до 100 секунд (HTTP) и до 5 минут на чтение. Обновление проверяется
        // при старте, поэтому ожидание было заметно как «подвисшая» программа.
        private const int StringTimeoutMs = 20000;
        private const int FileTimeoutMs = 30000;

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
                using (var client = new TimeoutWebClient(StringTimeoutMs))
                {
                    client.Headers[HttpRequestHeader.UserAgent] = "WOTBO";

                    // lastchange.json — UTF-8 с кириллицей. По умолчанию
                    // WebClient декодирует ответ системной ANSI-кодировкой,
                    // из-за чего в окне обновления был «Âûøå...».
                    client.Encoding = Encoding.UTF8;

                    string text = client.DownloadString(url);

                    // Иногда файл приходит с BOM — Version.TryParse на нём падает.
                    return text.TrimStart('\uFEFF');
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
                using (var client = new TimeoutWebClient(FileTimeoutMs))
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
                using (var client = new TimeoutWebClient(FileTimeoutMs))
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
        /// <summary>WebClient с явными таймаутами: иначе запрос может висеть минутами.</summary>
        private sealed class TimeoutWebClient : WebClient
        {
            private readonly int _timeoutMs;

            public TimeoutWebClient(int timeoutMs)
            {
                _timeoutMs = timeoutMs;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);

                var http = request as HttpWebRequest;
                if (http != null)
                {
                    http.Timeout = _timeoutMs;
                    http.ReadWriteTimeout = _timeoutMs;
                }

                return request;
            }
        }
    }
}
