using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>
    /// Ползунок полосы прокрутки.
    ///
    /// Обычная панель не умеет быть полупрозрачной, поэтому ползунок
    /// рисуется сам: скруглённый прямоугольник с нужной альфой. Отсюда
    /// плавное появление при прокрутке, подсветка под курсором и
    /// исчезновение, когда список прокручивать нечем.
    /// </summary>
    public sealed class ScrollThumb : Control
    {
        private const int FrameMs = 16;
        private const float IdleAlpha = 0.32f;
        private const float ActiveAlpha = 0.62f;
        private const int HoldMs = 520;

        private readonly Timer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly Stopwatch _holdClock = new Stopwatch();

        private float _alpha;
        private float _from;
        private float _to;
        private int _duration;
        private bool _hover;

        public ScrollThumb()
        {
            SetStyle(
                ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Cursor = SystemCursors.Hand;
            Visible = false;

            _timer = new Timer { Interval = FrameMs };
            _timer.Tick += OnTick;

            MouseEnter += delegate { _hover = true; Flash(); };
            MouseLeave += delegate { _hover = false; Settle(); };
        }

        /// <summary>Показать ползунок: плавно из нуля до обычной яркости.</summary>
        public void Reveal()
        {
            if (!Visible)
            {
                Visible = true;
                _alpha = 0f;
            }

            if (_alpha >= IdleAlpha - 0.01f && _to >= IdleAlpha - 0.01f)
            {
                return;
            }

            AnimateTo(IdleAlpha, 170);
        }

        /// <summary>Спрятать ползунок: плавно, а не рывком.</summary>
        public void FadeOut()
        {
            if (!Visible) return;

            if (_alpha <= 0.01f)
            {
                Visible = false;
                return;
            }

            AnimateTo(0f, 150);
        }

        /// <summary>Спрятать сразу — когда панель ушла с экрана.</summary>
        public void HideNow()
        {
            _timer.Stop();
            _clock.Reset();
            _alpha = 0f;
            _to = 0f;
            _holdClock.Reset();
            Visible = false;
        }

        /// <summary>Подсветить на время прокрутки, потом вернуться к обычной яркости.</summary>
        public void Flash()
        {
            if (!Visible)
            {
                Visible = true;
                _alpha = 0f;
            }

            AnimateTo(ActiveAlpha, 90);
            _holdClock.Restart();
        }

        private void Settle()
        {
            if (!Visible) return;
            AnimateTo(_hover ? ActiveAlpha : IdleAlpha, 260);
        }

        private void AnimateTo(float target, int duration)
        {
            _holdClock.Reset();
            _from = _alpha;
            _to = target;
            _duration = Math.Max(1, duration);
            _clock.Restart();
            _timer.Start();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (!Visible)
            {
                _timer.Stop();
                return;
            }

            if (_clock.ElapsedMilliseconds < _duration)
            {
                float t = _clock.ElapsedMilliseconds / (float)_duration;
                _alpha = _from + (_to - _from) * t;
                Invalidate();
                return;
            }

            _alpha = _to;

            // Подержать яркость после прокрутки, потом вернуться к обычной.
            if (_holdClock.IsRunning)
            {
                if (_holdClock.ElapsedMilliseconds < HoldMs)
                {
                    return;
                }

                Settle();
                return;
            }

            _timer.Stop();

            if (_alpha <= 0.01f)
            {
                Visible = false;
                return;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_alpha <= 0.01f || Width <= 0 || Height <= 0) return;

            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int alpha = (int)Math.Round(255 * Math.Min(1f, _alpha));
            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            float radius = Math.Min(Width, Height) / 2f;

            using (GraphicsPath path = Rounded(bounds, radius))
            using (var brush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
            {
                graphics.FillPath(brush, path);
            }
        }

        private static GraphicsPath Rounded(Rectangle bounds, float radius)
        {
            var path = new GraphicsPath();

            float diameter = radius * 2f;
            if (diameter <= 0f || bounds.Width <= diameter || bounds.Height <= diameter)
            {
                path.AddRectangle(bounds);
                return path;
            }

            var arc = new RectangleF(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
