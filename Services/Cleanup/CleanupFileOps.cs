using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WOTBO.Services.Cleanup
{
    internal struct CleanupMeasure
    {
        public long Bytes;
        public int Files;
    }

    internal struct CleanupDeleteResult
    {
        public long Freed;
        public int Files;

        public static CleanupDeleteResult Empty
        {
            get { return new CleanupDeleteResult(); }
        }
    }

    /// <summary>
    /// Подсчёт и удаление с защитой: не ходим по junction/symlink, не трогаем
    /// личные и системные корни, занятые файлы просто пропускаем.
    /// </summary>
    internal static class CleanupFileOps
    {
        private static readonly string[] Protected =
            new[]
        {
            @"C:\",
            CleanupLocations.Windows,
            Path.Combine(CleanupLocations.Windows, "System32"),
            Path.Combine(CleanupLocations.Windows, "SysWOW64"),
            CleanupLocations.UserProfile,
            Path.GetDirectoryName(CleanupLocations.UserProfile) ?? string.Empty,
            CleanupLocations.ProgramFiles,
            CleanupLocations.ProgramFilesX86,
            CleanupLocations.ProgramData,
            CleanupLocations.Local,
            CleanupLocations.Roaming,
            CleanupLocations.Downloads,
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
        }
        .Where(value => !string.IsNullOrEmpty(value))
        .Select(Normalize)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

        public static bool Exists(string path)
        {
            return !string.IsNullOrEmpty(path)
                && (File.Exists(path) || Directory.Exists(path));
        }

        public static CleanupMeasure Measure(string path, TimeSpan? minAge)
        {
            var result = new CleanupMeasure();
            if (!Exists(path))
            {
                return result;
            }

            string root = SafeFileSystem.TrustedPath(path);
            if (root == null)
            {
                return result;
            }

            DateTime cutoff = Cutoff(minAge);
            try
            {
                if (File.Exists(root))
                {
                    var file = new FileInfo(root);
                    if (file.LastWriteTimeUtc <= cutoff)
                    {
                        result.Bytes = file.Length;
                        result.Files = 1;
                    }

                    return result;
                }

                if (!Directory.Exists(root))
                {
                    return result;
                }

                foreach (string filePath in EnumerateFiles(root))
                {
                    try
                    {
                        var file = new FileInfo(filePath);
                        if (file.LastWriteTimeUtc > cutoff)
                        {
                            continue;
                        }

                        result.Bytes += file.Length;
                        result.Files++;
                    }
                    catch
                    {
                        // занятый или исчезнувший файл
                    }
                }
            }
            catch
            {
                // оставляем то, что успели посчитать
            }

            return result;
        }

        public static CleanupDeleteResult Delete(string path, TimeSpan? minAge)
        {
            var result = new CleanupDeleteResult();
            if (!Exists(path))
            {
                return result;
            }

            string root = SafeFileSystem.TrustedPath(path);
            if (root == null)
            {
                return result;
            }

            try
            {
                Guard(path);
                Guard(root);
            }
            catch
            {
                return result;
            }

            DateTime cutoff = Cutoff(minAge);
            try
            {
                if (File.Exists(root))
                {
                    var file = new FileInfo(root);
                    if (file.LastWriteTimeUtc > cutoff)
                    {
                        return result;
                    }

                    long length = file.Length;
                    if (SafeFileSystem.Delete(root, root))
                    {
                        result.Freed = length;
                        result.Files = 1;
                    }

                    return result;
                }

                if (!Directory.Exists(root))
                {
                    return result;
                }

                foreach (string filePath in EnumerateFiles(root).ToList())
                {
                    try
                    {
                        var file = new FileInfo(filePath);
                        if (file.LastWriteTimeUtc > cutoff)
                        {
                            continue;
                        }

                        long length = file.Length;
                        if (SafeFileSystem.Delete(filePath, root))
                        {
                            result.Freed += length;
                            result.Files++;
                        }
                    }
                    catch
                    {
                        // файл занят — пропускаем
                    }
                }

                // Пустые подпапки, начиная с самых глубоких. Сам root не удаляем.
                foreach (string directory in EnumerateDirectories(root)
                    .OrderByDescending(value => value.Length)
                    .ToList())
                {
                    SafeFileSystem.Delete(directory, root);
                }
            }
            catch
            {
                // best-effort
            }

            return result;
        }

        /// <summary>Удаляет папку, если после очистки она пуста. Защищённые корни не трогает.</summary>
        public static void RemoveIfEmpty(string path)
        {
            string real = SafeFileSystem.TrustedPath(path);
            if (real == null || !Directory.Exists(real))
            {
                return;
            }

            try
            {
                Guard(real);
            }
            catch
            {
                return;
            }

            SafeFileSystem.Delete(real, real);
        }

        private static DateTime Cutoff(TimeSpan? minAge)
        {
            return minAge.HasValue ? DateTime.UtcNow - minAge.Value : DateTime.MaxValue;
        }

        private static IEnumerable<string> EnumerateFiles(string root)
        {
            foreach (string entry in EnumerateEntries(root))
            {
                if (!Directory.Exists(entry))
                {
                    yield return entry;
                }
            }
        }

        private static IEnumerable<string> EnumerateDirectories(string root)
        {
            foreach (string entry in EnumerateEntries(root))
            {
                if (Directory.Exists(entry))
                {
                    yield return entry;
                }
            }
        }

        /// <summary>Обход без ссылок: junction/symlink не раскрываем.</summary>
        private static IEnumerable<string> EnumerateEntries(string root)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string directory = stack.Pop();
                IEnumerable<string> entries;
                try
                {
                    entries = Directory.EnumerateFileSystemEntries(directory).ToList();
                }
                catch
                {
                    continue;
                }

                foreach (string entry in entries)
                {
                    FileAttributes attributes;
                    try
                    {
                        attributes = File.GetAttributes(entry);
                    }
                    catch
                    {
                        continue;
                    }

                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }

                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        if (!string.Equals(Path.GetFileName(entry), ".net", StringComparison.OrdinalIgnoreCase))
                        {
                            stack.Push(entry);
                        }

                        continue;
                    }

                    yield return entry;
                }
            }
        }

        private static string Normalize(string path)
        {
            try
            {
                return Path.GetFullPath(path).TrimEnd('\\');
            }
            catch
            {
                return path.TrimEnd('\\');
            }
        }

        private static void Guard(string path)
        {
            string normalized = Normalize(path);
            if (normalized.Length <= 3)
            {
                throw new InvalidOperationException("Отказ: корень диска");
            }

            foreach (string root in Protected)
            {
                if (string.Equals(root, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Отказ: защищённая папка " + path);
                }
            }
        }
    }
}
