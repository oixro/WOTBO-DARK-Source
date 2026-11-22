using System;
using System.IO;
using System.Reflection;
using WOTBO.App.Logging;

namespace WOTBO.Services.Storage
{
    /// <summary>
    /// Рабочая папка приложения (<c>c:\Windows\oixro</c>).
    ///
    /// При старте пытается очистить её. Если не получается (файл занят),
    /// переключается на папку со случайным суффиксом. Логи при очистке
    /// сохраняются: без них теряется диагностика.
    /// </summary>
    public sealed class TempWorkspace
    {
        private const string Category = "Temp";
        private const string MarkerName = ".prepared";

        private readonly string _baseDirectory;
        private readonly IAppLogger _log;

        public TempWorkspace(string baseDirectory, IAppLogger log)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentException("Рабочая папка пуста.", nameof(baseDirectory));
            }

            _baseDirectory = baseDirectory;
            _log = log ?? throw new ArgumentNullException(nameof(log));
            Root = baseDirectory;
        }

        /// <summary>Текущая рабочая папка. Меняется после неудачной очистки.</summary>
        public string Root { get; private set; }

        public string RegpackZip
        {
            get { return Path.Combine(Root, "regpack.zip"); }
        }

        public string Regpack
        {
            get { return Path.Combine(Root, "regpack"); }
        }

        public string Services
        {
            get { return Path.Combine(Root, "services"); }
        }

        public string UiZip
        {
            get { return Path.Combine(Root, "ui.zip"); }
        }

        public string Ui
        {
            get { return Path.Combine(Root, "ui"); }
        }

        public string Scheme
        {
            get { return Path.Combine(Root, "scheme.pow"); }
        }

        public string Smartctl
        {
            get { return Path.Combine(Root, "smartctl.exe"); }
        }

        public string SuExecutable
        {
            get { return Path.Combine(Root, "su.exe"); }
        }

        /// <summary>
        /// Очищает рабочую папку. Вызывается один раз при старте.
        /// </summary>
        /// <summary>
        /// Отпечаток встроенных ресурсов: версия exe, размер и время сборки.
        /// Пока он совпадает, распаковку можно не повторять.
        /// </summary>
        public static string ResourcesFingerprint()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                var file = new FileInfo(assembly.Location);
                return assembly.GetName().Version + "|" + file.Length + "|" + file.LastWriteTimeUtc.Ticks;
            }
            catch
            {
                return "unknown";
            }
        }

        public bool IsPrepared()
        {
            try
            {
                string marker = Path.Combine(Root, MarkerName);
                if (!File.Exists(marker)
                    || !string.Equals(
                        File.ReadAllText(marker).Trim(),
                        ResourcesFingerprint(),
                        StringComparison.Ordinal))
                {
                    return false;
                }

                return File.Exists(RegpackZip)
                    && File.Exists(SuExecutable)
                    && Directory.Exists(Regpack)
                    && Directory.Exists(Ui);
            }
            catch
            {
                return false;
            }
        }

        public void MarkPrepared()
        {
            try
            {
                File.WriteAllText(Path.Combine(Root, MarkerName), ResourcesFingerprint());
            }
            catch (Exception exception)
            {
                _log.Warn(Category, "не удалось сохранить маркер ресурсов: " + exception.Message);
            }
        }

        public void Prepare()
        {
            if (!Directory.Exists(Root))
            {
                _log.Info(Category, "tempfolder нету");
                return;
            }

            if (IsPrepared())
            {
                _log.Info(Category, "ресурсы актуальны, распаковка не нужна");
                return;
            }

            _log.Info(Category, $"tempfolder - \"{Root}\" exists");
            _log.Info(Category, $"пытаюсь всё удалить из {Root}");

            try
            {
                DeleteContents();
                _log.Info(Category, $"Удалена папка: {Root}");
            }
            catch (Exception ex)
            {
                _log.Warn(Category, $"не удалось очистить {Root}: {ex.Message}");

                Root = _baseDirectory + "_" + Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())).Remove(5);
                _log.Info(Category, $"я теперь тут ошиваюсь - {Root}");
                _log.Info(Category, $"new path_regpack7z - {RegpackZip}");
                _log.Info(Category, $"new path_regpack - {Regpack}");
                _log.Info(Category, $"new path_services - {Services}");
                _log.Info(Category, $"new path_uizip - {UiZip}");
                _log.Info(Category, $"new path_ui - {Ui}");
                _log.Info(Category, $"new path_scheme - {Scheme}");
                _log.Info(Category, $"new smartctl - {Smartctl}");
            }
        }

        /// <summary>
        /// Удаляет рабочую папку при закрытии, не трогая логи.
        /// Возвращает <c>false</c>, если очистить не удалось.
        /// </summary>
        public bool Cleanup()
        {
            if (!Directory.Exists(Root))
            {
                return true;
            }

            try
            {
                _log.Info(Category, $"удаляем tempfolder -  \"{Root}\"");
                DeleteContents();
                if (Directory.Exists(Root) && Directory.GetFileSystemEntries(Root).Length == 0)
                {
                    Directory.Delete(Root, false);
                }

                return true;
            }
            catch (Exception ex)
            {
                _log.Warn(Category, $"не удалось удалить {Root}: {ex.Message}");
                return false;
            }
        }

        private void DeleteContents()
        {
            string[] logs = Directory.GetFiles(Root, "wotbo*.log", SearchOption.TopDirectoryOnly);
            if (logs.Length == 0)
            {
                Directory.Delete(Root, true);
                return;
            }

            foreach (string file in Directory.GetFiles(Root, "*", SearchOption.TopDirectoryOnly))
            {
                if (!IsLogFile(file))
                {
                    File.Delete(file);
                }
            }

            foreach (string directory in Directory.GetDirectories(Root, "*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(directory, true);
            }

            _log.Info(Category, $"логи сохранены в {Root}, остальное удалено");
        }

        private static bool IsLogFile(string path)
        {
            string name = Path.GetFileName(path);
            return name.StartsWith("wotbo", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".log", StringComparison.OrdinalIgnoreCase);
        }
    }
}
