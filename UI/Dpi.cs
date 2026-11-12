using System;
using System.Drawing;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>
    /// Масштабирование координат, которые заданы в коде.
    /// WinForms сам масштабирует дизайнерские контролы, но новые точки
    /// и панели нужно пересчитывать из 96 DPI в текущий.
    /// </summary>
    public static class Dpi
    {
        public static float Factor(Control control)
        {
            if (control == null) return 1f;

            int dpi = control.DeviceDpi;
            if (dpi <= 0)
            {
                using (Graphics graphics = control.CreateGraphics())
                {
                    dpi = (int)graphics.DpiX;
                }
            }

            return dpi <= 0 ? 1f : dpi / 96f;
        }

        public static int Scale(Control control, int value)
        {
            return (int)Math.Round(value * Factor(control));
        }

        public static Point Scale(Control control, int x, int y)
        {
            return new Point(Scale(control, x), Scale(control, y));
        }

        public static Size ScaleSize(Control control, int width, int height)
        {
            return new Size(Scale(control, width), Scale(control, height));
        }
    }
}
