using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>
    /// Прокрутка панели с тонкой полосой в стиле WOTBO.
    ///
    /// Системная полоса отнимает 17 px ширины у строк (а они уже почти
    /// вплотную) и рисуется светлым прямоугольником на тёмном фоне, поэтому
    /// полоса своя: 6 px поверх контента, без сдвига строк.
    /// </summary>
    public sealed class PanelScroller
    {
        private const int WheelMessage = 0x020A;
        private const int SetRedrawMessage = 0x000B;
        private const int ThumbWidth = 6;
        private const int ThumbMargin = 3;
        private const int WheelStep = 48;
        private const int MinThumbHeight = 28;

        private static readonly List<PanelScroller> Active = new List<PanelScroller>();
        private static readonly Dictionary<Panel, PanelScroller> ByPanel = new Dictionary<Panel, PanelScroller>();
        private static readonly HashSet<Panel> Locked = new HashSet<Panel>();
        private static bool _filterInstalled;

        private readonly Panel _panel;
        private readonly ScrollThumb _thumb;
        private int _offset;
        private bool _dragging;
        private int _dragCursorY;
        private int _dragOffset;

        public PanelScroller(Panel panel)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));

            _panel = panel;
            _panel.AutoScroll = false;

            _thumb = new ScrollThumb
            {
                Name = panel.Name + "_thumb",
                Width = ThumbWidth,
            };
            _thumb.MouseDown += ThumbMouseDown;
            _thumb.MouseMove += ThumbMouseMove;
            _thumb.MouseUp += ThumbMouseUp;

            _panel.Controls.Add(_thumb);
            _thumb.BringToFront();

            _panel.VisibleChanged += delegate
            {
                if (_panel.Visible)
                {
                    ScrollToTop();
                }
                else
                {
                    _thumb.HideNow();
                }
            };
            _panel.Resize += delegate { UpdateThumb(); };

            Active.Add(this);
            ByPanel[panel] = this;

            if (!_filterInstalled)
            {
                Application.AddMessageFilter(new WheelRouter());
                _filterInstalled = true;
            }
        }

        /// <summary>Высота содержимого: самая нижняя строка плюс отступ.</summary>
        public int ContentHeight
        {
            get
            {
                int bottom = 0;
                foreach (Control control in _panel.Controls)
                {
                    if (control == _thumb || !control.Visible) continue;
                    bottom = Math.Max(bottom, control.Bottom);
                }

                // Позиции строк уже сдвинуты прокруткой — возвращаем исходные,
                // иначе предел прокрутки «съезжает» с каждым шагом.
                return bottom + _offset + 8;
            }
        }

        /// <summary>Пересчитать полосу после того, как содержимое панели заменили.</summary>
        public static void Refresh(Panel panel)
        {
            PanelScroller scroller;
            if (panel != null && ByPanel.TryGetValue(panel, out scroller))
            {
                scroller.ScrollToTop();
            }
        }

        /// <summary>
        /// Запретить прокрутку на время анимации появления: строки в этот
        /// момент рисуются из снимка панели, и сдвиг сбил бы их с кадра.
        /// </summary>
        public static void Lock(Panel panel)
        {
            if (panel != null) Locked.Add(panel);
        }

        public static void Unlock(Panel panel)
        {
            if (panel != null) Locked.Remove(panel);
        }

        public bool IsLocked
        {
            get { return Locked.Contains(_panel); }
        }

        public void ScrollToTop()
        {
            SetOffset(0);
        }

        public void ScrollBy(int delta)
        {
            SetOffset(_offset + delta);
        }

        private void SetOffset(int value)
        {
            if (IsLocked) return;

            int maximum = Math.Max(0, ContentHeight - _panel.ClientSize.Height);
            value = Math.Max(0, Math.Min(maximum, value));

            int shift = _offset - value;
            if (shift == 0)
            {
                UpdateThumb();
                return;
            }

            // Пока строки сдвигаются, окно не перерисовываем: иначе видно
            // промежуточные кадры и полосы от прежних позиций.
            _panel.SuspendLayout();
            NativeMethods.SendMessage(_panel.Handle, SetRedrawMessage, IntPtr.Zero, IntPtr.Zero);
            foreach (Control control in _panel.Controls)
            {
                if (control == _thumb) continue;
                control.Top += shift;
            }
            NativeMethods.SendMessage(_panel.Handle, SetRedrawMessage, (IntPtr)1, IntPtr.Zero);
            _panel.ResumeLayout();
            _offset = value;

            UpdateThumb();
            _panel.Invalidate(true);
            _panel.Update();
            _thumb.Flash();
        }

        private void UpdateThumb()
        {
            int content = ContentHeight;
            int height = _panel.ClientSize.Height;

            if (content <= height + 1)
            {
                _thumb.FadeOut();
                _offset = 0;
                return;
            }

            int track = height - ThumbMargin * 2;
            int thumbHeight = Math.Max(MinThumbHeight, (int)((long)track * height / content));
            int travel = track - thumbHeight;
            int position = travel <= 0 ? 0 : (int)((long)travel * _offset / (content - height));

            _thumb.SetBounds(_panel.ClientSize.Width - ThumbWidth - ThumbMargin, ThumbMargin + position, ThumbWidth, thumbHeight);
            _thumb.Reveal();
            _thumb.BringToFront();
        }

        private void ThumbMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragging = true;
            _dragCursorY = Cursor.Position.Y;
            _dragOffset = _offset;
        }

        private void ThumbMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            int content = ContentHeight;
            int height = _panel.ClientSize.Height;
            int track = height - ThumbMargin * 2;
            int thumbHeight = _thumb.Height;
            int travel = track - thumbHeight;
            if (travel <= 0 || content <= height) return;

            int delta = Cursor.Position.Y - _dragCursorY;
            int moved = (int)((long)delta * (content - height) / travel);
            SetOffset(_dragOffset + moved);
        }

        private void ThumbMouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
        }

        /// <summary>
        /// Колесо мыши уходит в контрол под курсором, поэтому ловим его
        /// фильтром сообщений и крутим ту панель, над которой курсор.
        /// </summary>
        private sealed class WheelRouter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message message)
            {
                if (message.Msg != WheelMessage) return false;

                Point point = Cursor.Position;
                int delta = (short)((long)message.WParam >> 16);

                foreach (PanelScroller scroller in Active)
                {
                    if (!scroller._panel.Visible || scroller.IsLocked) continue;

                    Rectangle bounds = scroller._panel.RectangleToScreen(scroller._panel.ClientRectangle);
                    if (!bounds.Contains(point)) continue;

                    scroller.ScrollBy(-delta * WheelStep / 120);
                    return true;
                }

                return false;
            }
        }
    }
}
