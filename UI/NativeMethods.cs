using System;
using System.Runtime.InteropServices;

namespace WOTBO.UI
{
    /// <summary>Все Win32-вызовы, которые нужны оболочке WOTBO.</summary>
    public static class NativeMethods
    {
        public const int WmNcLButtonDown = 0x00A1;
        public const int HtCaption = 0x0002;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins margins);

        [DllImport("shcore.dll")]
        private static extern int GetProcessDpiAwareness(IntPtr process, out int awareness);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>0 — DPI не учитывается, 1 — системный, 2 — на монитор.</summary>
        public static int DpiAwareness()
        {
            try
            {
                int value;
                return GetProcessDpiAwareness(GetCurrentProcess(), out value) == 0 ? value : -1;
            }
            catch
            {
                return -2;
            }
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(ref int pfEnabled);

        public struct Margins
        {
            public int LeftWidth;
            public int RightWidth;
            public int TopHeight;
            public int BottomHeight;
        }
    }
}
