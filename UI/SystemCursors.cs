using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WOTBO.UI
{
    /// <summary>
    /// Курсоры из текущей схемы Windows (<c>HKCU\Control Panel\Cursors</c>).
    ///
    /// WinForms отдаёт «системный» <c>IDC_HAND</c>, который не совпадает с тем,
    /// что показывает проводник, если схема нестандартная (например, после
    /// твика «Установить новые курсоры»). Берём файл прямо из схемы — тогда в
    /// WOTBO ровно тот же курсор, что и в системе, включая размер под DPI.
    /// </summary>
    public static class SystemCursors
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadCursorFromFile(string path);

        private static Cursor _hand;

        /// <summary>Курсор «рука» (ссылки, навигация) из текущей схемы Windows.</summary>
        public static Cursor Hand
        {
            get
            {
                if (_hand == null)
                {
                    _hand = Load("Hand") ?? Cursors.Hand;
                }

                return _hand;
            }
        }

        private static Cursor Load(string name)
        {
            try
            {
                object value = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Cursors", name, null);
                string path = Convert.ToString(value);

                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                path = Environment.ExpandEnvironmentVariables(path);
                if (!File.Exists(path))
                {
                    return null;
                }

                IntPtr handle = LoadCursorFromFile(path);
                return handle == IntPtr.Zero ? null : new Cursor(handle);
            }
            catch
            {
                return null;
            }
        }
    }
}
