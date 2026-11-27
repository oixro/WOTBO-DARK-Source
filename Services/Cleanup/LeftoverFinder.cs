using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace WOTBO.Services.Cleanup
{
    internal sealed class LeftoverInfo
    {
        public string Path;
        public long Size;
        public DateTime LastChange;
    }

    /// <summary>
    /// Папки в AppData, которые, скорее всего, остались от удалённых программ:
    /// имя ни на что установленное не похоже, папка не менялась 90 дней и
    /// весит больше 50 МБ. Всегда «на усмотрение пользователя», без галочки.
    /// Логика повторяет LeftoverFinder из Roomy (MIT).
    /// </summary>
    internal static class LeftoverFinder
    {
        private const long MinSize = 50L * 1024L * 1024L;
        private static readonly TimeSpan MinIdle = TimeSpan.FromDays(90);

        private static readonly HashSet<string> Never = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Microsoft", "Windows", "Packages", "Temp", "Programs", "Publishers", "Comms",
            "ConnectedDevicesPlatform", "D3DSCache", "CrashDumps", "IsolatedStorage", "VirtualStore",
            "PlaceholderTileLogoFolder", "Package Cache", "Microsoft Help", "Microsoft_Corporation",
            "MicrosoftEdge", "History", "Application Data", "NVIDIA", "NVIDIA Corporation", "AMD",
            "ATI", "Intel", "Realtek", "npm", "npm-cache", "pip", "Yarn", "NuGet", "pnpm",
            "pnpm-cache", "node-gyp", "Python", "Java", "Sun", "Oracle", "Adobe", "Google",
            "Mozilla", "BraveSoftware", "Opera Software", "Vivaldi", "Chromium", "Steam",
            "Discord", "Spotify", "Slack", "Code", "Battle.net", "EpicGamesLauncher",
            "Electronic Arts", "Ubisoft Game Launcher", "Riot Games", "GOG.com", "WOTBO",
            "SquirrelTemp", "Downloaded Installations", "ElevatedDiagnostics",
        };

        public static List<LeftoverInfo> Find()
        {
            var result = new List<LeftoverInfo>();
            HashSet<string> installed = InstalledNames();

            foreach (string root in Roots())
            {
                IEnumerable<string> directories;
                try
                {
                    directories = Directory.EnumerateDirectories(root).ToList();
                }
                catch
                {
                    continue;
                }

                foreach (string directory in directories)
                {
                    string name;
                    try
                    {
                        name = Path.GetFileName(directory);
                    }
                    catch
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(name)
                        || name.StartsWith(".", StringComparison.Ordinal)
                        || name.StartsWith("{", StringComparison.Ordinal)
                        || Never.Contains(name)
                        || BelongsToInstalled(name, installed))
                    {
                        continue;
                    }

                    try
                    {
                        FileAttributes attributes = File.GetAttributes(directory);
                        if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.System)) != 0)
                        {
                            continue;
                        }

                        long size;
                        DateTime newest;
                        Measure(directory, out size, out newest);
                        if (size < MinSize || DateTime.UtcNow - newest < MinIdle)
                        {
                            continue;
                        }

                        result.Add(new LeftoverInfo
                        {
                            Path = directory,
                            Size = size,
                            LastChange = newest.ToLocalTime(),
                        });
                    }
                    catch
                    {
                        // нет доступа — пропускаем
                    }
                }
            }

            return result.OrderByDescending(item => item.Size).ToList();
        }

        private static IEnumerable<string> Roots()
        {
            yield return CleanupLocations.Local;
            yield return CleanupLocations.Roaming;
            string localLow = Path.Combine(
                Path.GetDirectoryName(CleanupLocations.Local) ?? CleanupLocations.Local,
                "LocalLow");
            yield return localLow;
        }

        private static HashSet<string> InstalledNames()
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            void AddName(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                string normalized = Normalize(value);
                if (normalized.Length >= 3)
                {
                    names.Add(normalized);
                }

                foreach (string word in value.Split(new[] { ' ', '.', '_', '-', '(', ')', ',' },
                    StringSplitOptions.RemoveEmptyEntries))
                {
                    string normalizedWord = Normalize(word);
                    if (normalizedWord.Length >= 4)
                    {
                        names.Add(normalizedWord);
                    }
                }
            }

            foreach (RegistryHive hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            {
                foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
                {
                    try
                    {
                        using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view))
                        using (RegistryKey root = baseKey.OpenSubKey(
                            @"Software\Microsoft\Windows\CurrentVersion\Uninstall"))
                        {
                            if (root == null)
                            {
                                continue;
                            }

                            foreach (string keyName in root.GetSubKeyNames())
                            {
                                using (RegistryKey key = root.OpenSubKey(keyName))
                                {
                                    AddName(keyName);
                                    if (key == null)
                                    {
                                        continue;
                                    }

                                    AddName(key.GetValue("DisplayName") as string);
                                    AddName(key.GetValue("Publisher") as string);
                                    string location = key.GetValue("InstallLocation") as string;
                                    if (!string.IsNullOrEmpty(location) && location.Length > 3)
                                    {
                                        AddName(Path.GetFileName(location.TrimEnd('\\')));
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            try
            {
                foreach (string directory in Directory.EnumerateDirectories(
                    CleanupLocations.LocalPath("Packages")))
                {
                    AddName(Path.GetFileName(directory).Split('_')[0]);
                }
            }
            catch
            {
            }

            foreach (string root in new[]
            {
                CleanupLocations.ProgramFiles,
                CleanupLocations.ProgramFilesX86,
                CleanupLocations.LocalPath("Programs"),
            })
            {
                try
                {
                    foreach (string directory in Directory.EnumerateDirectories(root))
                    {
                        AddName(Path.GetFileName(directory));
                    }
                }
                catch
                {
                }
            }

            return names;
        }

        private static bool BelongsToInstalled(string folderName, HashSet<string> installed)
        {
            string name = Normalize(folderName);
            if (name.Length < 4)
            {
                return true;
            }

            foreach (string installedName in installed)
            {
                if ((installedName.Length >= 4
                        && name.IndexOf(installedName, StringComparison.Ordinal) >= 0)
                    || installedName.IndexOf(name, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Measure(string directory, out long size, out DateTime newest)
        {
            size = 0;
            newest = Directory.GetLastWriteTimeUtc(directory);
            var stack = new Stack<string>();
            stack.Push(directory);

            while (stack.Count > 0)
            {
                string current = stack.Pop();
                IEnumerable<string> entries;
                try
                {
                    entries = Directory.EnumerateFileSystemEntries(current).ToList();
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
                        if ((attributes & FileAttributes.ReparsePoint) != 0)
                        {
                            continue;
                        }

                        if ((attributes & FileAttributes.Directory) != 0)
                        {
                            stack.Push(entry);
                            continue;
                        }

                        var file = new FileInfo(entry);
                        size += file.Length;
                        if (file.LastWriteTimeUtc > newest)
                        {
                            newest = file.LastWriteTimeUtc;
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static string Normalize(string value)
        {
            return new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        }
    }
}
