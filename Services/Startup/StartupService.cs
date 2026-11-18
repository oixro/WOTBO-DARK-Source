using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;

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
        ScheduledTask,
        UwpTask,
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

        /// <summary>Полный путь задачи планировщика для schtasks.</summary>
        public string TaskPath { get; set; }

        /// <summary>Путь к ключу UWP StartupTask.</summary>
        public string UwpKey { get; set; }

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

        private const string UwpDataPath =
            @"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData";

        private readonly IProcessService _processes;
        private readonly IAppLogger _log;

        public StartupService(IProcessService processes, IAppLogger log)
        {
            _processes = processes ?? throw new ArgumentNullException(nameof(processes));
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

        public Task<List<StartupEntry>> GetScheduledTasksAsync()
        {
            return Task.Run(() =>
            {
                var entries = new List<StartupEntry>();
                ReadScheduledTasks(entries);
                return entries;
            });
        }

        public Task<List<StartupEntry>> GetUwpTasksAsync()
        {
            return Task.Run(() =>
            {
                var entries = new List<StartupEntry>();
                ReadUwpTasks(entries);
                return entries;
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
            if (entry.Kind == StartupLocation.ScheduledTask)
            {
                return SetTaskEnabled(entry, enabled);
            }

            if (entry.Kind == StartupLocation.UwpTask)
            {
                return SetUwpEnabled(entry, enabled);
            }

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

        private bool SetTaskEnabled(StartupEntry entry, bool enabled)
        {
            string arguments = "/Change /TN \"" + entry.TaskPath + "\" "
                + (enabled ? "/ENABLE" : "/DISABLE");

            try
            {
                ProcessOutput output = _processes.RunAsync(
                    "schtasks.exe", arguments, 30000, Encoding.UTF8).GetAwaiter().GetResult();

                if (!output.Ok)
                {
                    _log.Warn(Category, "schtasks " + entry.TaskPath + ": " + output.Text.Trim());
                }

                return output.Ok;
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "schtasks " + entry.TaskPath + ": " + exception.Message);
                return false;
            }
        }

        private bool SetUwpEnabled(StartupEntry entry, bool enabled)
        {
            try
            {
                using (RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(entry.UwpKey, true))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    key.SetValue("State", enabled ? 2 : 1, RegistryValueKind.DWord);
                }

                _log.Info(Category, (enabled ? "включено: " : "выключено: ") + entry.Name);
                return true;
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "UWP " + entry.Name + ": " + exception.Message);
                return false;
            }
        }

        private void ReadScheduledTasks(List<StartupEntry> entries)
        {
            string script = Path.Combine(Path.GetTempPath(), "wotbo-startup-tasks.ps1");
            string outputFile = Path.Combine(Path.GetTempPath(), "wotbo-startup-tasks.txt");

            File.WriteAllText(
                script,
                "Get-ScheduledTask | ForEach-Object {\r\n"
                    + "  $startup = $_.Triggers | Where-Object { $_.CimClass.CimClassName -match 'Logon|Boot' }\r\n"
                    + "  if ($startup) { $_.TaskPath + [char]9 + $_.TaskName + [char]9 + $_.Settings.Enabled }\r\n"
                    + "} | Set-Content -LiteralPath '" + outputFile.Replace("'", "''") + "' -Encoding UTF8\r\n",
                Encoding.UTF8);

            try
            {
                File.Delete(outputFile);
            }
            catch
            {
            }

            try
            {
                ProcessOutput output = _processes.RunAsync(
                    "powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"",
                    30000,
                    Encoding.UTF8).GetAwaiter().GetResult();

                if (!File.Exists(outputFile))
                {
                    _log.Warn(Category, "список задач не получен: " + output.Text.Trim());
                    return;
                }

                foreach (string raw in File.ReadAllLines(outputFile, Encoding.UTF8))
                {
                    string[] parts = raw.Split('\t');
                    if (parts.Length < 3)
                    {
                        continue;
                    }

                    string taskPath = parts[0].Trim();
                    string taskName = parts[1].Trim();
                    bool enabled;
                    if (taskName.Length == 0 || !bool.TryParse(parts[2].Trim(), out enabled))
                    {
                        continue;
                    }

                    string fullPath = taskPath.TrimEnd('\\') + "\\" + taskName;
                    entries.Add(new StartupEntry
                    {
                        Name = taskName,
                        Command = fullPath,
                        Location = "Планировщик: " + taskPath,
                        Kind = StartupLocation.ScheduledTask,
                        TaskPath = fullPath,
                        Enabled = enabled,
                    });
                }
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "задачи планировщика: " + exception.Message);
            }
        }

        private void ReadUwpTasks(List<StartupEntry> entries)
        {
            try
            {
                using (RegistryKey baseKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UwpDataPath))
                {
                    if (baseKey == null)
                    {
                        return;
                    }

                    foreach (string family in baseKey.GetSubKeyNames())
                    {
                        using (RegistryKey familyKey = baseKey.OpenSubKey(family))
                        {
                            if (familyKey == null)
                            {
                                continue;
                            }

                            foreach (string task in familyKey.GetSubKeyNames())
                            {
                                using (RegistryKey taskKey = familyKey.OpenSubKey(task))
                                {
                                    object stateValue = taskKey == null ? null : taskKey.GetValue("State");
                                    if (!(stateValue is int))
                                    {
                                        continue;
                                    }

                                    int state = (int)stateValue;
                                    entries.Add(new StartupEntry
                                    {
                                        Name = family.Split('_')[0],
                                        Command = family + "\\" + task,
                                        Location = "UWP StartupTask",
                                        Kind = StartupLocation.UwpTask,
                                        UwpKey = UwpDataPath + "\\" + family + "\\" + task,
                                        Enabled = state == 2 || state == 4,
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "UWP StartupTask: " + exception.Message);
            }
        }

        private static int GroupOrder(StartupLocation kind)
        {
            if (kind == StartupLocation.ScheduledTask)
            {
                return 1;
            }

            if (kind == StartupLocation.UwpTask)
            {
                return 2;
            }

            return 0;
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
