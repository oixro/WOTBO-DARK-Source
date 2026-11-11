using System;

namespace WOTBO.App.Logging
{
    /// <summary>
    /// Точка доступа к логу до появления настоящего DI-контейнера.
    /// Настраивается один раз в <c>Program.Main</c>; если этого не сделать,
    /// лениво создаётся обычный файловый логгер.
    ///
    /// Это временный статический фасад: по мере перевода форм на сервисы
    /// <see cref="IAppLogger"/> будет передаваться через конструктор.
    /// </summary>
    public static class Log
    {
        private static readonly object Sync = new object();
        private static IAppLogger _instance;

        public static IAppLogger Instance
        {
            get
            {
                lock (Sync)
                {
                    if (_instance == null)
                    {
                        _instance = new AppLogger(AppPaths.LogFilePath);
                    }

                    return _instance;
                }
            }

            set
            {
                lock (Sync)
                {
                    _instance = value;
                }
            }
        }

        public static void Initialize(IAppLogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            Instance = logger;
        }

        public static void Write(string message)
        {
            Instance.Write(message);
        }

        public static void Trace(string category, string message)
        {
            Instance.Trace(category, message);
        }

        public static void Debug(string category, string message)
        {
            Instance.Debug(category, message);
        }

        public static void Info(string category, string message)
        {
            Instance.Info(category, message);
        }

        public static void Warn(string category, string message)
        {
            Instance.Warn(category, message);
        }

        public static void Error(string category, string message, Exception exception = null)
        {
            Instance.Error(category, message, exception);
        }
    }
}
