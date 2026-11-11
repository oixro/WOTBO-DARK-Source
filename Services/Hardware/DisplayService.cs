using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WOTBO.App.Logging;

namespace WOTBO.Services.Hardware
{
    public interface IDisplayService
    {
        string GetScreenInfo();
    }

    /// <summary>Разрешение/частота основного экрана.</summary>
    public sealed class DisplayService : IDisplayService
    {
        private const string Category = "Hardware";
        private const int VRefresh = 116;

        private readonly IAppLogger _log;

        public DisplayService(IAppLogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public string GetScreenInfo()
        {
            try
            {
                Screen primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen != null)
                {
                    Rectangle bounds = primaryScreen.Bounds;
                    int refreshRate = GetRefreshRate();
                    return $"{bounds.Width}x{bounds.Height} {refreshRate} Hz";
                }
            }
            catch (Exception ex)
            {
                _log.Debug(Category, "экран: " + ex.Message);
            }

            return string.Empty;
        }

        private static int GetRefreshRate()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            try
            {
                return GetDeviceCaps(hdc, VRefresh);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
    }
}
