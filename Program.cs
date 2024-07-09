using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
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
            }
            StringCollection files = new StringCollection();
            files.Add(logfile);
            Clipboard.SetFileDropList(files);
            MessageBox.Show($"Информация о ошибке была скопирована в буфер обмена\nОтправьте её в telegram @oixro","Exception");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c taskkill /f /in {AppDomain.CurrentDomain.FriendlyName}",
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                Application.Exit();
            }
            catch
            {
            }
        }
    }
}
