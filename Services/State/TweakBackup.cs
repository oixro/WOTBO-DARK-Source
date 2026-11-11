using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.State
{
    /// <summary>
    /// Постоянный бэкап одного твика. Хранится в
    /// <c>HKCU\Software\oixro\wotbo\Backups\&lt;tweak-id&gt;</c> как JSON,
    /// файловые копии — в <c>%LOCALAPPDATA%\oixro\wotbo\backups\&lt;tweak-id&gt;</c>.
    /// </summary>
    [DataContract]
    public sealed class TweakBackup
    {
        [DataMember(Order = 1)]
        public string TweakId { get; set; }

        [DataMember(Order = 2)]
        public string AppliedAt { get; set; }

        [DataMember(Order = 3)]
        public string AppVersion { get; set; }

        /// <summary>Изменённые значения реестра: ровно то, что было до твика.</summary>
        [DataMember(Order = 4)]
        public List<RegistryBackupValue> RegistryValues { get; set; }

        [DataMember(Order = 5)]
        public List<FileBackupEntry> Files { get; set; }

        [DataMember(Order = 6)]
        public List<DirectoryBackupEntry> Directories { get; set; }

        /// <summary>Полные экспорты веток (.reg), снятые через reg.exe.</summary>
        [DataMember(Order = 7)]
        public List<RegistryExportEntry> RegistryExports { get; set; }

        public TweakBackup()
        {
            RegistryValues = new List<RegistryBackupValue>();
            Files = new List<FileBackupEntry>();
            Directories = new List<DirectoryBackupEntry>();
            RegistryExports = new List<RegistryExportEntry>();
        }

        public bool HasData
        {
            get
            {
                return (RegistryValues != null && RegistryValues.Count > 0)
                    || (Files != null && Files.Count > 0)
                    || (Directories != null && Directories.Count > 0)
                    || (RegistryExports != null && RegistryExports.Count > 0);
            }
        }
    }

    [DataContract]
    public sealed class RegistryBackupValue
    {
        [DataMember(Order = 1)]
        public string Hive { get; set; }

        [DataMember(Order = 2)]
        public string KeyPath { get; set; }

        [DataMember(Order = 3)]
        public string Name { get; set; }

        [DataMember(Order = 4)]
        public bool Existed { get; set; }

        [DataMember(Order = 5)]
        public int Kind { get; set; }

        [DataMember(Order = 6)]
        public string Text { get; set; }

        [DataMember(Order = 7)]
        public string[] Lines { get; set; }

        [DataMember(Order = 8)]
        public byte[] Bytes { get; set; }

        public static RegistryBackupValue FromSnapshot(RegistryValueSnapshot snapshot)
        {
            var result = new RegistryBackupValue
            {
                Hive = snapshot.Hive.ToString(),
                KeyPath = snapshot.KeyPath,
                Name = snapshot.Name,
                Existed = snapshot.Exists,
                Kind = snapshot.Kind.HasValue ? (int)snapshot.Kind.Value : (int)RegistryValueKind.Unknown,
            };

            if (!snapshot.Exists)
            {
                return result;
            }

            switch (snapshot.Kind.Value)
            {
                case RegistryValueKind.MultiString:
                    result.Lines = snapshot.Data as string[];
                    break;

                case RegistryValueKind.Binary:
                    result.Bytes = snapshot.Data as byte[];
                    break;

                case RegistryValueKind.DWord:
                    result.Text = Convert.ToString(Convert.ToInt32(snapshot.Data, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                    break;

                case RegistryValueKind.QWord:
                    result.Text = Convert.ToString(Convert.ToInt64(snapshot.Data, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                    break;

                default:
                    result.Text = Convert.ToString(snapshot.Data, CultureInfo.InvariantCulture);
                    break;
            }

            return result;
        }

        public RegistryValueSnapshot ToSnapshot()
        {
            RegistryHive hive;
            if (!Enum.TryParse(Hive, true, out hive))
            {
                hive = RegistryHive.CurrentUser;
            }

            if (!Existed)
            {
                return RegistryValueSnapshot.Missing(hive, KeyPath, Name);
            }

            RegistryValueKind valueKind = (RegistryValueKind)Kind;
            object data;
            switch (valueKind)
            {
                case RegistryValueKind.MultiString:
                    data = Lines ?? new string[0];
                    break;

                case RegistryValueKind.Binary:
                    data = Bytes ?? new byte[0];
                    break;

                case RegistryValueKind.DWord:
                    data = int.Parse(Text, CultureInfo.InvariantCulture);
                    break;

                case RegistryValueKind.QWord:
                    data = long.Parse(Text, CultureInfo.InvariantCulture);
                    break;

                default:
                    data = Text ?? string.Empty;
                    break;
            }

            return RegistryValueSnapshot.Present(hive, KeyPath, Name, valueKind, data);
        }
    }

    [DataContract]
    public sealed class FileBackupEntry
    {
        [DataMember(Order = 1)]
        public string OriginalPath { get; set; }

        [DataMember(Order = 2)]
        public string BackupPath { get; set; }

        [DataMember(Order = 3)]
        public bool Existed { get; set; }

        [DataMember(Order = 4)]
        public long Length { get; set; }
    }

    [DataContract]
    public sealed class DirectoryBackupEntry
    {
        [DataMember(Order = 1)]
        public string Path { get; set; }

        [DataMember(Order = 2)]
        public bool Existed { get; set; }
    }

    [DataContract]
    public sealed class RegistryExportEntry
    {
        [DataMember(Order = 1)]
        public string Hive { get; set; }

        [DataMember(Order = 2)]
        public string KeyPath { get; set; }

        [DataMember(Order = 3)]
        public string ExportPath { get; set; }

        [DataMember(Order = 4)]
        public bool Existed { get; set; }

        /// <summary>
        /// Имена значений и подветок на момент экспорта. Нужны, чтобы после
        /// импорта удалить то, что твик создал заново (reg import не удаляет
        /// лишние значения).
        /// </summary>
        [DataMember(Order = 5)]
        public RegistryKeyShape Shape { get; set; }
    }

    [DataContract]
    public sealed class RegistryKeyShape
    {
        [DataMember(Order = 1)]
        public string Path { get; set; }

        [DataMember(Order = 2)]
        public string[] ValueNames { get; set; }

        [DataMember(Order = 3)]
        public List<RegistryKeyShape> SubKeys { get; set; }

        public RegistryKeyShape()
        {
            ValueNames = new string[0];
            SubKeys = new List<RegistryKeyShape>();
        }
    }
}
