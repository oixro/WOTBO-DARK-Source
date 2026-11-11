using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using Microsoft.Win32;
using WOTBO.App.Logging;

namespace WOTBO.Services.Registry
{
    /// <summary>
    /// Реализация <see cref="IRegistryService"/> поверх <see cref="Microsoft.Win32.Registry"/>.
    ///
    /// Чтение пишется в лог на уровне <c>DEBUG</c>, запись и удаление — на <c>INFO</c>
    /// со старым и новым значением. Так журнал отвечает на вопрос «что именно
    /// изменил твик», а не только «твик применён».
    ///
    /// Ошибки не глотаются: они превращаются в <see cref="RegistryOperationException"/>
    /// (понятный текст + исходное исключение) и пишутся в лог как <c>ERROR</c>.
    /// </summary>
    public sealed class RegistryService : IRegistryService
    {
        private const string Category = "Registry";

        private readonly IAppLogger _log;

        public RegistryService(IAppLogger log)
            : this(log, RegistryView.Registry64)
        {
        }

        public RegistryService(IAppLogger log, RegistryView view)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            View = view;
        }

        public RegistryView View { get; }

        public bool KeyExists(RegistryHive hive, string keyPath)
        {
            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View))
                using (RegistryKey key = baseKey.OpenSubKey(Normalize(keyPath), false))
                {
                    bool exists = key != null;
                    _log.Debug(Category, Format("проверка ключа", hive, keyPath, null) + " -> " + (exists ? "есть" : "нет"));
                    return exists;
                }
            }
            catch (Exception ex) when (IsRegistryFailure(ex))
            {
                throw Fail("проверка ключа", hive, keyPath, null, ex);
            }
        }

        public RegistryValueSnapshot ReadValue(RegistryHive hive, string keyPath, string name)
        {
            string valueName = NormalizeName(name);

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View))
                using (RegistryKey key = baseKey.OpenSubKey(Normalize(keyPath), false))
                {
                    if (key == null)
                    {
                        _log.Debug(Category, Format("чтение", hive, keyPath, valueName) + " -> ключа нет");
                        return RegistryValueSnapshot.Missing(hive, keyPath, valueName);
                    }

                    object data = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (data == null)
                    {
                        _log.Debug(Category, Format("чтение", hive, keyPath, valueName) + " -> значения нет");
                        return RegistryValueSnapshot.Missing(hive, keyPath, valueName);
                    }

                    RegistryValueKind kind = key.GetValueKind(valueName);
                    RegistryValueSnapshot snapshot = RegistryValueSnapshot.Present(hive, keyPath, valueName, kind, data);
                    _log.Debug(Category, "чтение " + snapshot);
                    return snapshot;
                }
            }
            catch (Exception ex) when (IsRegistryFailure(ex))
            {
                throw Fail("чтение", hive, keyPath, valueName, ex);
            }
        }

        public IDictionary<string, object> ReadValues(RegistryHive hive, string keyPath)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View))
                using (RegistryKey key = baseKey.OpenSubKey(Normalize(keyPath), false))
                {
                    if (key == null)
                    {
                        _log.Debug(Category, Format("чтение значений", hive, keyPath, null) + " -> ключа нет");
                        return result;
                    }

                    foreach (string name in key.GetValueNames())
                    {
                        result[name] = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    }

                    _log.Debug(Category, string.Format(
                        CultureInfo.InvariantCulture,
                        "чтение значений {0}\\{1} -> {2} шт.",
                        hive,
                        keyPath,
                        result.Count));

                    return result;
                }
            }
            catch (Exception ex) when (IsRegistryFailure(ex))
            {
                throw Fail("чтение значений", hive, keyPath, null, ex);
            }
        }

        public object GetValue(RegistryHive hive, string keyPath, string name, object defaultValue = null)
        {
            RegistryValueSnapshot snapshot = ReadValue(hive, keyPath, name);
            return snapshot.Exists ? snapshot.Data : defaultValue;
        }

        public bool ValueExists(RegistryHive hive, string keyPath, string name)
        {
            return ReadValue(hive, keyPath, name).Exists;
        }

        public void WriteValue(RegistryHive hive, string keyPath, string name, object value, RegistryValueKind kind)
        {
            string valueName = NormalizeName(name);
            string path = Normalize(keyPath);

            // Старое значение нужно и для лога, и для отката: читаем до записи.
            RegistryValueSnapshot before = ReadValue(hive, path, valueName);

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View))
                using (RegistryKey key = baseKey.CreateSubKey(path))
                {
                    key.SetValue(valueName, value ?? string.Empty, kind);
                }

                RegistryValueSnapshot after = RegistryValueSnapshot.Present(hive, path, valueName, kind, value);
                _log.Info(Category, "запись " + after + " (было: " + DescribeBefore(before) + ")");
            }
            catch (Exception ex) when (IsRegistryFailure(ex))
            {
                throw Fail("запись", hive, path, valueName, ex);
            }
        }

        public void DeleteValue(RegistryHive hive, string keyPath, string name)
        {
            string valueName = NormalizeName(name);
            string path = Normalize(keyPath);

            RegistryValueSnapshot before = ReadValue(hive, path, valueName);
            if (!before.Exists)
            {
                _log.Debug(Category, Format("удаление значения", hive, path, valueName) + " -> значения не было");
                return;
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View))
                using (RegistryKey key = baseKey.OpenSubKey(path, true))
                {
                    if (key == null)
                    {
                        _log.Debug(Category, Format("удаление значения", hive, path, valueName) + " -> ключа нет");
                        return;
                    }

                    key.DeleteValue(valueName, false);
                }

                _log.Info(Category, Format("удаление значения", hive, path, valueName) + " (было: " + DescribeBefore(before) + ")");
            }
            catch (Exception ex) when (IsRegistryFailure(ex))
            {
                throw Fail("удаление значения", hive, path, valueName, ex);
            }
        }

        public void DeleteKey(RegistryHive hive, string keyPath)
        {
            string path = Normalize(keyPath);

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Нельзя удалить корень реестра.", nameof(keyPath));
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View))
                {
                    if (baseKey.OpenSubKey(path, false) == null)
                    {
                        _log.Debug(Category, Format("удаление ключа", hive, path, null) + " -> ключа нет");
                        return;
                    }

                    baseKey.DeleteSubKeyTree(path, false);
                }

                _log.Info(Category, "удаление ключа " + Format(null, hive, path, null));
            }
            catch (Exception ex) when (IsRegistryFailure(ex))
            {
                throw Fail("удаление ключа", hive, path, null, ex);
            }
        }

        public RegistryKeySnapshot Capture(RegistryHive hive, string keyPath)
        {
            string path = Normalize(keyPath);

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, View))
                using (RegistryKey key = baseKey.OpenSubKey(path, false))
                {
                    if (key == null)
                    {
                        _log.Debug(Category, "снимок " + Format(null, hive, path, null) + " -> ключа нет");
                        return RegistryKeySnapshot.Missing(hive, path);
                    }

                    RegistryKeySnapshot snapshot = CaptureKey(hive, path, key);
                    _log.Debug(Category, string.Format(
                        CultureInfo.InvariantCulture,
                        "снимок {0}\\{1} -> значений {2}, подветок {3}",
                        hive,
                        path,
                        snapshot.Values.Count,
                        snapshot.SubKeys.Count));

                    return snapshot;
                }
            }
            catch (Exception ex) when (IsRegistryFailure(ex))
            {
                throw Fail("снимок", hive, path, null, ex);
            }
        }

        public void Restore(RegistryKeySnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            try
            {
                using (RegistryKey baseKey = RegistryKey.OpenBaseKey(snapshot.Hive, View))
                {
                    if (!snapshot.Existed)
                    {
                        if (baseKey.OpenSubKey(snapshot.Path, false) != null)
                        {
                            baseKey.DeleteSubKeyTree(snapshot.Path, false);
                        }

                        _log.Info(Category, "откат: ветка " + snapshot.FullPath + " удалена (до твика её не было)");
                        return;
                    }

                    RestoreKey(baseKey, snapshot);
                    _log.Info(Category, "откат: ветка " + snapshot.FullPath + " восстановлена");
                }
            }
            catch (Exception ex) when (IsRegistryFailure(ex))
            {
                throw Fail("откат", snapshot.Hive, snapshot.Path, null, ex);
            }
        }

        private RegistryKeySnapshot CaptureKey(RegistryHive hive, string path, RegistryKey key)
        {
            var values = new List<RegistryValueSnapshot>();
            foreach (string name in key.GetValueNames())
            {
                object data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                RegistryValueKind kind = key.GetValueKind(name);
                values.Add(RegistryValueSnapshot.Present(hive, path, name, kind, data));
            }

            var subKeys = new List<RegistryKeySnapshot>();
            foreach (string subName in key.GetSubKeyNames())
            {
                using (RegistryKey subKey = key.OpenSubKey(subName, false))
                {
                    if (subKey != null)
                    {
                        subKeys.Add(CaptureKey(hive, path + "\\" + subName, subKey));
                    }
                }
            }

            return new RegistryKeySnapshot(hive, path, true, values, subKeys);
        }

        private void RestoreKey(RegistryKey baseKey, RegistryKeySnapshot snapshot)
        {
            using (RegistryKey key = baseKey.CreateSubKey(snapshot.Path))
            {
                var expectedValues = new HashSet<string>(
                    snapshot.Values.Select(v => v.Name),
                    StringComparer.OrdinalIgnoreCase);

                // Значения, появившиеся после снимка, откат убирает: иначе ветка
                // не вернётся к исходному состоянию.
                foreach (string existing in key.GetValueNames())
                {
                    if (!expectedValues.Contains(existing))
                    {
                        key.DeleteValue(existing, false);
                        _log.Warn(Category, "откат: удалено лишнее значение " + Format(null, snapshot.Hive, snapshot.Path, existing));
                    }
                }

                foreach (RegistryValueSnapshot value in snapshot.Values)
                {
                    if (value.Exists)
                    {
                        key.SetValue(value.Name, value.Data ?? string.Empty, value.Kind.Value);
                    }
                    else
                    {
                        key.DeleteValue(value.Name, false);
                    }
                }
            }

            var expectedSubKeys = new HashSet<string>(
                snapshot.SubKeys.Select(s => LeafName(s.Path)),
                StringComparer.OrdinalIgnoreCase);

            using (RegistryKey key = baseKey.OpenSubKey(snapshot.Path, true))
            {
                if (key != null)
                {
                    foreach (string existing in key.GetSubKeyNames())
                    {
                        if (!expectedSubKeys.Contains(existing))
                        {
                            key.DeleteSubKeyTree(existing, false);
                            _log.Warn(Category, "откат: удалена лишняя подветка " + Format(null, snapshot.Hive, snapshot.Path + "\\" + existing, null));
                        }
                    }
                }
            }

            foreach (RegistryKeySnapshot sub in snapshot.SubKeys)
            {
                RestoreKey(baseKey, sub);
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

        private static string Normalize(string keyPath)
        {
            return (keyPath ?? string.Empty).Trim('\\');
        }

        private static string NormalizeName(string name)
        {
            return name ?? string.Empty;
        }

        private static string DescribeBefore(RegistryValueSnapshot before)
        {
            return before.Exists ? RegistryValueSnapshot.Describe(before.Data) : "<отсутствовало>";
        }

        private static string Format(string operation, RegistryHive hive, string keyPath, string valueName)
        {
            string target = string.IsNullOrEmpty(valueName)
                ? string.Format("{0}\\{1}", hive, keyPath)
                : string.Format("{0}\\{1}\\{2}", hive, keyPath, valueName);

            return string.IsNullOrEmpty(operation) ? target : operation + " " + target;
        }

        private static bool IsRegistryFailure(Exception ex)
        {
            return ex is UnauthorizedAccessException
                || ex is SecurityException
                || ex is IOException
                || ex is ArgumentException
                || ex is ObjectDisposedException
                || ex is InvalidOperationException;
        }

        private RegistryOperationException Fail(
            string operation,
            RegistryHive hive,
            string keyPath,
            string valueName,
            Exception ex)
        {
            string reason;
            if (ex is UnauthorizedAccessException || ex is SecurityException)
            {
                reason = "доступ запрещён";
            }
            else if (ex is IOException)
            {
                reason = "ошибка ввода-вывода";
            }
            else
            {
                reason = ex.Message;
            }

            var wrapped = new RegistryOperationException(operation, hive, keyPath, valueName, reason, ex);
            _log.Error(Category, wrapped.Message, ex);
            return wrapped;
        }
    }
}
