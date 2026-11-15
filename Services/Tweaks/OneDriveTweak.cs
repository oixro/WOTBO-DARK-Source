using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Удаление OneDrive. Актуально для Windows 11 (включая 24H2–26H2),
    /// где OneDrive ставится в профиль пользователя.
    ///
    /// Порядок: остановить процесс, вызвать штатный <c>OneDriveSetup.exe
    /// /uninstall</c> (и пользовательский, и системный), затем вычистить
    /// остатки — папки программы, автозапуск, закрепление в проводнике,
    /// задачи обновления и Store-версию. Папка с файлами пользователя
    /// (<c>%USERPROFILE%\OneDrive</c>) не трогается.
    /// </summary>
    public sealed class OneDriveTweak : TweakBase
    {
        private const string LogName = "Tweak:extra.onedrive";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private const string NavPaneClsid =
            @"Software\Classes\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}";

        private const string NavPaneMachineClsid =
            @"SOFTWARE\Classes\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}";

        public override string Id
        {
            get { return "extra.onedrive"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            if (IsRunning())
            {
                return TweakState.NotApplied;
            }

            foreach (string directory in ProgramDirectories())
            {
                if (File.Exists(Path.Combine(directory, "OneDrive.exe")))
                {
                    return TweakState.NotApplied;
                }
            }

            if (SystemSetups().Any(File.Exists))
            {
                return TweakState.NotApplied;
            }

            if (context.Registry.ValueExists(RegistryHive.CurrentUser, RunKey, "OneDrive"))
            {
                return TweakState.NotApplied;
            }

            return TweakState.Applied;
        }

        public override void Apply(TweakContext context)
        {
            Stop();

            List<string> setups = SystemSetups().Where(File.Exists)
                .Concat(UserSetups())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (setups.Count == 0)
            {
                context.Log.Info(LogName, "OneDriveSetup.exe не найден — чистим остатки напрямую");
            }

            foreach (string setup in setups)
            {
                context.Log.Info(LogName, "удаление через " + setup);

                try
                {
                    Task<string> run = context.Processes.ReadOutputAsync(setup, "/uninstall");
                    if (!run.Wait(TimeSpan.FromSeconds(90)))
                    {
                        context.Log.Warn(LogName, "OneDriveSetup не завершился за 90 с: " + setup);
                    }
                }
                catch (Exception exception)
                {
                    context.Log.Warn(LogName, "ошибка запуска " + setup + ": " + exception.Message);
                }
            }

            RemoveStoreVersion(context);
            RemoveDirectories(context);
            RemoveSetups(context);
            RemoveRegistry(context);
            RemoveTasks(context);

            Stop();

            if (IsRunning() || ProgramDirectories().Any(dir => File.Exists(Path.Combine(dir, "OneDrive.exe"))))
            {
                context.Log.Warn(LogName, "OneDrive всё ещё на месте — возможно, нужна перезагрузка");
            }
        }

        public override void Rollback(TweakContext context)
        {
            // OneDrive удаляется без автоматического восстановления:
            // вернуть его можно установщиком с сайта Microsoft.
        }

        private static bool IsRunning()
        {
            try
            {
                return Process.GetProcessesByName("OneDrive").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static void Stop()
        {
            try
            {
                foreach (Process process in Process.GetProcessesByName("OneDrive"))
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
            }
            catch
            {
            }
        }

        private static string LocalAppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData); }
        }

        private static string ProgramFiles
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles); }
        }

        private static string ProgramFilesX86
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86); }
        }

        private static string Windows
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.Windows); }
        }

        /// <summary>Куда OneDrive ставит свои файлы (без папки с данными пользователя).</summary>
        private static IEnumerable<string> ProgramDirectories()
        {
            yield return Path.Combine(LocalAppData, @"Microsoft\OneDrive");
            yield return Path.Combine(ProgramFiles, "Microsoft OneDrive");
            yield return Path.Combine(ProgramFilesX86, "Microsoft OneDrive");
        }

        private static IEnumerable<string> SystemSetups()
        {
            yield return Path.Combine(Windows, @"System32\OneDriveSetup.exe");
            yield return Path.Combine(Windows, @"SysWOW64\OneDriveSetup.exe");
        }

        /// <summary>Пользовательский установщик: он лежит внутри папки версии.</summary>
        private static List<string> UserSetups()
        {
            var result = new List<string>();

            try
            {
                string root = Path.Combine(LocalAppData, @"Microsoft\OneDrive");
                if (Directory.Exists(root))
                {
                    result.AddRange(Directory.GetFiles(
                        root, "OneDriveSetup.exe", SearchOption.AllDirectories));
                }
            }
            catch
            {
            }

            return result;
        }

        private static void RemoveStoreVersion(TweakContext context)
        {
            try
            {
                Task<string> run = context.Processes.ReadOutputAsync(
                    "powershell.exe",
                    "-NoProfile -NonInteractive -Command \"Get-AppxPackage -AllUsers *OneDrive* | " +
                    "Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\"");

                run.Wait(TimeSpan.FromSeconds(60));
            }
            catch (Exception exception)
            {
                context.Log.Debug(LogName, "Store-версия: " + exception.Message);
            }
        }

        private static void RemoveDirectories(TweakContext context)
        {
            foreach (string directory in ProgramDirectories().Concat(new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Microsoft OneDrive"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\OneDrive"),
            }))
            {
                try
                {
                    if (!Directory.Exists(directory))
                    {
                        continue;
                    }

                    Directory.Delete(directory, true);
                    context.Log.Info(LogName, "удалена папка " + directory);
                }
                catch (Exception exception)
                {
                    context.Log.Debug(LogName, "не удалось удалить " + directory + ": " + exception.Message);
                }
            }
        }

        private static void RemoveSetups(TweakContext context)
        {
            foreach (string setup in SystemSetups())
            {
                try
                {
                    if (!File.Exists(setup))
                    {
                        continue;
                    }

                    File.Delete(setup);
                    context.Log.Info(LogName, "удалён " + setup);
                }
                catch (Exception exception)
                {
                    context.Log.Debug(LogName, "не удалось удалить " + setup + ": " + exception.Message);
                }
            }
        }

        private static void RemoveRegistry(TweakContext context)
        {
            try
            {
                context.Registry.DeleteValue(RegistryHive.CurrentUser, RunKey, "OneDrive");
            }
            catch (Exception exception)
            {
                context.Log.Debug(LogName, "автозапуск: " + exception.Message);
            }

            // Убираем пункт OneDrive из проводника (обе ветки — 64 и 32 бита).
            foreach (string path in new[] { NavPaneClsid, NavPaneMachineClsid })
            {
                try
                {
                    RegistryHive hive = path.StartsWith("SOFTWARE", StringComparison.OrdinalIgnoreCase)
                        ? RegistryHive.LocalMachine
                        : RegistryHive.CurrentUser;

                    if (!context.Registry.KeyExists(hive, path))
                    {
                        continue;
                    }

                    context.BackupRegistryKey(hive, path);
                    context.Registry.WriteValue(hive, path, "System.IsPinnedToNameSpaceTree", 0, RegistryValueKind.DWord);
                    context.Registry.WriteValue(hive, path, "Attributes", 0, RegistryValueKind.DWord);
                }
                catch (Exception exception)
                {
                    context.Log.Debug(LogName, "проводник: " + exception.Message);
                }
            }
        }

        private static void RemoveTasks(TweakContext context)
        {
            try
            {
                context.Processes.ReadOutputAsync(
                    "powershell.exe",
                    "-NoProfile -NonInteractive -Command \"Get-ScheduledTask -TaskPath '\\Microsoft\\OneDrive\\*' " +
                    "-ErrorAction SilentlyContinue | Unregister-ScheduledTask -Confirm:$false -ErrorAction SilentlyContinue\"")
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception exception)
            {
                context.Log.Debug(LogName, "задачи: " + exception.Message);
            }
        }
    }
}
