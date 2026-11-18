using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>Перетаскивание окна за заголовок, логотип или подпись.</summary>
    public static class WindowDragBehavior
    {
        /// <summary>
        /// Заголовок/иконка: обычный клик вызывает действие, а перетаскивание
        /// с порогом двигает окно. Так настройки открываются с названия, но
        /// окно по-прежнему можно таскать за шапку.
        /// </summary>
        public static void AttachClickableDrag(Control control, Action click)
        {
            if (control == null)
            {
                return;
            }

            control.Cursor = SystemCursors.Hand;
            bool pressed = false;
            bool dragged = false;
            Point start = Point.Empty;

            control.MouseDown += delegate(object sender, MouseEventArgs args)
            {
                if (args.Button != MouseButtons.Left)
                {
                    return;
                }

                pressed = true;
                dragged = false;
                start = Cursor.Position;
            };

            control.MouseMove += delegate
            {
                if (!pressed || dragged)
                {
                    return;
                }

                Point now = Cursor.Position;
                if (Math.Abs(now.X - start.X) < 4 && Math.Abs(now.Y - start.Y) < 4)
                {
                    return;
                }

                dragged = true;
                pressed = false;

                Form form = control.FindForm();
                if (form == null)
                {
                    return;
                }

                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(
                    form.Handle,
                    NativeMethods.WmNcLButtonDown,
                    new IntPtr(NativeMethods.HtCaption),
                    IntPtr.Zero);
            };

            control.MouseUp += delegate(object sender, MouseEventArgs args)
            {
                if (args.Button != MouseButtons.Left)
                {
                    return;
                }

                if (pressed && !dragged && click != null)
                {
                    click();
                }

                pressed = false;
            };
        }

        public static void Attach(IEnumerable<Control> controls)
        {
            if (controls == null) return;

            foreach (Control control in controls)
            {
                if (control == null) continue;

                control.MouseDown += (sender, args) =>
                {
                    control.Capture = false;
                    Form form = control.FindForm();
                    if (form == null) return;

                    form.Capture = false;
                    NativeMethods.ReleaseCapture();
                    NativeMethods.SendMessage(
                        form.Handle,
                        NativeMethods.WmNcLButtonDown,
                        new IntPtr(NativeMethods.HtCaption),
                        IntPtr.Zero);
                };
            }
        }
    }
}
