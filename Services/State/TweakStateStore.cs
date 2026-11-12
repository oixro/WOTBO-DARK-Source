using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Registry;

namespace WOTBO.Services.State
{
    /// <summary>
    /// Постоянное состояние твиков.
    ///
    /// Всё лежит в <c>HKCU\Software\oixro\wotbo\Backups\&lt;tweak-id&gt;</c>:
    /// <list type="bullet">
    /// <item><c>applied</c> — DWORD 1;</item>
    /// <item><c>appliedAt</c> — время применения;</item>
    /// <item><c>version</c> — версия WOTBO;</item>
    /// <item><c>data</c> — JSON с прежними значениями реестра и файлами.</item>
    /// </list>
    /// Файловые копии — в
    /// <c>%LOCALAPPDATA%\oixro\wotbo\backups\&lt;tweak-id&gt;</c>.
    ///
    /// Старые флаги из корня <c>HKCU\Software\oixro\wotbo</c> при первом
    /// обращении мигрируются в новый формат и удаляются.
    /// </summary>
    public sealed class TweakStateStore
    {
        private const string RootPath = RegistryPaths.AppRoot;
        private const string BackupsPath = RegistryPaths.BackupsRoot;
        private const string LogCategory = "State";

        /// <summary>Больше этого JSON уходит в файл: значение реестра ограничено.</summary>
        private const int MaxInlineJson = 900 * 1024;

        private static readonly Dictionary<string, LegacyFlag> LegacyFlags =
            new Dictionary<string, LegacyFlag>(StringComparer.OrdinalIgnoreCase)
            {
                { "main.regpack", new LegacyFlag("regpack", false) },
                { "main.reservedstorage", new LegacyFlag("ReservedStorageState", false) },
                { "main.hibernate", new LegacyFlag("hibernate", false) },
                { "main.powerscheme", new LegacyFlag("imported_powerscheme", false) },
                { "ui.animations", new LegacyFlag("animations", false) },
                { "main.mousefix", new LegacyFlag("mousefix", true) },
                { "main.usb", new LegacyFlag("usbpowersaving", false) },
                { "main.bcdedit", new LegacyFlag("bcdedit", false) },
                { "main.mitigations", new LegacyFlag("Mitigations", false) },
                { "main.mmagent", new LegacyFlag("mmagent", false) },
                { "ui.buttons", new LegacyFlag("buttons", false) },
                { "ui.explorerdetails", new LegacyFlag("litle_explorer_things", false) },
                { "pro.homegallery", new LegacyFlag("pro_1", false) },
                { "main.defender", new LegacyFlag("defenderdisabled", false) },
                { "pro.bufferbloat", new LegacyFlag("buffer", true) },
            };

        /// <summary>
        /// Твики, у которых бэкап принципиально невозможен или бессмысленен:
        /// DISM/сжатие/очистки/powercfg. Для них хранится только время
        /// применения, без data.
        /// </summary>
        private static readonly HashSet<string> NoBackupTweaks =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "main.reservedstorage",
                "main.hibernate",
                "main.powerscheme",
                "gpu.ansel",
                "gpu.directplay",
                "ui.animations",
                "extra.compactos",
                "extra.winsxs",
                "extra.tempclean",
                "extra.updateclean",
                "pro.bufferbloat",
                "ui.tweakTooltips",
            };

        /// <summary>Может ли твик иметь бэкап значений/файлов.</summary>
        public static bool SupportsBackup(string tweakId)
        {
            return !string.IsNullOrEmpty(tweakId) && !NoBackupTweaks.Contains(tweakId);
        }

        /// <summary>
        /// Вызывается после изменения состояния (apply/rollback). Подсказки
        /// сбрасывают кэш по этому id.
        /// </summary>
        public event Action<string> Changed;

        private void NotifyChanged(string tweakId)
        {
            Action<string> handler = Changed;
            if (handler != null)
            {
                handler(tweakId);
            }
        }

        private readonly IRegistryService _registry;
        private readonly IAppLogger _log;

        public TweakStateStore(IRegistryService registry, IAppLogger log)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>
        /// Применён ли твик. Если найден старый флаг, он мигрирует в новый
        /// формат и удаляется — повторного чтения старого значения не будет.
        /// </summary>
        public bool IsApplied(string tweakId)
        {
            string path = BackupKeyPath(tweakId);
            object applied = _registry.GetValue(RegistryHive.CurrentUser, path, "applied");
            if (applied != null && Convert.ToInt32(applied, CultureInfo.InvariantCulture) == 1)
            {
                return true;
            }

            LegacyFlag legacy;
            if (LegacyFlags.TryGetValue(tweakId, out legacy))
            {
                RegistryValueSnapshot old = _registry.ReadValue(RegistryHive.CurrentUser, RootPath, legacy.Name);
                if (old.Exists && (legacy.ExistsOnly || Convert.ToInt32(old.Data, CultureInfo.InvariantCulture) == 1))
                {
                    _log.Info(LogCategory, $"миграция старого флага {legacy.Name} -> {tweakId}");
                    MarkApplied(tweakId, null);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Привести сохранённое состояние к фактическому результату Detect.
        /// </summary>
        public void Reconcile(string tweakId, bool applied)
        {
            if (applied && !IsApplied(tweakId))
            {
                MarkApplied(tweakId, null);
            }
            else if (!applied && IsApplied(tweakId))
            {
                Remove(tweakId);
            }
        }

        public bool HasBackup(string tweakId)
        {
            object data = _registry.GetValue(RegistryHive.CurrentUser, BackupKeyPath(tweakId), "data");
            if (data is string && !string.IsNullOrEmpty((string)data))
            {
                return true;
            }

            object dataFile = _registry.GetValue(RegistryHive.CurrentUser, BackupKeyPath(tweakId), "dataFile");
            return dataFile is string && File.Exists((string)dataFile);
        }

        /// <summary>Время применения в ISO-8601 или <c>null</c>.</summary>
        public string GetAppliedAt(string tweakId)
        {
            return _registry.GetValue(RegistryHive.CurrentUser, BackupKeyPath(tweakId), "appliedAt") as string;
        }

        /// <summary>Дополнительные данные твика в его ключе состояния.</summary>
        public void WriteExtra(string tweakId, string name, object value)
        {
            _registry.WriteValue(RegistryHive.CurrentUser, BackupKeyPath(tweakId), name, value);
        }

        public object ReadExtra(string tweakId, string name)
        {
            return _registry.GetValue(RegistryHive.CurrentUser, BackupKeyPath(tweakId), name);
        }

        /// <summary>
        /// Переводит все известные старые флаги в новый формат и удаляет их.
        /// Вызывается один раз при старте, чтобы в корне
        /// <c>HKCU\Software\oixro\wotbo</c> не оставалось старых значений.
        /// </summary>
        public void MigrateLegacyFlags()
        {
            var migrated = new List<string>();
            foreach (string tweakId in LegacyFlags.Keys)
            {
                if (IsApplied(tweakId))
                {
                    migrated.Add(tweakId);
                }
            }

            if (migrated.Count > 0)
            {
                _log.Info(LogCategory, "старые флаги мигрированы: " + string.Join(", ", migrated));
            }
        }

        public TweakBackup LoadBackup(string tweakId)
        {
            object data = _registry.GetValue(RegistryHive.CurrentUser, BackupKeyPath(tweakId), "data");
            string json = data as string;

            if (string.IsNullOrEmpty(json))
            {
                object dataFile = _registry.GetValue(RegistryHive.CurrentUser, BackupKeyPath(tweakId), "dataFile");
                string path = dataFile as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try
                    {
                        json = File.ReadAllText(path, Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        _log.Error(LogCategory, $"не удалось прочитать файл бэкапа {tweakId}", ex);
                        return null;
                    }
                }
            }

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                return Deserialize(json);
            }
            catch (Exception ex)
            {
                _log.Error(LogCategory, $"не удалось прочитать бэкап {tweakId}", ex);
                return null;
            }
        }

        public void MarkApplied(string tweakId, TweakBackup backup)
        {
            string path = BackupKeyPath(tweakId);
            string appliedAt = backup != null && !string.IsNullOrEmpty(backup.AppliedAt)
                ? backup.AppliedAt
                : DateTime.Now.ToString("o", CultureInfo.InvariantCulture);

            _registry.WriteValue(RegistryHive.CurrentUser, path, "applied", 1);
            _registry.WriteValue(RegistryHive.CurrentUser, path, "appliedAt", appliedAt);
            _registry.WriteValue(RegistryHive.CurrentUser, path, "version", AppVersion());

            if (backup != null && backup.HasData)
            {
                string json = Serialize(backup);
                if (json.Length > MaxInlineJson)
                {
                    string directory = BackupDirectory(tweakId);
                    Directory.CreateDirectory(directory);
                    string file = Path.Combine(directory, "backup.json");
                    File.WriteAllText(file, json, Encoding.UTF8);
                    _registry.WriteValue(RegistryHive.CurrentUser, path, "dataFile", file);
                    _registry.DeleteValue(RegistryHive.CurrentUser, path, "data");
                    _log.Info(LogCategory, $"сохранён бэкап {tweakId} в файл: значений {backup.RegistryValues.Count}, файлов {backup.Files.Count}, каталогов {backup.Directories.Count}");
                }
                else
                {
                    _registry.WriteValue(RegistryHive.CurrentUser, path, "data", json);
                    _registry.DeleteValue(RegistryHive.CurrentUser, path, "dataFile");
                    _log.Info(LogCategory, $"сохранён бэкап {tweakId}: значений {backup.RegistryValues.Count}, файлов {backup.Files.Count}, каталогов {backup.Directories.Count}");
                }
            }
            else
            {
                _registry.DeleteValue(RegistryHive.CurrentUser, path, "data");
                _registry.DeleteValue(RegistryHive.CurrentUser, path, "dataFile");
                if (SupportsBackup(tweakId))
                {
                    _log.Warn(LogCategory, $"бэкап {tweakId} пуст: твик меняет систему вне RegistryService (regedit/DISM/файлы)");
                }
                else
                {
                    _log.Info(LogCategory, $"состояние {tweakId} сохранено без бэкапа (твик не поддерживает откат данных)");
                }
            }

            DeleteLegacyFlag(tweakId);
            NotifyChanged(tweakId);
        }

        /// <summary>
        /// Возвращает прежние значения и файлы, затем удаляет состояние.
        /// Вызывается после штатного обработчика отката, поэтому итог —
        /// ровно то, что было до применения.
        /// </summary>
        public void Restore(string tweakId)
        {
            TweakBackup backup = LoadBackup(tweakId);
            if (backup == null || !backup.HasData)
            {
                _log.Warn(LogCategory, $"для {tweakId} нет сохранённого бэкапа, остаётся штатный откат");
                Remove(tweakId);
                return;
            }

            foreach (RegistryBackupValue value in backup.RegistryValues)
            {
                try
                {
                    RegistryValueSnapshot snapshot = value.ToSnapshot();
                    if (snapshot.Exists)
                    {
                        _registry.WriteValue(snapshot.Hive, snapshot.KeyPath, snapshot.Name, snapshot.Data, snapshot.Kind.Value);
                    }
                    else
                    {
                        _registry.DeleteValue(snapshot.Hive, snapshot.KeyPath, snapshot.Name);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(LogCategory, $"не удалось восстановить значение {value.KeyPath}\\{value.Name}", ex);
                }
            }

            // Полные экспорты веток важнее отдельных значений: они
            // восстанавливают и подветки, и значения, которых не было в JSON.
            if (backup.RegistryExports != null)
            {
                foreach (RegistryExportEntry export in backup.RegistryExports)
                {
                    if (!export.Existed)
                    {
                        continue;
                    }

                    try
                    {
                        if (string.IsNullOrEmpty(export.ExportPath) || !File.Exists(export.ExportPath))
                        {
                            _log.Warn(LogCategory, $"экспорт ветки не найден: {export.KeyPath}");
                            continue;
                        }

                        RunReg("import \"" + export.ExportPath + "\"");
                    }
                    catch (Exception ex)
                    {
                        _log.Error(LogCategory, $"не удалось импортировать ветку {export.KeyPath}", ex);
                    }
                }

                foreach (RegistryExportEntry export in backup.RegistryExports)
                {
                    if (export.Existed)
                    {
                        continue;
                    }

                    try
                    {
                        RegistryHive hive;
                        if (Enum.TryParse(export.Hive, true, out hive))
                        {
                            _registry.DeleteKey(hive, export.KeyPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(LogCategory, $"не удалось удалить созданную ветку {export.KeyPath}", ex);
                    }
                }

                // reg import только добавляет/перезаписывает. Удаляем значения
                // и подветки, которых не было на момент экспорта.
                foreach (RegistryExportEntry export in backup.RegistryExports)
                {
                    if (!export.Existed || export.Shape == null)
                    {
                        continue;
                    }

                    try
                    {
                        RegistryHive hive;
                        if (Enum.TryParse(export.Hive, true, out hive))
                        {
                            PruneShape(hive, export.Shape);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(LogCategory, $"не удалось очистить лишнее в ветке {export.KeyPath}", ex);
                    }
                }
            }

            foreach (FileBackupEntry file in backup.Files)
            {
                try
                {
                    RestoreFile(file);
                }
                catch (Exception ex)
                {
                    _log.Error(LogCategory, $"не удалось восстановить файл {file.OriginalPath}", ex);
                }
            }

            if (backup.Directories != null)
            {
                foreach (DirectoryBackupEntry directory in backup.Directories)
                {
                    try
                    {
                        if (!directory.Existed && Directory.Exists(directory.Path))
                        {
                            Directory.Delete(directory.Path, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(LogCategory, $"не удалось удалить созданный каталог {directory.Path}", ex);
                    }
                }
            }

            _log.Info(LogCategory, $"восстановлен бэкап {tweakId}: значений {backup.RegistryValues.Count}, файлов {backup.Files.Count}");
            Remove(tweakId);
        }

        public void Remove(string tweakId)
        {
            _registry.DeleteKey(RegistryHive.CurrentUser, BackupKeyPath(tweakId));
            DeleteLegacyFlag(tweakId);
            DeleteBackupDirectory(tweakId);
            NotifyChanged(tweakId);
        }

        /// <summary>Каталог файловых копий конкретного твика.</summary>
        public string BackupDirectory(string tweakId)
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "oixro",
                "wotbo",
                "backups");
            return Path.Combine(root, SafeName(tweakId));
        }

        private void PruneShape(RegistryHive hive, RegistryKeyShape shape)
        {
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
            using (RegistryKey key = baseKey.OpenSubKey(shape.Path, true))
            {
                if (key == null)
                {
                    return;
                }

                var expectedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (shape.ValueNames != null)
                {
                    foreach (string name in shape.ValueNames)
                    {
                        expectedValues.Add(name);
                    }
                }

                foreach (string name in key.GetValueNames())
                {
                    if (!expectedValues.Contains(name))
                    {
                        key.DeleteValue(name, false);
                        _log.Warn(LogCategory, "откат: удалено лишнее значение " + shape.Path + "\\" + name);
                    }
                }

                var expectedSubKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (shape.SubKeys != null)
                {
                    foreach (RegistryKeyShape sub in shape.SubKeys)
                    {
                        expectedSubKeys.Add(LeafName(sub.Path));
                    }
                }

                foreach (string subName in key.GetSubKeyNames())
                {
                    if (!expectedSubKeys.Contains(subName))
                    {
                        key.DeleteSubKeyTree(subName, false);
                        _log.Warn(LogCategory, "откат: удалена лишняя подветка " + shape.Path + "\\" + subName);
                    }
                }
            }

            if (shape.SubKeys != null)
            {
                foreach (RegistryKeyShape sub in shape.SubKeys)
                {
                    PruneShape(hive, sub);
                }
            }
        }

        private static string LeafName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            int index = path.LastIndexOf('\\');
            return index < 0 ? path : path.Substring(index + 1);
        }

        private static void RunReg(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "reg.exe",
                Arguments = arguments,
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

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "reg.exe завершился с кодом " + process.ExitCode + ": " + arguments);
                }
            }
        }

        private void RestoreFile(FileBackupEntry file)
        {
            if (file.Existed)
            {
                if (!File.Exists(file.BackupPath))
                {
                    _log.Warn(LogCategory, $"файловый бэкап не найден: {file.BackupPath}");
                    return;
                }

                string directory = Path.GetDirectoryName(file.OriginalPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(file.BackupPath, file.OriginalPath, true);
                return;
            }

            if (File.Exists(file.OriginalPath))
            {
                File.Delete(file.OriginalPath);
            }
        }

        private void DeleteLegacyFlag(string tweakId)
        {
            LegacyFlag legacy;
            if (!LegacyFlags.TryGetValue(tweakId, out legacy))
            {
                return;
            }

            _registry.DeleteValue(RegistryHive.CurrentUser, RootPath, legacy.Name);
        }

        private void DeleteBackupDirectory(string tweakId)
        {
            try
            {
                string directory = BackupDirectory(tweakId);
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
            catch (Exception ex)
            {
                _log.Warn(LogCategory, $"не удалось удалить каталог бэкапа {tweakId}: {ex.Message}");
            }
        }

        private static string BackupKeyPath(string tweakId)
        {
            return BackupsPath + "\\" + tweakId;
        }

        private static string SafeName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                builder.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            return builder.ToString();
        }

        private static string AppVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private static string Serialize(TweakBackup backup)
        {
            var serializer = new DataContractJsonSerializer(typeof(TweakBackup));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, backup);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static TweakBackup Deserialize(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(TweakBackup));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (TweakBackup)serializer.ReadObject(stream);
            }
        }

        private sealed class LegacyFlag
        {
            public LegacyFlag(string name, bool existsOnly)
            {
                Name = name;
                ExistsOnly = existsOnly;
            }

            public string Name { get; }

            /// <summary>Старый код проверял само наличие значения, а не «== 1».</summary>
            public bool ExistsOnly { get; }
        }
    }
}
