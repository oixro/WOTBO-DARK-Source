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

        public AppLogger(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Путь к файлу лога пуст.", nameof(filePath));
            }

            _filePath = filePath;
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        public void Write(string message)
        {
            Append(FormatPlain(message));
        }

        public void Write(LogLevel level, string category, string message)
        {
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
