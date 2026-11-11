using System;
using System.Diagnostics;
using System.Globalization;
using WOTBO.App;
using WOTBO.Services.State;

namespace WOTBO.App.Logging
{
    /// <summary>
    /// Область одного твика: пишет в лог начало, конец и длительность,
    /// накапливает постоянный бэкап и сохраняет состояние.
    ///
    /// <code>
    /// using (TweakScope.Apply("main.mpo"))
    /// {
    ///     AppServices.Registry.WriteValue(...);
    /// }
    /// </code>
    ///
    /// При применении все изменённые значения реестра автоматически попадают
    /// в бэкап; файлы добавляются вызовом <see cref="BackupFile"/>. При откате
    /// после штатного обработчика <see cref="TweakStateStore.Restore"/>
    /// возвращает ровно то, что было до твика.
    /// </summary>
    public sealed class TweakScope : IDisposable
    {
        private readonly Stopwatch _watch;
        private readonly string _category;
        private readonly string _action;
        private readonly string _tweakId;
        private readonly bool _apply;
        private TweakBackupSession _session;
        private bool _disposed;

        private TweakScope(string id, string action, bool apply)
        {
            _tweakId = id;
            _action = action;
            _apply = apply;
            _category = "Tweak:" + id;
            _watch = Stopwatch.StartNew();
            Log.Info(_category, _action + " начато");

            if (apply && TweakStateStore.SupportsBackup(id))
            {
                _session = TweakBackupSession.Begin(id, AppServices.Tweaks.BackupDirectory(id));
            }
        }

        public static TweakScope Apply(string id)
        {
            return new TweakScope(id, "применение", true);
        }

        public static TweakScope Rollback(string id)
        {
            return new TweakScope(id, "откат", false);
        }

        /// <summary>
        /// Сохранить файл перед изменением/удалением в текущем apply-блоке.
        /// Если твик создаёт новый файл, вызов до создания запомнит
        /// «файла не было», и откат удалит его.
        /// </summary>
        public static void BackupFile(string path)
        {
            TweakBackupSession.RecordFile(path);
        }

        /// <summary>Рекурсивно сохранить каталог перед изменением/удалением.</summary>
        public static void BackupDirectory(string path)
        {
            TweakBackupSession.RecordDirectory(path);
        }

        /// <summary>
        /// Сохранить все ветки, которые меняет .reg-файл. Вызывать до
        /// <c>regedit /s</c>.
        /// </summary>
        public static void BackupRegFile(string regFilePath)
        {
            TweakBackupSession.RecordRegFile(regFilePath);
        }

        /// <summary>Полный экспорт ветки реестра перед изменением.</summary>
        public static void BackupRegistryKey(Microsoft.Win32.RegistryHive hive, string keyPath)
        {
            TweakBackupSession session = TweakBackupSession.Current;
            if (session != null)
            {
                session.BackupRegistryKey(hive, keyPath);
            }
        }

        public string Category
        {
            get { return _category; }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                if (_apply)
                {
                    TweakBackup backup = _session != null ? _session.ToBackup() : null;
                    TweakBackupSession.End();
                    AppServices.Tweaks.MarkApplied(_tweakId, backup);
                }
                else
                {
                    AppServices.Tweaks.Restore(_tweakId);
                }
            }
            catch (Exception ex)
            {
                Log.Error(_category, "ошибка сохранения состояния твика", ex);
            }

            Log.Info(_category, string.Format(
                CultureInfo.InvariantCulture,
                "{0} завершено за {1} мс",
                _action,
                _watch.ElapsedMilliseconds));
        }
    }
}
