using System;
using Microsoft.Win32;

namespace WOTBO.Services.Registry
{
    /// <summary>
    /// Ошибка операции с реестром с человекочитаемым описанием.
    ///
    /// Сохраняет исходное исключение (<see cref="Exception.InnerException"/>),
    /// поэтому в логе виден и понятный текст, и настоящая причина.
    /// </summary>
    public sealed class RegistryOperationException : Exception
    {
        public RegistryOperationException(
            string operation,
            RegistryHive hive,
            string keyPath,
            string valueName,
            string message,
            Exception innerException)
            : base(BuildMessage(operation, hive, keyPath, valueName, message, innerException), innerException)
        {
            Operation = operation;
            Hive = hive;
            KeyPath = keyPath ?? string.Empty;
            ValueName = valueName ?? string.Empty;
        }

        public string Operation { get; }

        public RegistryHive Hive { get; }

        public string KeyPath { get; }

        public string ValueName { get; }

        public string FullPath
        {
            get
            {
                if (string.IsNullOrEmpty(ValueName))
                {
                    return string.Format("{0}\\{1}", Hive, KeyPath);
                }

                return string.Format("{0}\\{1}\\{2}", Hive, KeyPath, ValueName);
            }
        }

        private static string BuildMessage(
            string operation,
            RegistryHive hive,
            string keyPath,
            string valueName,
            string message,
            Exception innerException)
        {
            string target = string.IsNullOrEmpty(valueName)
                ? string.Format(@"{0}\{1}", hive, keyPath)
                : string.Format(@"{0}\{1}\{2}", hive, keyPath, valueName);

            string text = string.Format(
                "Не удалось выполнить «{0}» для {1}: {2}",
                operation,
                target,
                message);

            if (innerException is UnauthorizedAccessException || innerException is System.Security.SecurityException)
            {
                text += ". Нужны права администратора и, возможно, владелец ключа (TrustedInstaller). " +
                        "Запустите WOTBO от имени администратора.";
            }

            return text;
        }
    }
}
