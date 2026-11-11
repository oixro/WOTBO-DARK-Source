using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using WOTBO.App.Logging;

namespace WOTBO.Services.Network
{
    public interface ILatestVersionService
    {
        /// <summary>
        /// URL последнего ассета GitHub-релиза. <paramref name="contains"/> и
        /// <paramref name="endsWith"/> — фильтры имени файла.
        /// </summary>
        Task<string> GetLatestAssetUrlAsync(string repository, string contains, string endsWith = null);

        /// <summary>Последний URL для программы по ключу вкладки «программы».</summary>
        Task<string> GetProgramUrlAsync(string programKey);
    }

    public sealed class LatestVersionService : ILatestVersionService
    {
        private const string Category = "LatestVersion";
        private const string Api = "https://api.github.com/repos/";

        private readonly INetworkService _network;
        private readonly IAppLogger _log;

        public LatestVersionService(INetworkService network, IAppLogger log)
        {
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task<string> GetLatestAssetUrlAsync(string repository, string contains, string endsWith = null)
        {
            return Task.Run(() =>
            {
                try
                {
                    string url = Api + repository + "/releases/latest";
                    string json = _network.DownloadString(url);
                    GitHubRelease release = Deserialize(json);
                    if (release == null || release.Assets == null)
                    {
                        return null;
                    }

                    foreach (GitHubAsset asset in release.Assets)
                    {
                        if (asset == null || string.IsNullOrEmpty(asset.Name))
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(contains)
                            && asset.Name.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(endsWith)
                            && !asset.Name.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        _log.Info(Category, repository + " -> " + asset.Name);
                        return asset.BrowserDownloadUrl;
                    }

                    _log.Warn(Category, "в релизе " + repository + " не найден подходящий ассет");
                    return null;
                }
                catch (Exception ex)
                {
                    _log.Warn(Category, "не удалось получить релиз " + repository + ": " + ex.Message);
                    return null;
                }
            });
        }

        public Task<string> GetProgramUrlAsync(string programKey)
        {
            switch ((programKey ?? string.Empty).ToLowerInvariant())
            {
                case "notepadpp":
                    return GetLatestAssetUrlAsync("notepad-plus-plus/notepad-plus-plus", "Installer.x64.exe");
                case "sharex":
                    return GetLatestAssetUrlAsync("ShareX/ShareX", "-setup-x64.exe");
                case "obs":
                    return GetLatestAssetUrlAsync("obsproject/obs-studio", "Windows-x64-Installer.exe");
                case "qbittorrent":
                    return GetLatestAssetUrlAsync("qbittorrent/qBittorrent", "_x64_setup.exe");
                case "crystaldiskinfo":
                    return GetLatestAssetUrlAsync("CrystalDewWorld/CrystalDiskInfo", ".exe");
                case "ffmpeg":
                    return GetLatestAssetUrlAsync("GyanD/codexffmpeg", "essentials_build", ".zip");
                default:
                    return Task.FromResult<string>(null);
            }
        }

        private static GitHubRelease Deserialize(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (GitHubRelease)serializer.ReadObject(stream);
            }
        }

        [DataContract]
        private sealed class GitHubRelease
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "assets")]
            public List<GitHubAsset> Assets { get; set; }
        }

        [DataContract]
        private sealed class GitHubAsset
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
    }
}
