using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using WOTBO.UI;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Синие папки проводника (imageres).
    ///
    /// Раньше установка шла через .bat: cmd с `chcp 65001` ломал кириллицу в
    /// путях профиля, ошибки терялись, а твик всё равно считался применённым.
    /// Теперь всё делается нативно: остановка проводника, привилегированная
    /// замена файла через su.exe, ярлыки и иконки папок, очистка кэшей.
    /// Результат проверяется по SHA-256, признак «применён» — хеш установленного
    /// файла, а не файл `_bak`.
    /// </summary>
    public sealed class BlueFoldersTweak : TweakBase
    {
        private const string LogName = "Tweak:ui.bluefolders";

        private const string MunPath = @"C:\Windows\SystemResources\imageres.dll.mun";
        private const string DllPath = @"C:\Windows\System32\imageres.dll";
        private const string CompressedIconKey = @"SOFTWARE\Classes\CompressedFolder\DefaultIcon";

        private const string HashValue = "bluefolders.hash";
        private const string SizeValue = "bluefolders.size";
        private const string PrefixValue = "bluefolders.prefix";

        private const string Win11Url =
            "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/BlueIcon_Minimal.zip";
        private const string Win10Url =
            "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/imageres.zip";

        private const int PrefixBytes = 64 * 1024;

        /// <summary>Папка в архиве → куда копировать иконки и desktop.ini.</summary>
        private sealed class FolderIcon
        {
            public FolderIcon(string source, string target)
            {
                Source = source;
                Target = target;
            }

            public string Source { get; private set; }

            public string Target { get; private set; }
        }

        public override string Id
        {
            get { return "ui.bluefolders"; }
        }

        public override string Category
        {
            get { return "UI"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            string path = TargetPath(context);

            // Маркеры в HKCU появились в новой версии. Если иконки ставила
            // старая версия (или маркеры потерялись), остаётся _bak — по нему
            // и определяем, что файл уже подменён.
            bool legacyBackup = HasOriginalBackup(path);
            if (!File.Exists(path))
            {
                return legacyBackup ? TweakState.Applied : TweakState.NotApplied;
            }

            string root = WOTBO.Services.Registry.RegistryPaths.AppRoot;
            object prefix = context.Registry.GetValue(RegistryHive.CurrentUser, root, PrefixValue);
            object size = context.Registry.GetValue(RegistryHive.CurrentUser, root, SizeValue);

            if (prefix == null || size == null)
            {
                return legacyBackup ? TweakState.Applied : TweakState.NotApplied;
            }

            try
            {
                if (new FileInfo(path).Length != Convert.ToInt64(size))
                {
                    return TweakState.NotApplied;
                }

                return string.Equals(PrefixHash(path), Convert.ToString(prefix), StringComparison.OrdinalIgnoreCase)
                    ? TweakState.Applied
                    : TweakState.NotApplied;
            }
            catch
            {
                return legacyBackup ? TweakState.Applied : TweakState.NotApplied;
            }
        }

        private static bool HasOriginalBackup(string path)
        {
            string backup = path + "_bak";
            try
            {
                if (!File.Exists(backup))
                {
                    return false;
                }

                if (!File.Exists(path))
                {
                    return true;
                }

                var current = new FileInfo(path);
                var original = new FileInfo(backup);
                if (current.Length != original.Length)
                {
                    return true;
                }

                return !string.Equals(
                    PrefixHash(path),
                    PrefixHash(backup),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        public override void Apply(TweakContext context)
        {
            EnsureSu(context);

            bool win10 = context.WindowsVersion.Detect().IsWindows10;
            string path = TargetPath(context);
            string source = PrepareSource(context, win10);

            // Иконки папок качаем до остановки проводника, иначе рабочий стол
            // и панель задач пропадают на всё время загрузки.
            string icons = PrepareIcons(context);

            StopExplorer();

            try
            {
                if (!ReplacePrivileged(context, source, path))
                {
                    throw new TweakApplyException(
                        "Не удалось заменить " + Path.GetFileName(path) + ".\n\n" +
                        "Файл защищён системой — возможно, его держит антивирус или другая программа.\n" +
                        "Перезагрузите компьютер и попробуйте снова.");
                }

                if (!VerifyCopy(source, path))
                {
                    RestoreOriginal(context, path);
                    throw new TweakApplyException(
                        "Файл иконок скопировался некорректно (проверка хеша не прошла).\n\n" +
                        "Изменения отменены, иконки остались прежними.");
                }

                // На Win10 раньше менялась только imageres.dll, поэтому
                // оставались жёлтыми иконки Program Files, Windows, Users и
                // «Сжатой папки». Теперь ярлыки и desktop.ini применяются
                // на обеих системах.
                ApplyShortcuts(context, icons);
                ApplyFolderIcons(context, icons);
                SetCompressedFolderIcon(context);

                ClearIconCaches(context);
                SaveMarker(context, path);

                context.Log.Info(LogName, "иконки заменены и проверены");
            }
            catch
            {
                RestoreOriginal(context, path);
                throw;
            }
            finally
            {
                StartExplorer(context);
            }
        }

        public override void Rollback(TweakContext context)
        {
            EnsureSu(context);
            StopExplorer();

            try
            {
                RestoreOriginal(context, TargetPath(context));
                ClearIconCaches(context);
            }
            finally
            {
                StartExplorer(context);
            }

            // Ярлыки, иконки папок и значение реестра вернёт бэкап TweakScope.
        }

        #region подготовка архива
        private static string TargetPath(TweakContext context)
        {
            return context.WindowsVersion.Detect().IsWindows10 ? DllPath : MunPath;
        }

        /// <summary>Скачивает и распаковывает набор иконок, возвращает путь к файлу.</summary>
        private static string PrepareSource(TweakContext context, bool win10)
        {
            string root = context.Workspace.Root;
            Directory.CreateDirectory(root);

            if (win10)
            {
                string dll = Path.Combine(root, "imageres.dll");
                if (!File.Exists(dll))
                {
                    string zip = Path.Combine(root, "imageres.zip");
                    ResourceDownload.Ensure(context, Win10Url, zip, "imageres.zip (синие папки)");
                    Extract(zip, root);
                }

                if (!File.Exists(dll))
                {
                    throw new TweakApplyException("В архиве не нашёлся imageres.dll. Скачайте заново.");
                }

                return dll;
            }

            string folder = Path.Combine(root, "BlueIcon Minimal");
            string mun = Path.Combine(folder, "imageres.dll.mun");

            if (!File.Exists(mun))
            {
                string zip = Path.Combine(root, "BlueIcon_Minimal.zip");
                ResourceDownload.Ensure(context, Win11Url, zip, "BlueIcon_Minimal.zip (синие папки)");
                Extract(zip, folder);
            }

            if (!File.Exists(mun))
            {
                throw new TweakApplyException("В архиве не нашёлся imageres.dll.mun. Скачайте заново.");
            }

            return mun;
        }

        /// <summary>
        /// Папка с ярлыком проводника и desktop.ini для Program Files,
        /// Windows и Users. Нужна и на Win10 — оттуда берутся значки папок.
        /// </summary>
        private static string PrepareIcons(TweakContext context)
        {
            string root = context.Workspace.Root;
            string folder = Path.Combine(root, "BlueIcon Minimal");

            if (!File.Exists(Path.Combine(folder, "users", "users.ico")))
            {
                string zip = Path.Combine(root, "BlueIcon_Minimal.zip");
                ResourceDownload.Ensure(context, Win11Url, zip, "BlueIcon_Minimal.zip (синие папки)");
                Extract(zip, folder);
            }

            return folder;
        }

        private static void Extract(string zip, string destination)
        {
            try
            {
                Directory.CreateDirectory(destination);

                // 30 МБ иконок распаковываются пару секунд — в фоне, чтобы
                // окно не подвисало.
                if (Application.MessageLoop)
                {
                    using (var busy = new BusyForm("Синие папки", "Распаковываем иконки…"))
                    {
                        busy.Run(delegate { ZipFile.ExtractToDirectory(zip, destination); });

                        if (busy.Error != null)
                        {
                            throw busy.Error;
                        }
                    }
                }
                else
                {
                    ZipFile.ExtractToDirectory(zip, destination);
                }
            }
            catch (Exception exception)
            {
                throw new TweakApplyException(
                    "Архив с иконками повреждён: " + exception.Message + "\nУдалите временную папку и попробуйте снова.",
                    exception);
            }
        }
        #endregion

        #region замена системного файла
        /// <summary>
        /// ren original → *.bat, copy new → original. Всё через su.exe: ветка
        /// SystemResources принадлежит TrustedInstaller.
        /// </summary>
        private static bool ReplacePrivileged(TweakContext context, string source, string target)
        {
            string backup = target + "_bak";

            // Старый _bak — это и есть оригинал; не перезаписываем его,
            // иначе откат вернёт уже подменённый файл.
            if (!File.Exists(backup))
            {
                RunSuper(context, "cmd.exe /c if exist \"" + target + "\" ren \"" + target + "\" \"" + Path.GetFileName(backup) + "\"");
                Thread.Sleep(200);
            }

            RunSuper(context, "cmd.exe /c copy /y \"" + source + "\" \"" + target + "\"");
            Thread.Sleep(300);

            return File.Exists(target);
        }

        private static void RestoreOriginal(TweakContext context, string target)
        {
            string backup = target + "_bak";
            if (!File.Exists(backup))
            {
                return;
            }

            RunSuper(context, "cmd.exe /c if exist \"" + target + "\" del /f /q \"" + target + "\"");
            RunSuper(context, "cmd.exe /c ren \"" + backup + "\" \"" + Path.GetFileName(target) + "\"");
        }

        private static bool VerifyCopy(string source, string target)
        {
            try
            {
                return string.Equals(HashOf(source), HashOf(target), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// su.exe живёт в рабочей папке, а её чистит каждый запуск приложения.
        /// Перед привилегированной командой проверяем и при необходимости
        /// распаковываем заново.
        /// </summary>
        private static void EnsureSu(TweakContext context)
        {
            string path = context.Workspace.SuExecutable;
            if (File.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(context.Workspace.Root);
            File.WriteAllBytes(path, WOTBO.Properties.Resources.su);
            Thread.Sleep(100);
        }

        private static void RunSuper(TweakContext context, string command)
        {
            EnsureSu(context);
            context.Processes.RunSuper(command, context.Workspace.SuExecutable);
        }

        private static void StopExplorer()
        {
            foreach (Process process in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch
                {
                }
            }

            Thread.Sleep(500);
        }

        private static void StartExplorer(TweakContext context)
        {
            context.Processes.StartCaptured("explorer.exe");
        }
        #endregion

        #region ярлыки и иконки папок
        private static void ApplyShortcuts(TweakContext context, string folder)
        {
            string link = Path.Combine(folder, "File Explorer.lnk");
            if (!File.Exists(link))
            {
                return;
            }

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string[] targets =
            {
                Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs\System Tools\File Explorer.lnk"),
                Path.Combine(appData, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\File Explorer.lnk"),
            };

            foreach (string target in targets)
            {
                try
                {
                    string directory = Path.GetDirectoryName(target);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    context.BackupFile(target);
                    File.Copy(link, target, true);
                }
                catch (Exception exception)
                {
                    context.Log.Debug(LogName, "ярлык " + target + ": " + exception.Message);
                }
            }
        }

        private static void ApplyFolderIcons(TweakContext context, string folder)
        {
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string users = Path.Combine(Path.GetPathRoot(windows) ?? @"C:\", "Users");

            var icons = new[]
            {
                new FolderIcon("x64", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
                new FolderIcon("x86", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)),
                new FolderIcon("users", users),
                new FolderIcon("windows", windows),
            };

            foreach (FolderIcon icon in icons)
            {
                string source = Path.Combine(folder, icon.Source);
                if (string.IsNullOrEmpty(icon.Target) || !Directory.Exists(source))
                {
                    continue;
                }

                foreach (string file in Directory.GetFiles(source))
                {
                    string target = Path.Combine(icon.Target, Path.GetFileName(file));
                    try
                    {
                        context.BackupFile(target);
                        RunSuper(context, "cmd.exe /c copy /y \"" + file + "\" \"" + target + "\" & attrib +h \"" + target + "\"");
                    }
                    catch (Exception exception)
                    {
                        context.Log.Debug(LogName, "иконка " + target + ": " + exception.Message);
                    }
                }

                try
                {
                    // Чтобы Windows не вернула стандартные иконки папок.
                    var info = new DirectoryInfo(icon.Target);
                    info.Attributes |= FileAttributes.ReadOnly;
                }
                catch (Exception exception)
                {
                    context.Log.Debug(LogName, "атрибут папки " + icon.Target + ": " + exception.Message);
                }
            }
        }

        private static void SetCompressedFolderIcon(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.LocalMachine, CompressedIconKey);
            context.Registry.WriteValue(
                RegistryHive.LocalMachine,
                CompressedIconKey,
                string.Empty,
                @"%windir%\system32\imageres.dll,165",
                RegistryValueKind.ExpandString);
        }

        private static void ClearIconCaches(TweakContext context)
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string[] directories =
            {
                local,
                Path.Combine(local, @"Microsoft\Windows\Explorer"),
            };

            foreach (string directory in directories)
            {
                foreach (string pattern in new[] { "iconcache*", "thumbcache*" })
                {
                    try
                    {
                        foreach (string file in Directory.GetFiles(directory, pattern))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            context.Log.Debug(LogName, "кэш иконок очищен");
        }
        #endregion

        #region хеши
        private static void SaveMarker(TweakContext context, string path)
        {
            string root = WOTBO.Services.Registry.RegistryPaths.AppRoot;

            context.Registry.WriteValue(RegistryHive.CurrentUser, root, HashValue, HashOf(path), RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.CurrentUser, root, SizeValue, (int)new FileInfo(path).Length, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.CurrentUser, root, PrefixValue, PrefixHash(path), RegistryValueKind.String);
        }

        private static string HashOf(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static string PrefixHash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                int size = (int)Math.Min(stream.Length, PrefixBytes);
                var buffer = new byte[size];
                stream.Read(buffer, 0, size);
                return BitConverter.ToString(sha.ComputeHash(buffer)).Replace("-", string.Empty);
            }
        }
        #endregion
    }
}
