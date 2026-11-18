using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;
using WOTBO.App.Logging;

namespace WOTBO.Services.Startup
{
    public enum StartupLocation
    {
        HkcuRun,
        HklmRun,
        HkcuRunOnce,
        HklmRunOnce,
        HklmRun32,
        HklmRunOnce32,
        UserFolder,
        CommonFolder,
    }

    /// <summary>Одна запись автозагрузки.</summary>
    public sealed class StartupEntry
    {
        public string Name { get; set; }

        public string Command { get; set; }

        public string Location { get; set; }

        public StartupLocation Kind { get; set; }

        public RegistryHive Hive { get; set; }

        public RegistryView View { get; set; }

        public string ApprovedKey { get; set; }

        public string ValueName { get; set; }

        public bool Enabled { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Автозагрузка: Run/RunOnce (HKCU, HKLM и 32-битная ветка), папки
    /// «Автозагрузка». Включение и выключение — через StartupApproved,
    /// как это делает диспетчер задач; сами записи не удаляются.
    /// </summary>
    public sealed class StartupService
    {
        private const string Category = "Startup";
        private const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunOncePath = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
        private const string ApprovedPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved";

        private readonly IAppLogger _log;

        public StartupService(IAppLogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public Task<List<StartupEntry>> GetAsync()
        {
            return Task.Run(() =>
            {
                var entries = new List<StartupEntry>();

                ReadRunKey(entries, RegistryHive.CurrentUser, RegistryView.Default, RunPath, StartupLocation.HkcuRun, "Run");
                ReadRunKey(entries, RegistryHive.LocalMachine, RegistryView.Default, RunPath, StartupLocation.HklmRun, "Run");
                ReadRunKey(entries, RegistryHive.CurrentUser, RegistryView.Default, RunOncePath, StartupLocation.HkcuRunOnce, "RunOnce");
                ReadRunKey(entries, RegistryHive.LocalMachine, RegistryView.Default, RunOncePath, StartupLocation.HklmRunOnce, "RunOnce");
                ReadRunKey(entries, RegistryHive.LocalMachine, RegistryView.Registry32, RunPath, StartupLocation.HklmRun32, "Run32");
                ReadRunKey(entries, RegistryHive.LocalMachine, RegistryView.Registry32, RunOncePath, StartupLocation.HklmRunOnce32, "RunOnce32");

                ReadFolder(entries, Environment.GetFolderPath(Environment.SpecialFolder.Startup), StartupLocation.UserFolder, RegistryHive.CurrentUser);
                ReadFolder(entries, Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), StartupLocation.CommonFolder, RegistryHive.LocalMachine);

                return entries
                    .OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            });
        }

        public Task<bool> SetEnabledAsync(StartupEntry entry, bool enabled)
        {
            if (entry == null)
            {
                return Task.FromResult(false);
            }

            return Task.Run(() => SetEnabled(entry, enabled));
        }

        private bool SetEnabled(StartupEntry entry, bool enabled)
        {
            try
            {
                string path = ApprovedPath + "\\" + entry.ApprovedKey;
                using (RegistryKey root = RegistryKey.OpenBaseKey(entry.Hive, RegistryView.Default))
                using (RegistryKey key = root.CreateSubKey(path))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    var data = new byte[12];
                    data[0] = enabled ? (byte)2 : (byte)3;
                    byte[] stamp = BitConverter.GetBytes(DateTime.UtcNow.ToFileTimeUtc());
                    Array.Copy(stamp, 0, data, 4, 8);

                    key.SetValue(entry.ValueName, data, RegistryValueKind.Binary);
                }

                _log.Info(Category, (enabled ? "включено: " : "выключено: ") + entry.Name);
                return true;
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "не удалось изменить " + entry.Name + ": " + exception.Message);
                return false;
            }
        }

        private void ReadRunKey(
            List<StartupEntry> entries,
            RegistryHive hive,
            RegistryView view,
            string path,
            StartupLocation kind,
            string approvedKey)
        {
            try
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, view))
                using (RegistryKey key = root.OpenSubKey(path))
                {
                    if (key == null)
                    {
                        return;
                    }

                    foreach (string name in key.GetValueNames())
                    {
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        string command = key.GetValue(name) as string;
                        if (string.IsNullOrWhiteSpace(command))
                        {
                            continue;
                        }

                        entries.Add(new StartupEntry
                        {
                            Name = name,
                            Command = command,
                            Location = HiveName(hive) + "\\" + path,
                            Kind = kind,
                            Hive = hive,
                            View = RegistryView.Default,
                            ApprovedKey = approvedKey,
                            ValueName = name,
                            Enabled = ReadEnabled(hive, approvedKey, name),
                        });
                    }
                }
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "чтение " + path + ": " + exception.Message);
            }
        }

        private void ReadFolder(List<StartupEntry> entries, string folder, StartupLocation kind, RegistryHive hive)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return;
            }

            try
            {
                foreach (string file in Directory.EnumerateFileSystemEntries(folder))
                {
                    string name = Path.GetFileName(file);
                    if (string.IsNullOrEmpty(name)
                        || string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    entries.Add(new StartupEntry
                    {
                        Name = name,
                        Command = file,
                        Location = kind == StartupLocation.CommonFolder
                            ? "Общая папка автозагрузки"
                            : "Папка автозагрузки",
                        Kind = kind,
                        Hive = hive,
                        View = RegistryView.Default,
                        ApprovedKey = "StartupFolder",
                        ValueName = name,
                        Enabled = ReadEnabled(hive, "StartupFolder", name),
                    });
                }
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "чтение папки " + folder + ": " + exception.Message);
            }
        }

        private bool ReadEnabled(RegistryHive hive, string approvedKey, string valueName)
        {
            try
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, RegistryView.Default))
                using (RegistryKey key = root.OpenSubKey(ApprovedPath + "\\" + approvedKey))
                {
                    byte[] data = key == null ? null : key.GetValue(valueName) as byte[];
                    if (data == null || data.Length == 0)
                    {
                        return true;
                    }

                    return data[0] == 2 || data[0] == 6;
                }
            }
            catch
            {
                return true;
            }
        }

        private static string HiveName(RegistryHive hive)
        {
            return hive == RegistryHive.CurrentUser ? "HKCU" : "HKLM";
        }
    }
}
