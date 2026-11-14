using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WOTBO.App
{
    /// <summary>
    /// Единая иконка окна: берётся из ресурсов самого exe (ApplicationIcon),
    /// поэтому в формах не нужно хранить копию иконки в .resx.
    /// </summary>
    internal static class AppIcon
    {
        private const int AppIconResourceId = 32512;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadIcon(IntPtr instance, IntPtr name);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        public static void Apply(Form form)
        {
            if (form == null)
            {
                return;
            }

            try
            {
                IntPtr handle = LoadIcon(GetModuleHandle(null), new IntPtr(AppIconResourceId));
                if (handle != IntPtr.Zero)
                {
                    using (Icon shared = Icon.FromHandle(handle))
                    {
                        form.Icon = (Icon)shared.Clone();
                    }

                    return;
                }
            }
            catch
            {
            }

            try
            {
                form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
            }
        }
    }
}
