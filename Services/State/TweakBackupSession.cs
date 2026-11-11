using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Registry;

namespace WOTBO.Services.State
{
    /// <summary>
    /// Сборщик бэкапа текущего применения твика.
    ///
    /// <see cref="RegistryService"/> при каждой записи/удалении сообщает сюда
    /// старое значение; файловые операции вызывают <see cref="BackupFile"/>.
    /// На выходе из <c>TweakScope.Apply</c> накопленное сохраняется в
    /// <see cref="TweakStateStore"/>.
    ///
    /// Запись привязана к потоку: apply-блоки выполняются в UI-потоке.
    /// </summary>
    public sealed class TweakBackupSession
    {
        private const string LogCategory = "State";

        [ThreadStatic]
        private static TweakBackupSession _current;

        private readonly string _tweakId;
        private readonly string _backupDirectory;
        private readonly List<RegistryBackupValue> _values = new List<RegistryBackupValue>();
        private readonly List<FileBackupEntry> _files = new List<FileBackupEntry>();
        private readonly List<DirectoryBackupEntry> _directories = new List<DirectoryBackupEntry>();
        private readonly List<RegistryExportEntry> _exports = new List<RegistryExportEntry>();
        private readonly HashSet<string> _valueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _fileKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _directoryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _exportKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly DateTime _startedAt = DateTime.Now;

        private TweakBackupSession(string tweakId, string backupDirectory)
        {
            _tweakId = tweakId;
            _backupDirectory = backupDirectory;
        }

        public static TweakBackupSession Current
        {
            get { return _current; }
        }

        public static TweakBackupSession Begin(string tweakId, string backupDirectory)
        {
            if (_current != null)
            {
                Log.Warn(LogCategory, $"бэкап {_current._tweakId} не завершён, начинается {tweakId}");
            }

            _current = new TweakBackupSession(tweakId, backupDirectory);
            return _current;
        }

        /// <summary>Вызывается из <see cref="RegistryService"/> перед изменением.</summary>
        public static void RecordValue(RegistryValueSnapshot before)
        {
            TweakBackupSession current = _current;
            if (current != null && before != null)
            {
                current.Record(before);
            }
        }

        public static void RecordFile(string path)
        {
            TweakBackupSession current = _current;
            if (current != null)
            {
                current.BackupFile(path);
            }
        }

        public static void RecordDirectory(string path)
        {
            TweakBackupSession current = _current;
            if (current != null)
            {
                current.BackupDirectory(path);
            }
        }

        /// <summary>
        /// Разбирает .reg-файл и экспортирует все затронутые ветки через
        /// <c>reg.exe export</c>. Вызывать до <c>regedit /s</c>.
        /// </summary>
        public static void RecordRegFile(string regFilePath)
        {
            TweakBackupSession current = _current;
            if (current != null)
            {
                current.BackupRegFile(regFilePath);
            }
        }

        public static void End()
        {
            _current = null;
        }

        public TweakBackup ToBackup()
        {
            return new TweakBackup
            {
                TweakId = _tweakId,
                AppliedAt = _startedAt.ToString("o", CultureInfo.InvariantCulture),
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                RegistryValues = new List<RegistryBackupValue>(_values),
                Files = new List<FileBackupEntry>(_files),
                Directories = new List<DirectoryBackupEntry>(_directories),
                RegistryExports = new List<RegistryExportEntry>(_exports),
            };
        }

        public void Record(RegistryValueSnapshot before)
        {
            string key = string.Format(
                CultureInfo.InvariantCulture,
                "{0}\\{1}\\{2}",
                before.Hive,
                before.KeyPath,
                before.Name);

            // Первое (самое старое) значение важнее: повторная запись в том же
            // твике не должна затирать исходное состояние.
            if (!_valueKeys.Add(key))
            {
                return;
            }

            _values.Add(RegistryBackupValue.FromSnapshot(before));
        }

        /// <summary>
        /// Копирует файл в каталог бэкапа. Если файла нет, запоминает это,
        /// чтобы откат удалил созданный твиком файл.
        /// </summary>
        public bool BackupFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string fullPath = Path.GetFullPath(path);
            if (!_fileKeys.Add(fullPath))
            {
                return true;
            }

            var entry = new FileBackupEntry
            {
                OriginalPath = fullPath,
                Existed = File.Exists(fullPath),
            };

            if (!entry.Existed)
            {
                _files.Add(entry);
                return true;
            }

            try
            {
                Directory.CreateDirectory(_backupDirectory);
                string name = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:D3}_{1}",
                    _files.Count,
                    Path.GetFileName(fullPath));
                string destination = Path.Combine(_backupDirectory, name);
                File.Copy(fullPath, destination, true);

                entry.BackupPath = destination;
                entry.Length = new FileInfo(destination).Length;
                _files.Add(entry);
                Log.Debug(LogCategory, $"файл сохранён в бэкап: {fullPath} -> {destination}");
                return true;
            }
            catch (Exception ex)
            {
                // Запись всё равно нужна: откат хотя бы попробует сообщить,
                // что копии нет.
                _files.Add(entry);
                Log.Error(LogCategory, $"не удалось сохранить файл в бэкап: {fullPath}", ex);
                return false;
            }
        }

        public string TweakId
        {
            get { return _tweakId; }
        }

        /// <summary>
        /// Запоминает каталог и рекурсивно копирует его файлы. Если каталога
        /// не было, откат удалит созданное дерево.
        /// </summary>
        public void BackupDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string fullPath = Path.GetFullPath(path).TrimEnd('\\');
            if (!_directoryKeys.Add(fullPath))
            {
                return;
            }

            _directories.Add(new DirectoryBackupEntry
            {
                Path = fullPath,
                Existed = Directory.Exists(fullPath),
            });

            if (!Directory.Exists(fullPath))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
            {
                BackupFile(file);
            }
        }

        /// <summary>Полный экспорт ветки реестра через reg.exe.</summary>
        public bool BackupRegistryKey(RegistryHive hive, string keyPath)
        {
            if (string.IsNullOrEmpty(keyPath))
            {
                return false;
            }

            string normalizedPath = keyPath.Trim('\\');
            string key = hive + "\\" + normalizedPath;
            if (!_exportKeys.Add(key))
            {
                return true;
            }

            var entry = new RegistryExportEntry
            {
                Hive = hive.ToString(),
                KeyPath = normalizedPath,
            };

            try
            {
                Directory.CreateDirectory(_backupDirectory);
                string file = Path.Combine(
                    _backupDirectory,
                    string.Format(CultureInfo.InvariantCulture, "{0:D3}_key.reg", _exports.Count));

                var psi = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = string.Format(
                        CultureInfo.InvariantCulture,
                        "export \"{0}\\{1}\" \"{2}\" /y",
                        HiveName(hive),
                        normalizedPath,
                        file),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (Process process = Process.Start(psi))
                {
                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && File.Exists(file))
                    {
                        entry.Existed = true;
                        entry.ExportPath = file;
                        entry.Shape = CaptureShape(hive, normalizedPath);
                        _exports.Add(entry);
                        Log.Debug(LogCategory, $"ветка сохранена в бэкап: {key} -> {file}");
                        return true;
                    }
                }

                entry.Existed = KeyExists(hive, normalizedPath);
                if (entry.Existed)
                {
                    entry.Shape = CaptureShape(hive, normalizedPath);
                }

                _exports.Add(entry);
                if (entry.Existed)
                {
                    Log.Error(LogCategory, $"reg.exe не смог экспортировать существующую ветку {key}", null);
                }
                else
                {
                    Log.Debug(LogCategory, $"ветки {key} нет, откат её удалит");
                }

                return !entry.Existed;
            }
            catch (Exception ex)
            {
                entry.Existed = KeyExists(hive, normalizedPath);
                _exports.Add(entry);
                Log.Error(LogCategory, $"не удалось экспортировать ветку {key}", ex);
                return false;
            }
        }

        public void BackupRegFile(string regFilePath)
        {
            if (string.IsNullOrEmpty(regFilePath) || !File.Exists(regFilePath))
            {
                return;
            }

            string text;
            try
            {
                text = File.ReadAllText(regFilePath);
            }
            catch (Exception ex)
            {
                Log.Error(LogCategory, $"не удалось прочитать .reg для бэкапа: {regFilePath}", ex);
                return;
            }

            foreach (string rawLine in text.Split('\n'))
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("[", StringComparison.Ordinal))
                {
                    continue;
                }

                int end = line.IndexOf(']');
                if (end < 0)
                {
                    continue;
                }

                // [-HKEY...] — ветка удаляется; её тоже надо сохранить.
                string section = line.Substring(1, end - 1).TrimStart('-').Trim();
                int slash = section.IndexOf('\\');
                if (slash < 0)
                {
                    continue; // корень улья целиком не экспортируем
                }

                string hiveName = section.Substring(0, slash);
                string keyPath = section.Substring(slash + 1);
                RegistryHive hive;
                if (TryParseHive(hiveName, out hive))
                {
                    BackupRegistryKey(hive, keyPath);
                }
            }
        }

        private static bool KeyExists(RegistryHive hive, string keyPath)
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
            using (RegistryKey key = baseKey.OpenSubKey(keyPath, false))
            {
                return key != null;
            }
        }

        private static RegistryKeyShape CaptureShape(RegistryHive hive, string keyPath)
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
            using (RegistryKey key = baseKey.OpenSubKey(keyPath, false))
            {
                return key == null ? null : CaptureShape(key, keyPath);
            }
        }

        private static RegistryKeyShape CaptureShape(RegistryKey key, string path)
        {
            var shape = new RegistryKeyShape
            {
                Path = path,
                ValueNames = key.GetValueNames(),
            };

            foreach (string subName in key.GetSubKeyNames())
            {
                using (RegistryKey subKey = key.OpenSubKey(subName, false))
                {
                    if (subKey != null)
                    {
                        shape.SubKeys.Add(CaptureShape(subKey, path + "\\" + subName));
                    }
                }
            }

            return shape;
        }

        private static string HiveName(RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.CurrentUser: return "HKEY_CURRENT_USER";
                case RegistryHive.ClassesRoot: return "HKEY_CLASSES_ROOT";
                case RegistryHive.Users: return "HKEY_USERS";
                case RegistryHive.CurrentConfig: return "HKEY_CURRENT_CONFIG";
                default: return "HKEY_LOCAL_MACHINE";
            }
        }

        private static bool TryParseHive(string name, out RegistryHive hive)
        {
            switch (name.Trim().ToUpperInvariant())
            {
                case "HKEY_CURRENT_USER":
                case "HKCU":
                    hive = RegistryHive.CurrentUser;
                    return true;
                case "HKEY_LOCAL_MACHINE":
                case "HKLM":
                    hive = RegistryHive.LocalMachine;
                    return true;
                case "HKEY_CLASSES_ROOT":
                case "HKCR":
                    hive = RegistryHive.ClassesRoot;
                    return true;
                case "HKEY_USERS":
                case "HKU":
                    hive = RegistryHive.Users;
                    return true;
                case "HKEY_CURRENT_CONFIG":
                case "HKCC":
                    hive = RegistryHive.CurrentConfig;
                    return true;
                default:
                    hive = RegistryHive.LocalMachine;
                    return false;
            }
        }
    }
}
