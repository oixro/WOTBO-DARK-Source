using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace WOTBO.App.Logging
{
    /// <summary>
    /// Пишет лог в один файл, построчно, с блокировкой.
    ///
    /// Файл открывается и закрывается на каждую запись: так работают
    /// <c>Get-Content -Wait</c> (F12), чтение лога после падения и копирование
    /// файла при закрытии. Писать синхронно выгоднее, чем через очередь:
    /// при крахе приложения последние строки не теряются.
    ///
    /// Формат — как в старом <c>writelog()</c>:
    /// <code>2026-11-11 20:24:51.4461 - текст</code>
    /// </summary>
    public sealed class AppLogger : IAppLogger
    {
        private readonly object _sync = new object();
        private readonly string _filePath;
        private LogLevel _minimumLevel;

        public AppLogger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Путь к файлу лога пуст.", nameof(filePath));
            }

            _filePath = filePath;
            _minimumLevel = LevelFromEnvironmentOrDefault();
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public LogLevel MinimumLevel
        {
            get { return _minimumLevel; }
            set { _minimumLevel = value; }
        }

        public void Write(string message)
        {
            Append(FormatPlain(message));
        }

        public void Write(LogLevel level, string category, string message)
        {
            if (level < _minimumLevel)
            {
                return;
            }

            Append(FormatLeveled(level, category, message, null));
        }

        public void Trace(string category, string message)
        {
            Write(LogLevel.Trace, category, message);
        }

        public void Debug(string category, string message)
        {
            Write(LogLevel.Debug, category, message);
        }

        public void Info(string category, string message)
        {
            Write(LogLevel.Info, category, message);
        }

        public void Warn(string category, string message)
        {
            Write(LogLevel.Warn, category, message);
        }

        public void Error(string category, string message, Exception exception = null)
        {
            if (LogLevel.Error < _minimumLevel)
            {
                return;
            }

            Append(FormatLeveled(LogLevel.Error, category, message, exception));
        }

        private static string FormatPlain(string message)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} - {1}", Timestamp(), message);
        }

        private static string FormatLeveled(LogLevel level, string category, string message, Exception exception)
        {
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0} - [{1}] [{2}] {3}",
                Timestamp(),
                LevelName(level),
                string.IsNullOrEmpty(category) ? "App" : category,
                message);

            if (exception != null)
            {
                line += Environment.NewLine + exception;
            }

            return line;
        }

        private static string Timestamp()
        {
            // Формат сохранён прежним, включая четыре знака после точки.
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff", CultureInfo.InvariantCulture);
        }

        private static string LevelName(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Trace: return "TRACE";
                case LogLevel.Debug: return "DEBUG";
                case LogLevel.Info: return "INFO ";
                case LogLevel.Warn: return "WARN ";
                case LogLevel.Error: return "ERROR";
                default: return "INFO ";
            }
        }

        /// <summary>
        /// Уровень можно поднять переменной окружения <c>WOTBO_LOG_LEVEL</c>
        /// (<c>Trace</c>, <c>Debug</c>, <c>Info</c>, <c>Warn</c>, <c>Error</c>).
        /// По умолчанию <c>Info</c>: подробное чтение реестра пишется только
        /// при явной диагностике.
        /// </summary>
        private static LogLevel LevelFromEnvironmentOrDefault()
        {
            string raw = Environment.GetEnvironmentVariable("WOTBO_LOG_LEVEL");
            LogLevel parsed;
            if (!string.IsNullOrWhiteSpace(raw) &&
                Enum.TryParse(raw.Trim(), true, out parsed))
            {
                return parsed;
            }

            return LogLevel.Info;
        }

        private void Append(string line)
        {
            lock (_sync)
            {
                try
                {
                    using (var writer = new StreamWriter(_filePath, true, Encoding.UTF8))
                    {
                        writer.WriteLine(line);
                    }
                }
                catch (IOException)
                {
                    // Лог не должен ронять приложение: файл может быть занят
                    // (открыт на чтение, удалён очисткой temp) — пропускаем запись.
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }
}
