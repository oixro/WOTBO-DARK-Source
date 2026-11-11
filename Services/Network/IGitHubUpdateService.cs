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
    }

    public interface IGitHubUpdateService
    {
        Task<UpdateInfo> CheckAsync(string localVersion);
    }
}
