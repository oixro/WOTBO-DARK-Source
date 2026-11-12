using System;
using System.Windows.Forms;
using WOTBO.UI;

namespace Project
{
    // Наследуемся от Form
    public class FormShadow : Form
    {
        #region Тень формы + перетаскивание окна + предотвращение разворачивания
#if !DEBUG
        readonly bool m_aeroEnabled;

        public FormShadow() => m_aeroEnabled = CheckAeroEnabled();

        bool CheckAeroEnabled()
        {
            if (Environment.OSVersion.Version.Major >= 6)
            {
                int enabled = 0;
                NativeMethods.DwmIsCompositionEnabled(ref enabled);
                return enabled == 1;
            }
            return false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x00A3)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            base.WndProc(ref m);
            switch (m.Msg)
            {
                case 0x0085:
                    if (m_aeroEnabled)
                    {
                        var v = 2; NativeMethods.DwmSetWindowAttribute(Handle, 2, ref v, 4);
                        NativeMethods.Margins margins = new NativeMethods.Margins()
                        {
                            BottomHeight = 1,
                            LeftWidth = 0,
                            RightWidth = 0,
                            TopHeight = 0
                        };
                        NativeMethods.DwmExtendFrameIntoClientArea(Handle, ref margins);
                    }
                    break;
                case 0x0083:
                    m.Result = (IntPtr)0; break;
                case 0x84 when (int)m.Result == 0x1: m.Result = (IntPtr)0x2; break;
                default: break;
            }
        }
#endif
        #endregion
    }
}
