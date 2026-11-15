using System;
using System.Drawing;
using System.Windows.Forms;
using WOTBO.UI;

namespace WOTBO
{
    /// <summary>
    /// Базовая форма WOTBO: тень DWM, перетаскивание за пустую область
    /// и изменение размера за края (рамки у окна нет — рисуем её сами
    /// через WM_NCHITTEST).
    /// </summary>
    public class ShadowForm : Form
    {
        private const int WmNcHitTest = 0x0084;
        private const int WmNcCalcSize = 0x0083;
        private const int WmDwmComposition = 0x0085;
        private const int WmNcLButtonDown = 0x00A3;

        private const int HitClient = 0x0001;
        private const int HitCaption = 0x0002;
        private const int HitLeft = 0x000A;
        private const int HitRight = 0x000B;
        private const int HitTop = 0x000C;
        private const int HitBottom = 0x000F;
        private const int HitTopLeft = 0x000D;
        private const int HitTopRight = 0x000E;
        private const int HitBottomLeft = 0x0010;
        private const int HitBottomRight = 0x0011;

        /// <summary>Ширина зоны захвата для ресайза, в пикселях 96 DPI.</summary>
        private const int ResizeBorder = 6;

        private readonly bool _aeroEnabled;

        public ShadowForm()
        {
            _aeroEnabled = CheckAeroEnabled();
        }

        /// <summary>Можно ли тянуть размер за края. Верх не трогаем: за него таскают окно.</summary>
        public bool Resizable { get; set; }

        private static bool CheckAeroEnabled()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    int enabled = 0;
                    NativeMethods.DwmIsCompositionEnabled(ref enabled);
                    return enabled == 1;
                }
            }
            catch
            {
            }

            return false;
        }

        protected override void WndProc(ref Message m)
        {
            // Разворачивание по двойному клику не нужно: окно не системное.
            if (m.Msg == WmNcLButtonDown)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == WmNcHitTest)
            {
                m.Result = (IntPtr)HitTest(m.LParam);
                return;
            }

            base.WndProc(ref m);

            switch (m.Msg)
            {
                case WmDwmComposition:
                    if (_aeroEnabled)
                    {
                        var value = 2;
                        NativeMethods.DwmSetWindowAttribute(Handle, 2, ref value, 4);

                        NativeMethods.Margins margins = new NativeMethods.Margins
                        {
                            BottomHeight = 1,
                            LeftWidth = 0,
                            RightWidth = 0,
                            TopHeight = 0,
                        };

                        NativeMethods.DwmExtendFrameIntoClientArea(Handle, ref margins);
                    }

                    break;

                case WmNcCalcSize:
                    m.Result = IntPtr.Zero;
                    break;
            }
        }

        private int HitTest(IntPtr lParam)
        {
            if (!Resizable || WindowState != FormWindowState.Normal)
            {
                return HitCaption;
            }

            long value = lParam.ToInt64();
            var screen = new Point((short)(value & 0xFFFF), (short)((value >> 16) & 0xFFFF));
            Point point = PointToClient(screen);

            int border = Dpi.Px(ResizeBorder);

            // Тянем только по вертикали: горизонтальный размер окна
            // фиксирован, поэтому бока и углы не отдают коды ресайза.
            bool top = point.Y <= border;
            bool bottom = point.Y >= ClientSize.Height - border;

            if (top) return HitTop;
            if (bottom) return HitBottom;

            // Пустая область окна — как раньше: тянем окно целиком.
            return HitCaption;
        }
    }
}
