using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
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
                case "7zip":
                    return GetLatestAssetUrlAsync("ip7z/7zip", "-x64.exe");
                case "rustdesk":
                    return GetLatestAssetUrlAsync("rustdesk/rustdesk", "x86_64.exe", ".exe");
                case "explorerblurmica":
                    return GetLatestAssetUrlAsync("Maplespe/ExplorerBlurMica", "Release_x64.zip", ".zip");
                case "defenderdisable":
                    return GetLatestAssetUrlAsync("pgkt04/defender-control", "disable-defender.exe", ".exe");
                case "defenderenable":
                    return GetLatestAssetUrlAsync("pgkt04/defender-control", "enable-defender.exe", ".exe");
                case "ffmpeg":
                    return GetLatestAssetUrlAsync("GyanD/codexffmpeg", "essentials_build", ".zip");
                case "klite":
                    return GetKLiteUrlAsync();
                case "wiztree":
                    return GetWizTreeUrlAsync();
                default:
                    return Task.FromResult<string>(null);
            }
        }

        private Task<string> GetKLiteUrlAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    string html = _network.DownloadString("https://codecguide.com/download_k-lite_codec_pack_mega.htm");
                    Match match = Regex.Match(
                        html,
                        @"https://files[0-9]?\.codecguide\.com/K-Lite_Codec_Pack_[0-9]+_Mega\.exe",
                        RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        _log.Info(Category, "K-Lite -> " + match.Value);
                        return match.Value;
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(Category, "K-Lite: " + ex.Message);
                }

                return null;
            });
        }

        private Task<string> GetWizTreeUrlAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    string html = _network.DownloadString("https://diskanalyzer.com/download");
                    Match match = Regex.Match(
                        html,
                        @"files/wiztree_[0-9_]+_portable\.zip",
                        RegexOptions.IgnoreCase);

                    if (match.Success)
                    {
                        string url = "https://diskanalyzer.com/" + match.Value;
                        _log.Info(Category, "WizTree -> " + url);
                        return url;
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(Category, "WizTree: " + ex.Message);
                }

                return null;
            });
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
