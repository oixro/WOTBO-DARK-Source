using Project;
using System;
using System.Diagnostics;
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
            Clipboard.SetText(e.Exception.ToString());
            MessageBox.Show($"{e.Exception.Message}\n\nПолный лог был скопирован в буфер\nОтправьте его в тг @oixro");
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
