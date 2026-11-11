using System.Collections.Generic;

namespace WOTBO.Services.Registry
{
    /// <summary>
    /// Снимок ветки реестра для отката.
    ///
    /// <see cref="Existed"/> == <c>false</c> означает, что ключа до твика не было:
    /// откат должен удалить всю ветку, а не пересоздавать её пустой.
    /// </summary>
    public sealed class RegistryKeySnapshot
    {
        public RegistryKeySnapshot(
            Microsoft.Win32.RegistryHive hive,
            string path,
            bool existed,
            IReadOnlyList<RegistryValueSnapshot> values,
            IReadOnlyList<RegistryKeySnapshot> subKeys)
        {
            Hive = hive;
            Path = path ?? string.Empty;
            Existed = existed;
            Values = values ?? new List<RegistryValueSnapshot>();
            SubKeys = subKeys ?? new List<RegistryKeySnapshot>();
        }

        public Microsoft.Win32.RegistryHive Hive { get; }

        public string Path { get; }

        public bool Existed { get; }

        public IReadOnlyList<RegistryValueSnapshot> Values { get; }

        public IReadOnlyList<RegistryKeySnapshot> SubKeys { get; }

        public string FullPath
        {
            get { return string.Format("{0}\\{1}", Hive, Path); }
        }

        public static RegistryKeySnapshot Missing(Microsoft.Win32.RegistryHive hive, string path)
        {
            return new RegistryKeySnapshot(hive, path, false, null, null);
        }
    }
}
