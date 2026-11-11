using System;
using System.Net;
using System.Threading.Tasks;

namespace WOTBO.Services.Network
{
    /// <summary>
    /// Проверка сети и загрузки. URL и поведение прежние.
    /// </summary>
    public interface INetworkService
    {
        /// <summary>Доступен ли GitHub. DNS-проверка с таймаутом.</summary>
        bool IsAvailable();

        string DownloadString(string url);

        void DownloadFile(string url, string path);

        Task DownloadFileAsync(string url, string path);
    }
}
