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
        private static Control _reference;
        private static float _designWidth;

        /// <summary>
        /// Задаёт контроль, по ширине которого вычисляется масштаб.
        /// Нужно потому, что DeviceDpi может вернуть 96 даже при
        /// масштабировании 125–200 %.
        /// </summary>
        public static void Configure(Control reference, float designWidth)
        {
            _reference = reference;
            _designWidth = designWidth;
        }

        public static float Factor(Control control)
        {
            if (control == null) return 1f;

            if (_reference != null && !_reference.IsDisposed &&
                _reference.Width > 0 && _designWidth > 0)
            {
                Form referenceForm = _reference.FindForm();
                Form form = control as Form;
                if (form == null) form = control.FindForm();
                if (referenceForm == null || form == null || referenceForm == form)
                {
                    return _reference.Width / _designWidth;
                }
            }

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
