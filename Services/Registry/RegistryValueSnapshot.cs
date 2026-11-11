using System;
using Microsoft.Win32;

namespace WOTBO.Services.Registry
{
    /// <summary>
    /// Снимок одного значения реестра.
    ///
    /// Отдельно хранится <see cref="Exists"/>: если значения не было до применения
    /// твика, откат должен его удалить, а не записать ноль или пустую строку.
    /// Для <c>REG_EXPAND_SZ</c> данные читаются без раскрытия переменных
    /// (<c>DoNotExpandEnvironmentNames</c>), чтобы вернуть ровно то, что было.
    /// </summary>
    public sealed class RegistryValueSnapshot
    {
        private RegistryValueSnapshot(
            RegistryHive hive,
            string keyPath,
            string name,
            bool exists,
            RegistryValueKind? kind,
            object data)
        {
            Hive = hive;
            KeyPath = keyPath ?? string.Empty;
            Name = name ?? string.Empty;
            Exists = exists;
            Kind = kind;
            Data = data;
        }

        public RegistryHive Hive { get; }

        public string KeyPath { get; }

        /// <summary>Имя значения; пустая строка — значение по умолчанию.</summary>
        public string Name { get; }

        public bool Exists { get; }

        /// <summary>Тип значения. <c>null</c>, если значения не существовало.</summary>
        public RegistryValueKind? Kind { get; }

        public object Data { get; }

        public static RegistryValueSnapshot Missing(RegistryHive hive, string keyPath, string name)
        {
            return new RegistryValueSnapshot(hive, keyPath, name, false, null, null);
        }

        public static RegistryValueSnapshot Present(
            RegistryHive hive,
            string keyPath,
            string name,
            RegistryValueKind kind,
            object data)
        {
            return new RegistryValueSnapshot(hive, keyPath, name, true, kind, data);
        }

        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return string.Format("{0}\\{1}", Hive, KeyPath) + " (default)";
                }

                return string.Format("{0}\\{1}\\{2}", Hive, KeyPath, Name);
            }
        }

        public override string ToString()
        {
            if (!Exists)
            {
                return FullPath + " = <отсутствует>";
            }

            return string.Format("{0} ({1}) = {2}", FullPath, Kind, Describe(Data));
        }

        internal static string Describe(object data)
        {
            if (data == null)
            {
                return "<null>";
            }

            if (data is string text)
            {
                return "\"" + text + "\"";
            }

            if (data is Array array)
            {
                var parts = new string[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    parts[i] = Convert.ToString(array.GetValue(i));
                }

                return "[" + string.Join(", ", parts) + "]";
            }

            return Convert.ToString(data);
        }
    }
}
