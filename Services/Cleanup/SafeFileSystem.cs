using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace WOTBO.Services.Cleanup
{
    /// <summary>
    /// Проверки и удаление через handle, а не по строке пути: junction или
    /// symlink нельзя подсунуть между проверкой и удалением. Логика взята из
    /// Roomy (MIT), адаптирована под .NET Framework 4.8.
    /// </summary>
    internal static class SafeFileSystem
    {
        private const uint DeleteAccess = 0x00010000;
        private const uint ReadAttributes = 0x80;
        private const uint WriteAttributes = 0x100;
        private const uint ShareAll = 0x1 | 0x2 | 0x4;
        private const uint OpenExisting = 3;
        private const uint BackupSemantics = 0x02000000;
        private const uint OpenReparsePoint = 0x00200000;
        private const int FileDispositionInfo = 4;
        private const int FileDispositionInfoEx = 21;
        private const uint DispositionDelete = 0x1;
        private const uint DispositionPosix = 0x2;
        private const uint DispositionIgnoreReadOnly = 0x10;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW(
            string name, uint access, uint share, IntPtr security,
            uint disposition, uint flags, IntPtr template);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFinalPathNameByHandleW(
            SafeFileHandle handle, StringBuilder path, uint length, uint flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetLongPathNameW(
            string shortPath, StringBuilder longPath, uint length);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileInformationByHandle(
            SafeFileHandle handle, int infoClass, ref uint info, uint size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileInformationByHandle(
            SafeFileHandle handle, int infoClass, ref byte info, uint size);

        private static SafeFileHandle Open(string path, bool forDelete)
        {
            return CreateFileW(
                @"\\?\" + path,
                ReadAttributes | (forDelete ? DeleteAccess | WriteAttributes : 0),
                ShareAll,
                IntPtr.Zero,
                OpenExisting,
                BackupSemantics | OpenReparsePoint,
                IntPtr.Zero);
        }

        private static string FinalPath(SafeFileHandle handle)
        {
            var buffer = new StringBuilder(1024);
            uint length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 0);
            if (length == 0)
            {
                return null;
            }

            if (length > buffer.Capacity)
            {
                buffer = new StringBuilder((int)length + 1);
                length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 0);
                if (length == 0 || length > buffer.Capacity)
                {
                    return null;
                }
            }

            string text = buffer.ToString();
            if (text.StartsWith(@"\\?\UNC\", StringComparison.Ordinal))
            {
                return @"\\" + text.Substring(8);
            }

            return text.StartsWith(@"\\?\", StringComparison.Ordinal) ? text.Substring(4) : text;
        }

        private static string LongPath(string path)
        {
            string full = System.IO.Path.GetFullPath(path);
            var buffer = new StringBuilder(1024);
            uint length = GetLongPathNameW(@"\\?\" + full, buffer, (uint)buffer.Capacity);
            if (length == 0 || length > buffer.Capacity)
            {
                return null;
            }

            string text = buffer.ToString();
            return text.StartsWith(@"\\?\", StringComparison.Ordinal) ? text.Substring(4) : text;
        }

        private static string Trim(string path)
        {
            return path.Length > 3 ? path.TrimEnd('\\') : path;
        }

        /// <summary>
        /// Реальный путь, если ни один компонент пути не является junction/symlink
        /// и сам элемент не ссылка. Иначе null — такой путь не трогаем.
        /// </summary>
        public static string TrustedPath(string path)
        {
            try
            {
                string expected = LongPath(path);
                if (expected == null)
                {
                    return null;
                }

                using (SafeFileHandle handle = Open(expected, false))
                {
                    if (handle.IsInvalid)
                    {
                        return null;
                    }

                    string real = FinalPath(handle);
                    if (real == null
                        || !string.Equals(Trim(real), Trim(expected), StringComparison.OrdinalIgnoreCase))
                    {
                        return null;
                    }

                    if ((System.IO.File.GetAttributes(expected) & System.IO.FileAttributes.ReparsePoint) != 0)
                    {
                        return null;
                    }

                    return Trim(real);
                }
            }
            catch
            {
                return null;
            }
        }

        public static bool IsInside(string real, string root)
        {
            return string.Equals(real, root, StringComparison.OrdinalIgnoreCase)
                || real.StartsWith(root.EndsWith("\\") ? root : root + "\\", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Удаляет файл или пустую папку, только если она реально лежит внутри
        /// trustedRoot. Проверка и удаление идут через один handle.
        /// </summary>
        public static bool Delete(string path, string trustedRoot)
        {
            try
            {
                using (SafeFileHandle handle = Open(System.IO.Path.GetFullPath(path), true))
                {
                    if (handle.IsInvalid)
                    {
                        return false;
                    }

                    string real = FinalPath(handle);
                    if (real == null || !IsInside(Trim(real), trustedRoot))
                    {
                        return false;
                    }

                    uint flags = DispositionDelete | DispositionPosix | DispositionIgnoreReadOnly;
                    if (SetFileInformationByHandle(handle, FileDispositionInfoEx, ref flags, sizeof(uint)))
                    {
                        return true;
                    }

                    // FAT и некоторые сетевые ФС знают только старый вызов.
                    byte delete = 1;
                    return SetFileInformationByHandle(handle, FileDispositionInfo, ref delete, 1);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
