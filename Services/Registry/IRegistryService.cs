using System.Collections.Generic;
using Microsoft.Win32;

namespace WOTBO.Services.Registry
{
    /// <summary>
    /// Работа с реестром через один явно выбранный <see cref="RegistryView"/>.
    ///
    /// Приложение x64, поэтому по умолчанию используется <c>Registry64</c>:
    /// иначе на 64-битной системе чтение <c>HKLM\Software\...</c> попадёт в
    /// WOW6432Node и твики применятся не туда.
    ///
    /// Все ошибки доступа выбрасываются как <see cref="RegistryOperationException"/>
    /// с понятным русским текстом: голый <c>UnauthorizedAccessException</c> в логе
    /// ничего не объясняет пользователю.
    /// </summary>
    public interface IRegistryService
    {
        /// <summary>Представление реестра. По умолчанию <c>Registry64</c>.</summary>
        RegistryView View { get; }

        bool KeyExists(RegistryHive hive, string keyPath);

        /// <summary>Читает значение вместе с типом. Отсутствующее значение — <c>Exists = false</c>.</summary>
        RegistryValueSnapshot ReadValue(RegistryHive hive, string keyPath, string name);

        /// <summary>Все значения ветки. Пустая ветка или отсутствие ключа — пустой словарь.</summary>
        IDictionary<string, object> ReadValues(RegistryHive hive, string keyPath);

        /// <summary>Значение или <paramref name="defaultValue"/>, если ключа/значения нет.</summary>
        object GetValue(RegistryHive hive, string keyPath, string name, object defaultValue = null);

        bool ValueExists(RegistryHive hive, string keyPath, string name);

        /// <summary>
        /// Пишет значение, создавая ключ при необходимости. В лог попадает
        /// старое и новое значение — это основа читаемой диагностики твиков.
        /// </summary>
        void WriteValue(RegistryHive hive, string keyPath, string name, object value, RegistryValueKind kind);

        /// <summary>Тип значения выводится из объекта: int → DWord, string[] → MultiString и т.д.</summary>
        void WriteValue(RegistryHive hive, string keyPath, string name, object value);

        /// <summary>
        /// Запись в уже открытый ключ. <c>null</c> — тихий пропуск, как
        /// <c>key?.SetValue(...)</c> в прежнем коде. В лог попадают old → new
        /// и полное имя ключа (<see cref="RegistryKey.Name"/>).
        /// </summary>
        void WriteValue(RegistryKey key, string name, object value, RegistryValueKind kind);

        void WriteValue(RegistryKey key, string name, object value);

        /// <summary>Удаление значения из открытого ключа; отсутствие — не ошибка.</summary>
        void DeleteValue(RegistryKey key, string name);

        void DeleteValue(RegistryHive hive, string keyPath, string name);

        /// <summary>Удаляет ветку целиком, если она есть. Отсутствие ключа — не ошибка.</summary>
        void DeleteKey(RegistryHive hive, string keyPath);

        /// <summary>Рекурсивный снимок ветки для последующего откката.</summary>
        RegistryKeySnapshot Capture(RegistryHive hive, string keyPath);

        /// <summary>
        /// Возвращает ветку к состоянию снимка: восстанавливает значения и
        /// подветки, удаляет появившиеся после снимка значения, а если ветки
        /// не было — удаляет её целиком.
        /// </summary>
        void Restore(RegistryKeySnapshot snapshot);
    }
}
