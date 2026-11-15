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

            string screenshotDirectory;
            float screenshotScale;
            if (TryGetScreenshotMode(out screenshotDirectory, out screenshotScale))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                MainForm.ScreenshotMode = true;

                using (var form = new MainForm())
                {
                    form.Show();
                    PumpMessages(4000);
                    form.ExportScreenshots(screenshotDirectory, screenshotScale);
                    form.Close();
                }

                return;
            }

            if (HasArgument("--fix-explorer"))
            {
                WOTBO.Services.Windows.ExplorerRecovery.Run();
                return;
            }

            if (HasArgument("--reset-all"))
            {
                WOTBO.Services.Windows.TweakReset.Run();
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
        /// <c>WOTBO.exe --export-screenshots &lt;dir&gt; [scale]</c> — сохранить
        /// по одному PNG на каждый раздел (для сайта).
        /// </summary>
        private static bool TryGetScreenshotMode(out string directory, out float scale)
        {
            directory = null;
            scale = 1f;
            string[] args = Environment.GetCommandLineArgs();

            for (int i = 0; i < args.Length; i++)
            {
                if (!string.Equals(args[i], "--export-screenshots", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                directory = i + 1 < args.Length ? args[i + 1] : "screenshots";
                if (i + 2 < args.Length)
                {
                    float.TryParse(
                        args[i + 2],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out scale);
                }

                return true;
            }

            return false;
        }

        private static void PumpMessages(int milliseconds)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < milliseconds)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(10);
            }
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

        private static bool HasArgument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
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
