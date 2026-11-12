using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>Перетаскивание окна за заголовок, логотип или подпись.</summary>
    public static class WindowDragBehavior
    {
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
