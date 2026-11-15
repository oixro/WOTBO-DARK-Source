using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>
    /// Подчёркивание активного пункта меню.
    ///
    /// Раньше активная подпись получала <c>FontStyle.Underline</c> — линия
    /// появлялась рывком вместе со сменой шрифта. Здесь линия рисуется сама:
    /// под прежним пунктом она быстро стягивается, под новым — выезжает
    /// слева направо. Пункт при этом остаётся обычным шрифтом, поэтому
    /// ширина подписи и вёрстка меню не меняются.
    /// </summary>
    public sealed class NavIndicator : Control
    {
        private const int FrameMs = 15;
        private const int LineHeight = 2;

        private readonly Timer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly Queue<Step> _steps = new Queue<Step>();

        private Step _current;
        private Label _target;

        public NavIndicator()
        {
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Visible = false;
            Enabled = false;

            _timer = new Timer { Interval = FrameMs };
            _timer.Tick += OnTick;
        }

        /// <summary>Перевести подчёркивание на пункт <paramref name="label"/>.</summary>
        public void PointTo(Label label)
        {
            PointTo(label, true);
        }

        public void PointTo(Label label, bool animate)
        {
            _steps.Clear();
            _current = null;
            _timer.Stop();

            if (label == null || label.IsDisposed || label.Parent == null)
            {
                FadeAway(_target);
                return;
            }

            bool same = _target == label;

            // Повторный клик по активному разделу ничего не перерисовывает.
            if (same && (!animate || Width >= label.Width - 1))
            {
                Place(label, label.Width);
                return;
            }

            if (!animate)
            {
                _target = label;
                Place(label, label.Width);
                return;
            }

            // Первое открытие вкладки: линия просто выезжает с нуля.
            if (!Visible || Width <= 1)
            {
                _steps.Enqueue(new Step { Label = label, From = 0, To = label.Width, Duration = 170 });
                _target = label;
                StartNext();
                return;
            }

            // Сначала убираем линию с прежнего пункта, потом выводим на новом.
            if (!same)
            {
                _steps.Enqueue(new Step { Label = _target, From = Width, To = 0, Duration = 90 });
            }

            _steps.Enqueue(new Step { Label = label, From = 0, To = label.Width, Duration = 150 });
            _target = label;

            StartNext();
        }

        /// <summary>Убрать подчёркивание (стартовый экран).</summary>
        public void Clear()
        {
            Label previous = _target;

            _steps.Clear();
            _current = null;
            _target = null;

            FadeAway(previous);
        }

        private void FadeAway(Label label)
        {
            if (!Visible) return;

            _steps.Enqueue(new Step { Label = label, From = Width, To = 0, Duration = 120 });
            StartNext();
        }

        private void StartNext()
        {
            if (_steps.Count == 0)
            {
                _current = null;
                _timer.Stop();
                if (Width <= 1) Visible = false;
                return;
            }

            _current = _steps.Dequeue();
            _clock.Restart();
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            Step step = _current;
            if (step == null)
            {
                _timer.Stop();
                return;
            }

            float t = Math.Min(1f, _clock.ElapsedMilliseconds / (float)step.Duration);
            float eased = 1f - (1f - t) * (1f - t);
            int width = (int)Math.Round(step.From + (step.To - step.From) * eased);

            if (step.Label != null && !step.Label.IsDisposed && step.Label.Parent != null)
            {
                Place(step.Label, width);
            }

            if (t >= 1f)
            {
                StartNext();
            }
            else
            {
                Invalidate();
            }
        }

        private void Place(Label label, int width)
        {
            if (label == null || label.IsDisposed) return;

            if (Parent != label.Parent)
            {
                Parent = label.Parent;
            }

            Width = Math.Max(0, width);
            Height = LineHeight;
            Location = new Point(label.Left, label.Top + label.Height);
            Visible = width > 0;

            Invalidate();
        }

        /// <summary>Клики проходят мимо: линия не должна перехватывать нажатия на пункт.</summary>
        protected override void WndProc(ref Message message)
        {
            const int HitTestMessage = 0x0084;
            const int Transparent = -1;

            if (message.Msg == HitTestMessage)
            {
                message.Result = (IntPtr)Transparent;
                return;
            }

            base.WndProc(ref message);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Width <= 0 || Height <= 0) return;

            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = new GraphicsPath())
            {
                float radius = Height / 2f;
                path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();

                using (var brush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
                {
                    graphics.FillPath(brush, path);
                }
            }
        }

        private sealed class Step
        {
            public Label Label;
            public int From;
            public int To;
            public int Duration;
        }
    }
}
