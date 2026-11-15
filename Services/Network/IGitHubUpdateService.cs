using System;
using System.Globalization;
using System.Threading.Tasks;

namespace WOTBO.Services.Network
{
    public enum UpdateStatus
    {
        NoInternet,
        Actual,
        Newer,
        Dev,
        Error,
    }

    public sealed class UpdateInfo
    {
        public UpdateStatus Status { get; set; }

        public string LocalVersion { get; set; }

        public string RemoteVersion { get; set; }

        public string ChangeLog { get; set; }

        public string Error { get; set; }

        /// <summary>
        /// Версия пришла с запасного зеркала: значит основной источник
        /// недоступен, и автоматически скачать exe не получится.
        /// </summary>
        public bool FromMirror { get; set; }
    }

    public interface IGitHubUpdateService
    {
        Task<UpdateInfo> CheckAsync(string localVersion);
    }
}
