using System;
using System.Windows.Forms;
using WOTBO.App;

namespace WOTBO
{
    internal static class Program
    {
        /// <summary>Главная точка входа для приложения.</summary>
        [STAThread]
        private static void Main()
        {
            if (IsServiceMode())
            {
                Services.Defender.DefenderServiceHost.RunAsService();
                return;
            }

            string resultMode;
            if (TryGetResultMode(out resultMode))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new WOTBO.UI.DefenderResultForm(resultMode));
                return;
            }

            AppBootstrap.Run();
        }

        /// <summary>
        /// <c>WOTBO.exe --defender-result apply|restore</c> — окно результата
        /// после автоматической перезагрузки, без запуска основного окна.
        /// </summary>
        private static bool TryGetResultMode(out string mode)
        {
            mode = null;
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], "--defender-result", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                mode = i + 1 < args.Length ? args[i + 1] : "apply";
                return true;
            }

            return false;
        }

        private static bool IsServiceMode()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(
                    args[i],
                    Services.Defender.DefenderServiceHost.ServiceArgument,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
