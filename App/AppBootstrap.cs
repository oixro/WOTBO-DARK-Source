using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO.App.Logging;

namespace WOTBO.App
{
    /// <summary>Запуск приложения и глобальные обработчики исключений.</summary>
    public static class AppBootstrap
    {
        public static void Run()
        {
            Log.Initialize(new AppLogger(AppPaths.LogFilePath));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            Application.Run(new Project.WOTBO());
        }

        private static void OnThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            Clipboard.Clear();
            Log.Error("App", "Необработанное исключение UI-потока", e.Exception);

            if (e.Exception is WebException webException &&
                webException.Message.Contains("Соединение было неожиданно закрыто"))
            {
                MessageBox.Show(
                    "Ошибка: Соединение было неожиданно закрыто. Попробуйте позже.",
                    "Ошибка загрузки",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            StringCollection files = new StringCollection();
            files.Add(Log.Instance.FilePath);
            Clipboard.SetFileDropList(files);
            MessageBox.Show(
                "Произошла непредвиденная ошибка в работе программы.\n" +
                "Файл с подробностями ошибки был скопирован в буфер обмена.\n" +
                "Пожалуйста, отправьте его в Telegram  - @oixro для устранения проблемы.",
                "WOTBO ERROR",
                MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error("App", "Необработанное исключение (AppDomain)", e.ExceptionObject as Exception);
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error("App", "Исключение в задаче без обработчика", e.Exception);
            e.SetObserved();
        }
    }
}
