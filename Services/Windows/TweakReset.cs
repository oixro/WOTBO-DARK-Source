using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Services.Tweaks;

namespace WOTBO.Services.Windows
{
    /// <summary>
    /// Аварийный сброс: <c>WOTBO.exe --reset-all</c>.
    ///
    /// Откатывает все применённые твики (по бэкапам), возвращает штатный
    /// imageres.dll.mun, если «Синие папки» его подменили, и перезапускает
    /// проводник. Нужен, когда после «полного прохода» система ведёт себя
    /// странно и непонятно, какой именно твик виноват.
    /// </summary>
    public static class TweakReset
    {
        private const string Category = "Reset";
        private const string ImageresMun = @"C:\Windows\SystemResources\imageres.dll.mun";

        public static void Run()
        {
            try
            {
                Log.Initialize(new AppLogger(AppPaths.LogFilePath));
                Log.Info(Category, "=== аварийный сброс твиков ===");
            }
            catch
            {
            }

            PrepareWorkspace();

            var rolled = new List<string>();
            var failed = new List<string>();

            foreach (ITweak tweak in AppServices.TweakCatalog.All)
            {
                try
                {
                    if (!AppServices.Tweaks.IsApplied(tweak.Id))
                    {
                        continue;
                    }

                    using (TweakScope.Rollback(tweak.Id))
                    {
                        tweak.Rollback(AppServices.TweakContext);
                    }

                    rolled.Add(tweak.Id);
                    Log.Info(Category, "откат " + tweak.Id);
                }
                catch (Exception exception)
                {
                    failed.Add(tweak.Id);
                    Log.Error(Category, "не удалось откатить " + tweak.Id, exception);
                }
            }

            RestoreImageres();

            try
            {
                AppServices.Processes.StartCaptured("taskkill /f /im explorer.exe & timeout /t 2 & explorer.exe");
            }
            catch (Exception exception)
            {
                Log.Error(Category, "не удалось перезапустить explorer", exception);
            }

            var text = new StringBuilder();
            text.AppendLine("Откатано твиков: " + rolled.Count + ".");
            text.AppendLine();

            if (rolled.Count > 0)
            {
                text.AppendLine(string.Join(", ", rolled.ToArray()));
                text.AppendLine();
            }

            if (failed.Count > 0)
            {
                text.AppendLine("Не удалось откатить: " + string.Join(", ", failed.ToArray()));
                text.AppendLine();
            }

            text.AppendLine("Перезагрузите компьютер. Если проводник после этого не ожил —");
            text.AppendLine("запустите в командной строке от администратора: sfc /scannow");

            MessageBox.Show(
                text.ToString(),
                "WOTBO — аварийный сброс",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>Если «Синие папки» подменили imageres.dll.mun — вернуть оригинал.</summary>
        private static void RestoreImageres()
        {
            try
            {
                string backup = ImageresMun + "_bak";
                if (!File.Exists(backup))
                {
                    return;
                }

                AppServices.Processes.RunSuper(
                    "cmd.exe /c del /q \"" + ImageresMun + "\" & ren \"" + backup + "\" imageres.dll.mun",
                    AppServices.Workspace.SuExecutable);

                Log.Info(Category, "imageres.dll.mun восстановлен из _bak");
            }
            catch (Exception exception)
            {
                Log.Error(Category, "imageres.dll.mun не восстановлен", exception);
            }
        }

        private static void PrepareWorkspace()
        {
            try
            {
                var workspace = AppServices.Workspace;
                workspace.Prepare();
                AppServices.ResourceExtractor.Extract(workspace);
            }
            catch (Exception exception)
            {
                Log.Debug(Category, "рабочая папка не подготовлена: " + exception.Message);
            }
        }
    }
}
