using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO.App;
using WOTBO.App.Logging;

namespace test1
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Log.Initialize(new AppLogger(AppPaths.LogFilePath));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Exception);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            Application.Run(new Project.WOTBO());
        }

        static void Exception(object sender, ThreadExceptionEventArgs e)
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
                    MessageBoxIcon.Warning
                );

                return; // Не завершаем приложение
            }

            StringCollection files = new StringCollection();
            files.Add(Log.Instance.FilePath);
            Clipboard.SetFileDropList(files);
            MessageBox.Show($"Произошла непредвиденная ошибка в работе программы.\nФайл с подробностями ошибки был скопирован в буфер обмена.\nПожалуйста, отправьте его в Telegram  - @oixro для устранения проблемы.", "WOTBO ERROR",
                MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Вызывается для исключений вне UI-потока. Процесс всё равно
            // завершится, но причина останется в логе.
            Log.Error("App", "Необработанное исключение (AppDomain)", e.ExceptionObject as Exception);
        }

        static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error("App", "Исключение в задаче без обработчика", e.Exception);
            e.SetObserved();
        }
    }
}
