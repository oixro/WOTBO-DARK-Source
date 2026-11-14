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

            AppBootstrap.Run();
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
