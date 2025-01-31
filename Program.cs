using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Exception);
            Application.Run(new Project.WOTBO());
        }

        static void Exception(object sender, ThreadExceptionEventArgs e)
        {
            Clipboard.Clear();
            string logfile = $@"{Environment.ExpandEnvironmentVariables("%temp%")}\wotbo.log";
            using (StreamWriter logs = new StreamWriter(logfile, true))
            {
                logs.WriteLine("Exception: " + e.Exception.Message);
                logs.WriteLine("StackTrace: " + e.Exception.StackTrace);
                logs.WriteLine("Date: " + DateTime.Now);
            }
            if (e.Exception is WebException webException &&
    webException.Message.Contains("Соединение было неожиданно закрыто"))
            {
                // Сообщение для пользователя
                MessageBox.Show(
                    "Ошибка: Соединение было неожиданно закрыто. Попробуйте позже.",
                    "Ошибка загрузки",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return; // Не завершаем приложение
            }
            StringCollection files = new StringCollection();
            files.Add(logfile);
            Clipboard.SetFileDropList(files);
            MessageBox.Show($"Произошла непредвиденная ошибка в работе программы.\nФайл с подробностями ошибки был скопирован в буфер обмена.\nПожалуйста, отправьте его в Telegram  - @oixro для устранения проблемы.", "WOTBO ERROR",
                MessageBoxButtons.OK,MessageBoxIcon.Exclamation);
            //try
            //{
            //    Process.Start(new ProcessStartInfo
            //    {
            //        FileName = "cmd.exe",
            //        Arguments = $"/c taskkill /f /in {AppDomain.CurrentDomain.FriendlyName}",
            //        WindowStyle = ProcessWindowStyle.Hidden
            //    });
            //    Application.Exit();
            //}
            //catch
            //{
            //}
        }
    }
}
