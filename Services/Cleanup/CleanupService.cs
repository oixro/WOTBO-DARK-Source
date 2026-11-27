using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Cleanup
{
    public enum CleanupAction
    {
        Delete,
        EmptyRecycleBin,
        Dism,
        HibernateOff,
        SteamUninstall,
        OpenWindowsCleanup,
    }

    public enum CleanupSafety
    {
        Safe,
        Review,
    }

    /// <summary>Один пункт очистки. Логика и набор мест взяты из Roomy (MIT).</summary>
    public sealed class CleanupItem
    {
        public CleanupItem(
            string id,
            string group,
            string name,
            CleanupAction action,
            CleanupSafety safety,
            IEnumerable<string> paths,
            TimeSpan? minAge = null)
        {
            Id = id;
            Group = group;
            Name = name;
            Action = action;
            Safety = safety;
            Paths = paths == null ? new List<string>() : paths.Where(p => !string.IsNullOrEmpty(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            MinAge = minAge;
        }

        public string Id { get; private set; }

        /// <summary>Заголовок группы в списке очистки.</summary>
        public string Group { get; private set; }

        public string Name { get; private set; }

        public CleanupAction Action { get; private set; }

        public CleanupSafety Safety { get; private set; }

        public List<string> Paths { get; set; }

        public TimeSpan? MinAge { get; private set; }

        /// <summary>Удаляется сам файл, а не содержимое папки.</summary>
        public bool IsFile { get; set; }

        /// <summary>После очистки удалить и саму папку, если она опустела.</summary>
        public bool WholeFolder { get; set; }

        public string AppId { get; set; }

        public long Size { get; set; }

        public int Files { get; set; }

        public string Detail { get; set; }

        /// <summary>Тяжёлый поиск путей, который нельзя делать в UI-потоке.</summary>
        internal Func<List<string>> PathProvider { get; set; }

        public override string ToString()
        {
            if (Action == CleanupAction.Dism)
            {
                return Name;
            }

            return Name + " — " + CleanupService.FormatSize(Size);
        }
    }

    /// <summary>
    /// Сканер и очистка мусора: Windows Update, временные файлы, кэши браузеров
    /// и приложений, шейдеры, дампы, старые установщики, большие загрузки и
    /// остатки удалённых программ. Всё best-effort: занятые файлы и ссылки
    /// пропускаются, личные папки защищены.
    /// </summary>
    public sealed class CleanupService
    {
        private const string Category = "Cleanup";
        private const uint SherbNoConfirmation = 0x00000001;
        private const uint SherbNoProgressUi = 0x00000002;
        private const uint SherbNoSound = 0x00000004;
        private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

        private static readonly HashSet<string> InstallerExtensions = new HashSet<string>(
            new[] { ".exe", ".msi", ".msix", ".msixbundle", ".appx", ".appxbundle" },
            StringComparer.OrdinalIgnoreCase);

        private readonly IProcessService _processes;
        private readonly IAppLogger _log;

        public CleanupService(IProcessService processes, IAppLogger log)
        {
            _processes = processes ?? throw new ArgumentNullException(nameof(processes));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public List<CleanupItem> CreateItems()
        {
            var items = new List<CleanupItem>();

            AddWindows(items);
            AddTemp(items);
            AddBrowsers(items);
            AddApps(items);
            AddShaders(items);
            AddDownloads(items);
            AddLeftovers(items);
            AddHibernation(items);

            return items;
        }

        public Task<CleanupItem> ScanAsync(CleanupItem item)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (item.PathProvider != null)
                    {
                        item.Paths = item.PathProvider() ?? new List<string>();
                    }

                    switch (item.Action)
                    {
                        case CleanupAction.EmptyRecycleBin:
                            item.Size = QueryRecycleBin();
                            break;

                        case CleanupAction.Dism:
                            item.Size = 0;
                            item.Detail = "Необратимо: после ResetBase нельзя удалить установленные обновления";
                            break;

                        case CleanupAction.HibernateOff:
                            item.Size = HibernationSize();
                            item.Files = item.Size > 0 ? 1 : 0;
                            item.Detail = "Отключит гибернацию и быстрый запуск";
                            break;

                        case CleanupAction.SteamUninstall:
                        case CleanupAction.OpenWindowsCleanup:
                            break;

                        default:
                            long total = 0;
                            int files = 0;
                            foreach (string path in item.Paths)
                            {
                                CleanupMeasure measure = CleanupFileOps.Measure(path, item.MinAge);
                                total += measure.Bytes;
                                files += measure.Files;
                            }

                            item.Size = total;
                            item.Files = files;
                            break;
                    }

                    if (item.Detail == null && item.Action == CleanupAction.Delete)
                    {
                        item.Detail = item.Files.ToString("N0", CultureInfo.CurrentCulture)
                            + " " + PluralFiles(item.Files);
                    }
                }
                catch (Exception exception)
                {
                    _log.Warn(Category, "размер " + item.Name + ": " + exception.Message);
                    item.Size = 0;
                }

                return item;
            });
        }

        public Task<long> CleanAsync(CleanupItem item)
        {
            return Task.Run(() =>
            {
                try
                {
                    switch (item.Action)
                    {
                        case CleanupAction.EmptyRecycleBin:
                            return EmptyRecycleBin() ? Math.Max(0, item.Size) : 0L;

                        case CleanupAction.Dism:
                            return CleanupWinSxs();

                        case CleanupAction.HibernateOff:
                            return DisableHibernation() ? Math.Max(0, item.Size) : 0L;

                        case CleanupAction.SteamUninstall:
                            OpenSteamUninstall(item.AppId);
                            return 0L;

                        case CleanupAction.OpenWindowsCleanup:
                            OpenWindowsCleanup();
                            return 0L;

                        default:
                            return DeleteItem(item);
                    }
                }
                catch (Exception exception)
                {
                    _log.Warn(Category, "очистка " + item.Name + ": " + exception.Message);
                    return 0L;
                }
            });
        }

        public static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L)
            {
                return (bytes / (1024d * 1024d * 1024d)).ToString("0.0", CultureInfo.CurrentCulture) + " ГБ";
            }

            if (bytes >= 1024L * 1024L)
            {
                return (bytes / (1024d * 1024d)).ToString("0", CultureInfo.CurrentCulture) + " МБ";
            }

            if (bytes >= 1024L)
            {
                return (bytes / 1024d).ToString("0", CultureInfo.CurrentCulture) + " КБ";
            }

            return bytes.ToString(CultureInfo.CurrentCulture) + " Б";
        }

        private static string PluralFiles(int count)
        {
            int mod10 = count % 10;
            int mod100 = count % 100;
            if (mod10 == 1 && mod100 != 11) return "файл";
            if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return "файла";
            return "файлов";
        }

        private long DeleteItem(CleanupItem item)
        {
            long freed = 0;
            foreach (string path in item.Paths.ToList())
            {
                CleanupDeleteResult result = CleanupFileOps.Delete(path, item.MinAge);
                freed += result.Freed;

                if (item.WholeFolder)
                {
                    CleanupFileOps.RemoveIfEmpty(path);
                }
            }

            _log.Info(Category, "очищено " + FormatSize(freed) + " (" + item.Name + ")");
            return freed;
        }

        // ------------------------------------------------------------- windows ---

        private static void AddWindows(List<CleanupItem> items)
        {
            items.Add(new CleanupItem(
                "update", "Windows", "Кэш обновлений Windows",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.WindowsPath("SoftwareDistribution", "Download") }));

            items.Add(new CleanupItem(
                "deliveryopt", "Windows", "Кэш Delivery Optimization",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[]
                {
                    CleanupLocations.WindowsPath("ServiceProfiles", "NetworkService", "AppData", "Local",
                        "Microsoft", "Windows", "DeliveryOptimization", "Cache"),
                }));

            items.Add(new CleanupItem(
                "wintemp", "Windows", "Временные файлы Windows",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.WindowsPath("Temp") },
                OneDay));

            items.Add(new CleanupItem(
                "systemdumps", "Windows", "Системные дампы",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[]
                {
                    CleanupLocations.WindowsPath("Minidump"),
                    CleanupLocations.WindowsPath("MEMORY.DMP"),
                    CleanupLocations.ProgramDataPath("Microsoft", "Windows", "WER", "ReportArchive"),
                    CleanupLocations.ProgramDataPath("Microsoft", "Windows", "WER", "ReportQueue"),
                }));

            items.Add(new CleanupItem(
                "winsxs", "Windows", "Компоненты WinSxS (DISM ResetBase)",
                CleanupAction.Dism, CleanupSafety.Review,
                new string[0]));
        }

        // ---------------------------------------------------------------- temp ---

        private static void AddTemp(List<CleanupItem> items)
        {
            items.Add(new CleanupItem(
                "temp", "Временные файлы", "Временные файлы пользователя",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.Temp },
                OneDay));

            items.Add(new CleanupItem(
                "crashes", "Временные файлы", "Дампы сбоев приложений",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.LocalPath("CrashDumps") }));

            items.Add(new CleanupItem(
                "wer", "Временные файлы", "Отчёты об ошибках",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.LocalPath("Microsoft", "Windows", "WER") }));

            items.Add(new CleanupItem(
                "recycle", "Временные файлы", "Корзина",
                CleanupAction.EmptyRecycleBin, CleanupSafety.Safe,
                new string[0]));
        }

        // ------------------------------------------------------------ browsers ---

        private static void AddBrowsers(List<CleanupItem> items)
        {
            AddChromium(items, "Chrome", "Google", "Chrome", "User Data");
            AddChromium(items, "Edge", "Microsoft", "Edge", "User Data");
            AddChromium(items, "Brave", "BraveSoftware", "Brave-Browser", "User Data");
            AddChromium(items, "Vivaldi", "Vivaldi", "User Data");
            AddChromium(items, "Chromium", "Chromium", "User Data");

            items.Add(new CleanupItem(
                "firefox", "Браузеры", "Firefox",
                CleanupAction.Delete, CleanupSafety.Safe,
                FirefoxCaches()));

            items.Add(new CleanupItem(
                "opera", "Браузеры", "Opera",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[]
                {
                    CleanupLocations.LocalPath("Opera Software", "Opera Stable", "Cache"),
                    CleanupLocations.LocalPath("Opera Software", "Opera GX Stable", "Cache"),
                }));
        }

        private static void AddChromium(List<CleanupItem> items, string name, params string[] userDataParts)
        {
            items.Add(new CleanupItem(
                "browser." + name.ToLowerInvariant(), "Браузеры", name,
                CleanupAction.Delete, CleanupSafety.Safe,
                ChromiumCaches(Path.Combine(
                    new[] { CleanupLocations.Local }.Concat(userDataParts).ToArray()))));
        }

        private static List<string> ChromiumCaches(string userData)
        {
            var result = new List<string>();
            if (!Directory.Exists(userData))
            {
                return result;
            }

            result.Add(Path.Combine(userData, "ShaderCache"));
            result.Add(Path.Combine(userData, "GrShaderCache"));
            result.Add(Path.Combine(userData, "GraphiteDawnCache"));

            try
            {
                foreach (string directory in Directory.EnumerateDirectories(userData))
                {
                    string name = Path.GetFileName(directory);
                    if (name == "Default"
                        || name == "Guest Profile"
                        || name.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(Path.Combine(directory, "Cache"));
                        result.Add(Path.Combine(directory, "Code Cache"));
                        result.Add(Path.Combine(directory, "GPUCache"));
                    }
                }
            }
            catch
            {
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> FirefoxCaches()
        {
            var result = new List<string>();
            string profiles = CleanupLocations.LocalPath("Mozilla", "Firefox", "Profiles");
            try
            {
                foreach (string directory in Directory.EnumerateDirectories(profiles))
                {
                    result.Add(Path.Combine(directory, "cache2"));
                }
            }
            catch
            {
            }

            return result;
        }

        // ---------------------------------------------------------------- apps ---

        private static void AddApps(List<CleanupItem> items)
        {
            foreach (string discord in new[] { "discord", "discordptb", "discordcanary" })
            {
                AddCacheBits(items, "Discord", "discord." + discord,
                    CleanupLocations.RoamingPath(discord));
            }

            AddCacheBits(items, "Slack", "slack", CleanupLocations.RoamingPath("Slack"));
            AddCacheBits(items, "Teams (классический)", "teams", CleanupLocations.RoamingPath("Microsoft", "Teams"));

            items.Add(new CleanupItem(
                "teams.new", "Приложения", "Microsoft Teams (новый)",
                CleanupAction.Delete, CleanupSafety.Safe,
                NewTeamsCaches()));

            items.Add(new CleanupItem(
                "spotify", "Приложения", "Spotify",
                CleanupAction.Delete, CleanupSafety.Safe,
                Bits(CleanupLocations.LocalPath("Spotify", "Browser"))
                    .Concat(new[]
                    {
                        CleanupLocations.LocalPath("Spotify", "Data"),
                        CleanupLocations.LocalPath("Packages", "SpotifyAB.SpotifyMusic_zpdnekdrzrea0",
                            "LocalCache", "Spotify", "Data"),
                    })));

            items.Add(new CleanupItem(
                "vscode", "Приложения", "VS Code",
                CleanupAction.Delete, CleanupSafety.Safe,
                Bits(CleanupLocations.RoamingPath("Code"))
                    .Concat(new[]
                    {
                        CleanupLocations.RoamingPath("Code", "CachedData"),
                        CleanupLocations.RoamingPath("Code", "CachedExtensionVSIXs"),
                    })));

            items.Add(new CleanupItem(
                "steam.browser", "Приложения", "Steam (браузер)",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.LocalPath("Steam", "htmlcache") }));

            AddChromiumCachesUnder(items, "Battle.net", "battlenet", CleanupLocations.LocalPath("Battle.net"));
            AddChromiumCachesUnder(items, "EA app", "ea", CleanupLocations.LocalPath("Electronic Arts", "EA Desktop"));
            AddChromiumCachesUnder(items, "Ubisoft Connect", "ubisoft", CleanupLocations.LocalPath("Ubisoft Game Launcher"));
            AddChromiumCachesUnder(items, "Riot Client", "riot", CleanupLocations.LocalPath("Riot Games", "Riot Client"));
            AddChromiumCachesUnder(items, "GOG Galaxy", "gog", CleanupLocations.LocalPath("GOG.com", "Galaxy"));

            items.Add(new CleanupItem(
                "epic", "Приложения", "Epic Games Launcher",
                CleanupAction.Delete, CleanupSafety.Safe,
                EpicWebCaches()));

            items.Add(new CleanupItem(
                "npm", "Приложения", "npm cache",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.LocalPath("npm-cache", "_cacache") }));

            items.Add(new CleanupItem(
                "pip", "Приложения", "pip cache",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.LocalPath("pip", "Cache") }));

            items.Add(new CleanupItem(
                "yarn", "Приложения", "Yarn cache",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.LocalPath("Yarn", "Cache") }));

            items.Add(new CleanupItem(
                "nuget", "Приложения", "NuGet cache",
                CleanupAction.Delete, CleanupSafety.Safe,
                new[] { CleanupLocations.LocalPath("NuGet", "v3-cache") }));
        }

        private static IEnumerable<string> Bits(string root)
        {
            return new[]
            {
                Path.Combine(root, "Cache"),
                Path.Combine(root, "Code Cache"),
                Path.Combine(root, "GPUCache"),
            };
        }

        private static void AddCacheBits(List<CleanupItem> items, string name, string id, string root)
        {
            items.Add(new CleanupItem(
                "app." + id, "Приложения", name,
                CleanupAction.Delete, CleanupSafety.Safe,
                Bits(root)));
        }

        private static List<string> NewTeamsCaches()
        {
            var result = new List<string>();
            string webView = CleanupLocations.LocalPath("Packages", "MSTeams_8wekyb3d8bbwe",
                "LocalCache", "Microsoft", "MSTeams", "EBWebView");
            try
            {
                foreach (string directory in Directory.EnumerateDirectories(webView))
                {
                    string name = Path.GetFileName(directory);
                    if (name == "Default" || name.StartsWith("WV2Profile", StringComparison.OrdinalIgnoreCase))
                    {
                        result.AddRange(Bits(directory));
                    }
                }
            }
            catch
            {
            }

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static List<string> EpicWebCaches()
        {
            var result = new List<string>();
            string saved = CleanupLocations.LocalPath("EpicGamesLauncher", "Saved");
            try
            {
                foreach (string directory in Directory.EnumerateDirectories(saved))
                {
                    if (Path.GetFileName(directory).StartsWith("webcache", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(directory);
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        private static void AddChromiumCachesUnder(
            List<CleanupItem> items, string name, string id, string root)
        {
            items.Add(new CleanupItem(
                "app." + id, "Приложения", name,
                CleanupAction.Delete, CleanupSafety.Safe,
                FindChromiumCaches(root)));
        }

        /// <summary>
        /// Ищем только настоящие Chromium-кэши: рядом должны быть Code Cache,
        /// GPUCache или Cache\Cache_Data, иначе можно снести данные приложения.
        /// </summary>
        private static List<string> FindChromiumCaches(string root)
        {
            var result = new List<string>();
            if (!Directory.Exists(root))
            {
                return result;
            }

            var queue = new Queue<KeyValuePair<string, int>>();
            queue.Enqueue(new KeyValuePair<string, int>(root, 0));
            var names = new HashSet<string>(new[] { "Cache", "Code Cache", "GPUCache" },
                StringComparer.OrdinalIgnoreCase);

            while (queue.Count > 0)
            {
                KeyValuePair<string, int> current = queue.Dequeue();
                if (current.Value >= 6)
                {
                    continue;
                }

                IEnumerable<string> children;
                try
                {
                    children = Directory.EnumerateDirectories(current.Key).ToList();
                }
                catch
                {
                    continue;
                }

                foreach (string child in children)
                {
                    string name = Path.GetFileName(child);
                    if (names.Contains(name))
                    {
                        string parent = Path.GetDirectoryName(child);
                        bool chromium = Directory.Exists(Path.Combine(parent, "Code Cache"))
                            || Directory.Exists(Path.Combine(parent, "GPUCache"))
                            || Directory.Exists(Path.Combine(parent, "Cache", "Cache_Data"));
                        if (chromium
                            && !result.Any(found => child.StartsWith(found + "\\", StringComparison.OrdinalIgnoreCase)))
                        {
                            result.Add(child);
                        }
                    }
                    else
                    {
                        queue.Enqueue(new KeyValuePair<string, int>(child, current.Value + 1));
                    }
                }
            }

            return result;
        }

        // ------------------------------------------------------------- shaders ---

        private static void AddShaders(List<CleanupItem> items)
        {
            items.Add(new CleanupItem(
                "shaders.dx", "Кэш шейдеров", "DirectX",
                CleanupAction.Delete, CleanupSafety.Review,
                new[] { CleanupLocations.LocalPath("D3DSCache") }));

            items.Add(new CleanupItem(
                "shaders.nvidia", "Кэш шейдеров", "NVIDIA",
                CleanupAction.Delete, CleanupSafety.Review,
                new[]
                {
                    CleanupLocations.LocalPath("NVIDIA", "DXCache"),
                    CleanupLocations.LocalPath("NVIDIA", "GLCache"),
                    CleanupLocations.LocalPath("NVIDIA Corporation", "NV_Cache"),
                    CleanupLocations.ProgramDataPath("NVIDIA Corporation", "NV_Cache"),
                }));

            items.Add(new CleanupItem(
                "shaders.amd", "Кэш шейдеров", "AMD",
                CleanupAction.Delete, CleanupSafety.Review,
                new[]
                {
                    CleanupLocations.LocalPath("AMD", "DxCache"),
                    CleanupLocations.LocalPath("AMD", "DxcCache"),
                    CleanupLocations.LocalPath("AMD", "GLCache"),
                    CleanupLocations.LocalPath("AMD", "VkCache"),
                }));

            items.Add(new CleanupItem(
                "shaders.intel", "Кэш шейдеров", "Intel",
                CleanupAction.Delete, CleanupSafety.Review,
                new[] { CleanupLocations.LocalPath("Intel", "ShaderCache") }));

            items.Add(new CleanupItem(
                "shaders.steam", "Кэш шейдеров", "Steam",
                CleanupAction.Delete, CleanupSafety.Review,
                SteamShaderCaches()));
        }

        private static List<string> SteamShaderCaches()
        {
            var result = new List<string>();
            foreach (string library in SteamLibraries())
            {
                result.Add(Path.Combine(library, "steamapps", "shadercache"));
            }

            return result;
        }

        private static List<string> SteamLibraries()
        {
            var libraries = new List<string>();
            string root = null;
            try
            {
                using (RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    root = key == null ? null : key.GetValue("SteamPath") as string;
                }
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                root = Path.Combine(CleanupLocations.ProgramFilesX86, "Steam");
            }

            if (!Directory.Exists(root))
            {
                return libraries;
            }

            root = Path.GetFullPath(root.Replace('/', '\\')).TrimEnd('\\');
            libraries.Add(root);

            try
            {
                string vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdf))
                {
                    foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    {
                        libraries.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
                    }
                }
            }
            catch
            {
            }

            return libraries
                .Where(Directory.Exists)
                .Select(path => Path.GetFullPath(path).TrimEnd('\\'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // ------------------------------------------------------------ downloads ---

        private void AddDownloads(List<CleanupItem> items)
        {
            List<FileInfo> files = DownloadFiles();
            DateTime installerCutoff = DateTime.Now.AddDays(-7);
            DateTime downloadCutoff = DateTime.Now.AddDays(-30);

            foreach (FileInfo file in files)
            {
                try
                {
                    if (!InstallerExtensions.Contains(file.Extension))
                    {
                        continue;
                    }

                    // Только файлы, лежащие прямо в Downloads: .exe внутри
                    // распакованных папок — обычно портативные программы.
                    if (!string.Equals(
                        (file.DirectoryName ?? string.Empty).TrimEnd('\\'),
                        CleanupLocations.Downloads.TrimEnd('\\'),
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    DateTime touched = Touched(file);
                    if (touched > installerCutoff)
                    {
                        continue;
                    }

                    items.Add(new CleanupItem(
                        "installer." + file.FullName, "Загрузки", file.Name,
                        CleanupAction.Delete, CleanupSafety.Review,
                        new[] { file.FullName })
                    {
                        IsFile = true,
                        Detail = "Скачан " + Ago(touched),
                    });
                }
                catch
                {
                }
            }

            const long minSize = 250L * 1024L * 1024L;
            foreach (FileInfo file in files)
            {
                try
                {
                    if (file.Length < minSize || InstallerExtensions.Contains(file.Extension))
                    {
                        continue;
                    }

                    DateTime touched = Touched(file);
                    if (file.LastAccessTime > touched)
                    {
                        touched = file.LastAccessTime;
                    }

                    if (touched > downloadCutoff)
                    {
                        continue;
                    }

                    items.Add(new CleanupItem(
                        "download." + file.FullName, "Загрузки", file.Name,
                        CleanupAction.Delete, CleanupSafety.Review,
                        new[] { file.FullName })
                    {
                        IsFile = true,
                        Detail = "Не трогали " + Ago(touched),
                    });
                }
                catch
                {
                }
            }
        }

        private static List<FileInfo> DownloadFiles()
        {
            var result = new List<FileInfo>();
            try
            {
                var root = new DirectoryInfo(CleanupLocations.Downloads);
                if (!root.Exists)
                {
                    return result;
                }

                var stack = new Stack<string>();
                stack.Push(root.FullName);
                while (stack.Count > 0)
                {
                    string directory = stack.Pop();
                    IEnumerable<string> entries;
                    try
                    {
                        entries = Directory.EnumerateFileSystemEntries(directory).ToList();
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (string entry in entries)
                    {
                        try
                        {
                            FileAttributes attributes = File.GetAttributes(entry);
                            if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0)
                            {
                                continue;
                            }

                            if ((attributes & FileAttributes.Directory) != 0)
                            {
                                stack.Push(entry);
                            }
                            else
                            {
                                result.Add(new FileInfo(entry));
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }

            return result;
        }

        private static DateTime Touched(FileInfo file)
        {
            return file.CreationTime > file.LastWriteTime ? file.CreationTime : file.LastWriteTime;
        }

        private static string Ago(DateTime moment)
        {
            TimeSpan age = DateTime.Now - moment;
            if (age.TotalDays >= 365)
            {
                return ((int)(age.TotalDays / 365)).ToString(CultureInfo.CurrentCulture) + " г. назад";
            }

            if (age.TotalDays >= 30)
            {
                return ((int)(age.TotalDays / 30)).ToString(CultureInfo.CurrentCulture) + " мес. назад";
            }

            if (age.TotalDays >= 1)
            {
                return ((int)age.TotalDays).ToString(CultureInfo.CurrentCulture) + " дн. назад";
            }

            return "сегодня";
        }

        // ------------------------------------------------------------ leftovers ---

        private static void AddLeftovers(List<CleanupItem> items)
        {
            var item = new CleanupItem(
                "leftovers", "Остатки программ", "Папки удалённых приложений",
                CleanupAction.Delete, CleanupSafety.Review,
                new string[0])
            {
                WholeFolder = true,
            };
            item.PathProvider = delegate
            {
                List<LeftoverInfo> found = LeftoverFinder.Find();
                item.Size = found.Sum(value => value.Size);
                item.Files = found.Count;
                item.Detail = found.Count == 0
                    ? "Ничего не найдено"
                    : found.Count + " папок, " + FormatSize(item.Size);
                return found.Select(value => value.Path).ToList();
            };
            items.Add(item);
        }

        // ------------------------------------------------------------ hibernation ---

        private static void AddHibernation(List<CleanupItem> items)
        {
            string path = Path.Combine(CleanupLocations.Windows, "hiberfil.sys");
            if (!File.Exists(path))
            {
                return;
            }

            items.Add(new CleanupItem(
                "hibernation", "Система", "Файл гибернации (hiberfil.sys)",
                CleanupAction.HibernateOff, CleanupSafety.Review,
                new[] { path }));
        }

        private static long HibernationSize()
        {
            try
            {
                var file = new FileInfo(Path.Combine(CleanupLocations.Windows, "hiberfil.sys"));
                return file.Exists ? file.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool DisableHibernation()
        {
            ProcessOutput output = _processes.RunAsync(
                "powercfg.exe",
                "/hibernate off",
                120000,
                Encoding.UTF8).GetAwaiter().GetResult();

            if (!output.Ok)
            {
                _log.Warn(Category, "powercfg /hibernate off: " + output.Text.Trim());
                return false;
            }

            return true;
        }

        private long CleanupWinSxs()
        {
            ProcessOutput output = _processes.RunAsync(
                "dism.exe",
                "/Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                1800000,
                Encoding.UTF8).GetAwaiter().GetResult();

            _log.Info("Cleanup", "DISM ResetBase: код " + output.ExitCode);
            if (!output.Ok)
            {
                _log.Warn("Cleanup", "DISM ResetBase: " + output.Text.Trim());
            }

            return 0L;
        }

        private static void OpenSteamUninstall(string appId)
        {
            try
            {
                if (!string.IsNullOrEmpty(appId))
                {
                    System.Diagnostics.Process.Start("steam://uninstall/" + appId);
                }
            }
            catch
            {
            }
        }

        private static void OpenWindowsCleanup()
        {
            try
            {
                System.Diagnostics.Process.Start("cleanmgr.exe");
            }
            catch
            {
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RecycleBinInfo
        {
            public uint Size;
            public long Items;
            public long Bytes;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string rootPath, ref RecycleBinInfo info);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string rootPath, uint flags);

        private static long QueryRecycleBin()
        {
            var info = new RecycleBinInfo();
            info.Size = (uint)Marshal.SizeOf(typeof(RecycleBinInfo));
            return SHQueryRecycleBin(null, ref info) == 0 ? info.Bytes : 0L;
        }

        private static bool EmptyRecycleBin()
        {
            return SHEmptyRecycleBin(
                IntPtr.Zero,
                null,
                SherbNoConfirmation | SherbNoProgressUi | SherbNoSound) == 0;
        }
    }
}
