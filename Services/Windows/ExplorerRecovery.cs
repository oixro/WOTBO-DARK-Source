using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Windows
{
    /// <summary>
    /// Аварийное восстановление проводника: <c>WOTBO.exe --fix-explorer</c>.
    ///
    /// Нужно, когда после «полного прохода» Explorer не запускается (мигает
    /// рабочий стол, нет панели задач) — все твики, которые патчат проводник,
    /// откатываются по бэкапам, Explorer перезапускается.
    /// </summary>
    public static class ExplorerRecovery
    {
        private const string Category = "Recovery";
        private const string InProcKey = @"CLSID\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}\InProcServer32";
        private const string FileExplorerDll = @"C:\Windows\System32\Windows.UI.FileExplorer.dll";

        /// <summary>Твики, которые вмешиваются в проводник и могут его сломать.</summary>
        private static readonly string[] Tweaks =
        {
            "ui.win11header",
            "ui.mica",
            "ui.bluefolders",
            "ui.explorerdetails",
            "ui.cleancontext",
            "ui.contextmenu",
        };

        public static void Run()
        {
            try
            {
                Log.Initialize(new AppLogger(AppPaths.LogFilePath));
                Log.Info(Category, "=== аварийное восстановление проводника ===");
            }
            catch
            {
            }

            PrepareWorkspace();

            var rolled = new List<string>();
            var failed = new List<string>();

            foreach (string id in Tweaks)
            {
                try
                {
                    using (TweakScope.Rollback(id))
                    {
                        AppServices.TweakCatalog.Get(id).Rollback(AppServices.TweakContext);
                    }

                    rolled.Add(id);
                    Log.Info(Category, "откат " + id);
                }
                catch (Exception exception)
                {
                    failed.Add(id);
                    Log.Error(Category, "не удалось откатить " + id, exception);
                }
            }

            RestoreExplorerIsland();

            try
            {
                AppServices.Processes.StartCaptured("taskkill /f /im explorer.exe & timeout /t 2 & explorer.exe");
            }
            catch (Exception exception)
            {
                Log.Error(Category, "не удалось перезапустить explorer", exception);
            }

            var text = new StringBuilder();
            text.AppendLine("Проводник: патчи сняты, explorer.exe перезапущен.");
            text.AppendLine();

            if (rolled.Count > 0)
            {
                text.AppendLine("Откатано: " + string.Join(", ", rolled.ToArray()));
            }

            if (failed.Count > 0)
            {
                text.AppendLine("Не удалось откатить: " + string.Join(", ", failed.ToArray()));
            }

            text.AppendLine();
            text.AppendLine("Если панель задач так и не появилась — откройте WOTBO и откатите");
            text.AppendLine("«Базовые настройки Windows» и «Классическая шапка проводника» вручную,");
            text.AppendLine("затем перезагрузитесь.");

            MessageBox.Show(
                text.ToString(),
                "WOTBO — восстановление проводника",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// Если после отката InProcServer32 всё ещё указывает на FixByVlado —
        /// возвращаем штатную библиотеку XAML-острова.
        /// </summary>
        private static void RestoreExplorerIsland()
        {
            try
            {
                object current = AppServices.Registry.GetValue(
                    RegistryHive.ClassesRoot, InProcKey, string.Empty);

                if (Convert.ToString(current).IndexOf("FixByVlado", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return;
                }

                // Ветка CLSID принадлежит TrustedInstaller: обычной записи
                // от админа не хватает, поэтому идём через su.exe (SYSTEM),
                // как и сам твик.
                bool fixedBySuper = false;
                try
                {
                    AppServices.Processes.RunSuper(
                        "reg add \"HKCR\\" + InProcKey + "\" /v \"\" /t REG_SZ /d \"" + FileExplorerDll + "\" /f",
                        AppServices.Workspace.SuExecutable);

                    object check = AppServices.Registry.GetValue(
                        RegistryHive.ClassesRoot, InProcKey, string.Empty);
                    fixedBySuper = Convert.ToString(check).IndexOf("FixByVlado", StringComparison.OrdinalIgnoreCase) < 0;
                }
                catch (Exception exception)
                {
                    Log.Error(Category, "su.exe не смог вернуть InProcServer32", exception);
                }

                if (fixedBySuper)
                {
                    Log.Info(Category, "InProcServer32 возвращён на " + FileExplorerDll);
                    return;
                }

                try
                {
                    AppServices.Registry.WriteValue(
                        RegistryHive.ClassesRoot, InProcKey, string.Empty, FileExplorerDll, RegistryValueKind.String);
                }
                catch (Exception exception)
                {
                    Log.Error(Category, "InProcServer32 не восстановлен", exception);
                }
            }
            catch (Exception exception)
            {
                Log.Error(Category, "не удалось вернуть InProcServer32", exception);
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
