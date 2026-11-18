using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Cleanup
{
    public enum CleanupKind
    {
        Directory,
        Thumbnails,
        RecycleBin,
        Dism,
    }

    /// <summary>Один пункт глубокой очистки.</summary>
    public sealed class CleanupItem
    {
        public CleanupItem(string id, string name, string path, CleanupKind kind)
        {
            Id = id;
            Name = name;
            Path = path;
            Kind = kind;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Path { get; private set; }

        public CleanupKind Kind { get; private set; }

        public long Size { get; set; }

        public override string ToString()
        {
            return Kind == CleanupKind.Dism
                ? Name
                : Name + " — " + CleanupService.FormatSize(Size);
        }
    }

    /// <summary>
    /// Подсчёт размера и удаление временных данных. Всё best-effort: занятые
    /// файлы пропускаются, ошибки не валят остальные пункты.
    /// </summary>
    public sealed class CleanupService
    {
        private const string Category = "Cleanup";
        private const uint SherbNoConfirmation = 0x00000001;
        private const uint SherbNoProgressUi = 0x00000002;
        private const uint SherbNoSound = 0x00000004;

        private readonly IProcessService _processes;
        private readonly IAppLogger _log;

        public CleanupService(IProcessService processes, IAppLogger log)
        {
            _processes = processes ?? throw new ArgumentNullException(nameof(processes));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public List<CleanupItem> CreateItems()
        {
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return new List<CleanupItem>
            {
                new CleanupItem("temp", "Временные файлы пользователя", Path.GetTempPath(), CleanupKind.Directory),
                new CleanupItem("wintemp", "Временные файлы Windows", Path.Combine(windows, "Temp"), CleanupKind.Directory),
                new CleanupItem("update", "Кэш обновлений Windows", Path.Combine(windows, "SoftwareDistribution", "Download"), CleanupKind.Directory),
                new CleanupItem("winsxs", "Компоненты WinSxS (DISM ResetBase)", string.Empty, CleanupKind.Dism),
                new CleanupItem("prefetch", "Prefetch", Path.Combine(windows, "Prefetch"), CleanupKind.Directory),
                new CleanupItem("thumbs", "Кэш эскизов", Path.Combine(local, @"Microsoft\Windows\Explorer"), CleanupKind.Thumbnails),
                new CleanupItem("crash", "Дампы сбоев", Path.Combine(local, "CrashDumps"), CleanupKind.Directory),
                new CleanupItem("recycle", "Корзина", string.Empty, CleanupKind.RecycleBin),
            };
        }

        public Task<long> ScanAsync(CleanupItem item)
        {
            return Task.Run(() =>
            {
                try
                {
                    switch (item.Kind)
                    {
                        case CleanupKind.RecycleBin:
                            return QueryRecycleBin();

                        case CleanupKind.Dism:
                            return 0L;

                        case CleanupKind.Thumbnails:
                            return Measure(item.Path, IsThumbnail);

                        default:
                            return Measure(item.Path, null);
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(Category, "размер " + item.Name + ": " + ex.Message);
                    return 0L;
                }
            });
        }

        public Task<long> CleanAsync(CleanupItem item)
        {
            return Task.Run(() =>
            {
                try
                {
                    switch (item.Kind)
                    {
                        case CleanupKind.RecycleBin:
                            return EmptyRecycleBin() ? Math.Max(0, item.Size) : 0L;

                        case CleanupKind.Dism:
                            return CleanupWinSxs();

                        case CleanupKind.Thumbnails:
                            return DeleteFiles(item.Path, IsThumbnail);

                        default:
                            return DeleteFiles(item.Path, null);
                    }
                }
                catch (Exception ex)
                {
                    _log.Warn(Category, "очистка " + item.Name + ": " + ex.Message);
                    return 0L;
                }
            });
        }

        public static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L)
            {
                return (bytes / (1024d * 1024d * 1024d)).ToString("0.0", CultureInfo.CurrentCulture) + " ГБ";
            }

            if (bytes >= 1024L * 1024L)
            {
                return (bytes / (1024d * 1024d)).ToString("0", CultureInfo.CurrentCulture) + " МБ";
            }

            if (bytes >= 1024L)
            {
                return (bytes / 1024d).ToString("0", CultureInfo.CurrentCulture) + " КБ";
            }

            return bytes.ToString(CultureInfo.CurrentCulture) + " Б";
        }

        private long CleanupWinSxs()
        {
            ProcessOutput output = _processes.RunAsync(
                "dism.exe",
                "/Online /Cleanup-Image /StartComponentCleanup /ResetBase",
                1800000,
                Encoding.UTF8).GetAwaiter().GetResult();

            _log.Info("Cleanup", "DISM ResetBase: код " + output.ExitCode);
            if (!output.Ok)
            {
                _log.Warn("Cleanup", "DISM ResetBase: " + output.Text.Trim());
            }

            return 0L;
        }

        private static bool IsThumbnail(string path)
        {
            string name = Path.GetFileName(path);
            return name.StartsWith("thumbcache_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("iconcache_", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsProtectedLog(string path)
        {
            string name = Path.GetFileName(path);
            return name.StartsWith("wotbo", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".log", StringComparison.OrdinalIgnoreCase);
        }

        private static long Measure(string path, Func<string, bool> filter)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return 0L;
            }

            long total = 0;
            foreach (string file in EnumerateFiles(path))
            {
                try
                {
                    if (filter != null && !filter(file))
                    {
                        continue;
                    }

                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // занятый или исчезнувший файл
                }
            }

            return total;
        }

        private long DeleteFiles(string path, Func<string, bool> filter)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return 0L;
            }

            long freed = 0;
            foreach (string file in EnumerateFiles(path).ToList())
            {
                try
                {
                    if (IsProtectedLog(file))
                    {
                        continue;
                    }

                    if (filter != null && !filter(file))
                    {
                        continue;
                    }

                    var info = new FileInfo(file);
                    long length = info.Length;
                    info.Delete();
                    freed += length;
                }
                catch
                {
                    // файл занят — пропускаем
                }
            }

            foreach (string directory in EnumerateDirectories(path))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch
                {
                    // занятая папка
                }
            }

            _log.Info(Category, "очищено " + FormatSize(freed) + " в " + path);
            return freed;
        }

        private static IEnumerable<string> EnumerateFiles(string path)
        {
            try
            {
                return Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        private static IEnumerable<string> EnumerateDirectories(string path)
        {
            try
            {
                return Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                    .OrderByDescending(value => value.Length)
                    .ToList();
            }
            catch
            {
                return Enumerable.Empty<string>();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RecycleBinInfo
        {
            public uint Size;
            public long Items;
            public long Bytes;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string rootPath, ref RecycleBinInfo info);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string rootPath, uint flags);

        private static long QueryRecycleBin()
        {
            var info = new RecycleBinInfo();
            info.Size = (uint)Marshal.SizeOf(typeof(RecycleBinInfo));
            return SHQueryRecycleBin(null, ref info) == 0 ? info.Bytes : 0L;
        }

        private static bool EmptyRecycleBin()
        {
            return SHEmptyRecycleBin(
                IntPtr.Zero,
                null,
                SherbNoConfirmation | SherbNoProgressUi | SherbNoSound) == 0;
        }
    }
}
