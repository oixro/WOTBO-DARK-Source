using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using WOTBO.App.Logging;

namespace WOTBO.UI
{
    /// <summary>
    /// Появление строк вкладки «лесенкой»: каждая строка (чекбокс твика и его
    /// стрелка отката) проявляется из фона, начинаясь чуть позже предыдущей.
    ///
    /// WinForms не умеет менять прозрачность отдельного контрола. Поэтому
    /// панель один раз снимается в bitmap, контролы на время анимации
    /// скрываются, а кадры рисуются в <see cref="Control.Paint"/> панели с
    /// нужной альфой. Фон панели (градиент) при этом остаётся живым, никакие
    /// контролы не пересоздаются, очередь сообщений не блокируется — кадры
    /// идут по таймеру, а перерисовываются только строки, которые меняются.
    /// </summary>
    public sealed class PanelReveal : IDisposable
    {
        private const int FrameMs = 15;
        private const int FadeMs = 180;
        private const int MaxStaggerMs = 18;
        private const int StaggerBudgetMs = 280;
        private const int RowTolerance = 4;
        private const int SlidePixels = 10;

        private readonly Timer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly ImageAttributes _attributes = new ImageAttributes();
        private readonly ColorMatrix _matrix = new ColorMatrix();

        private Panel _panel;
        private Bitmap _snapshot;
        private Row[] _rows;
        private PaintEventHandler _paint;
        private int _totalMs;
        private int _slide;

        public PanelReveal()
        {
            _timer = new Timer { Interval = FrameMs };
            _timer.Tick += OnTick;
        }

        /// <summary>Выключается твиком «Отключить анимации».</summary>
        public Func<bool> Enabled { get; set; }

        public bool IsPlaying
        {
            get { return _panel != null; }
        }

        /// <summary>Показать панель «лесенкой». Повторный вызов отменяет предыдущую анимацию.</summary>
        public void Play(Panel panel)
        {
            Stop();

            if (panel == null || panel.IsDisposed) return;
            if (Enabled != null && !Enabled()) return;
            if (!panel.Visible || panel.Width <= 0 || panel.Height <= 0) return;

            var children = new List<Control>();
            foreach (Control control in panel.Controls)
            {
                if (control.Visible && !IsChrome(control))
                {
                    children.Add(control);
                }
            }

            if (children.Count == 0) return;

            children.Sort(CompareControls);
            List<Row> rows = Group(children);
            if (rows.Count == 0) return;

            var watch = Stopwatch.StartNew();
            var snapshot = Capture(panel);
            if (snapshot == null) return;

            _rows = rows.ToArray();
            _snapshot = snapshot;

            int stagger = _rows.Length > 1
                ? Math.Min(MaxStaggerMs, Math.Max(6, StaggerBudgetMs / (_rows.Length - 1)))
                : 0;

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].Delay = i * stagger;
            }

            _totalMs = (_rows.Length - 1) * stagger + FadeMs;
            _slide = Dpi.Scale(panel, SlidePixels);
            _panel = panel;

            // Обработчик ставим до скрытия: панель перерисуется уже пустой,
            // без кадра с полным содержимым.
            _paint = OnPaint;
            panel.Paint += _paint;
            PanelScroller.Lock(panel);

            foreach (Control control in children)
            {
                control.Visible = false;
            }

            // Полоса прокрутки без содержимого смотрелась бы висящей в воздухе;
            // PanelScroller сам вернёт её в Stop().
            HideThumb(panel);

            panel.Invalidate();
            _clock.Restart();
            _timer.Start();

            Log.Debug("UI", "reveal " + panel.Name + ": строк " + _rows.Length
                + ", кадр снят за " + watch.ElapsedMilliseconds + " мс");
        }

        /// <summary>Досрочно прекратить анимацию и вернуть строки на место.</summary>
        public void Stop()
        {
            _timer.Stop();
            _clock.Reset();
            _totalMs = 0;

            Panel panel = _panel;
            Row[] rows = _rows;

            _panel = null;
            _rows = null;

            PanelScroller.Unlock(panel);

            if (panel != null && !panel.IsDisposed)
            {
                if (_paint != null)
                {
                    panel.Paint -= _paint;
                }

                _paint = null;

                if (rows != null)
                {
                    foreach (Row row in rows)
                    {
                        foreach (Control control in row.Controls)
                        {
                            if (!control.IsDisposed)
                            {
                                control.Visible = true;
                            }
                        }
                    }
                }

                // Полосу прокрутки пересчитываем после возврата строк:
                // пока они были скрыты, высота содержимого считалась нулевой.
                PanelScroller.Refresh(panel);
                panel.Invalidate();
            }

            if (_snapshot != null)
            {
                _snapshot.Dispose();
                _snapshot = null;
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Tick -= OnTick;
            _timer.Dispose();
            _attributes.Dispose();
        }

        private static Bitmap Capture(Panel panel)
        {
            try
            {
                var snapshot = new Bitmap(panel.Width, panel.Height, PixelFormat.Format32bppPArgb);

                // DrawToBitmap рисует в bitmap с её собственным DPI. Без этого
                // при масштабе 125–200 % текст на кадре был бы мельче, чем на
                // экране, и в конце анимации строки «прыгали» бы в размере.
                using (Graphics graphics = panel.CreateGraphics())
                {
                    snapshot.SetResolution(graphics.DpiX, graphics.DpiY);
                }

                panel.DrawToBitmap(snapshot, new Rectangle(0, 0, panel.Width, panel.Height));
                return snapshot;
            }
            catch (Exception ex)
            {
                Log.Debug("UI", "не удалось снять панель для анимации: " + ex.Message);
                return null;
            }
        }

        private static bool IsChrome(Control control)
        {
            // Полоса прокрутки — не строка, её не трогаем.
            return control.Name.EndsWith("_thumb", StringComparison.Ordinal);
        }

        private static void HideThumb(Panel panel)
        {
            if (!panel.Controls.ContainsKey(panel.Name + "_thumb")) return;

            Control thumb = panel.Controls[panel.Name + "_thumb"];

            var slide = thumb as ScrollThumb;
            if (slide != null)
            {
                slide.HideNow();
                return;
            }

            thumb.Visible = false;
        }

        private static int CompareControls(Control left, Control right)
        {
            int byTop = left.Top.CompareTo(right.Top);
            return byTop != 0 ? byTop : left.Left.CompareTo(right.Left);
        }

        /// <summary>Контролы с одинаковым Top — одна строка (твик + его стрелка отката).</summary>
        private static List<Row> Group(List<Control> sorted)
        {
            var rows = new List<Row>();
            var current = new List<Control>();

            foreach (Control control in sorted)
            {
                if (current.Count > 0 && Math.Abs(control.Top - current[0].Top) > RowTolerance)
                {
                    rows.Add(new Row(current.ToArray()));
                    current = new List<Control>();
                }

                current.Add(control);
            }

            if (current.Count > 0)
            {
                rows.Add(new Row(current.ToArray()));
            }

            return rows;
        }

        private void OnTick(object sender, EventArgs e)
        {
            Panel panel = _panel;
            if (panel == null || panel.IsDisposed)
            {
                Stop();
                return;
            }

            long elapsed = _clock.ElapsedMilliseconds;
            Rectangle dirty = Rectangle.Empty;

            foreach (Row row in _rows)
            {
                if (elapsed < row.Delay) continue;

                Rectangle bounds = row.Bounds(panel);
                Rectangle shifted = row.Shifted(bounds, elapsed, _slide);
                Rectangle area = Rectangle.Union(bounds, shifted);
                dirty = dirty.IsEmpty ? area : Rectangle.Union(dirty, area);
            }

            if (!dirty.IsEmpty)
            {
                // Перерисовываем только проявившиеся строки, а не всю панель.
                panel.Invalidate(dirty, false);
            }

            if (elapsed >= _totalMs)
            {
                Stop();
            }
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            Bitmap snapshot = _snapshot;
            Row[] rows = _rows;
            Panel panel = _panel;

            if (snapshot == null || rows == null || panel == null) return;

            Graphics graphics = e.Graphics;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            graphics.SetClip(panel.ClientRectangle);

            long elapsed = _clock.ElapsedMilliseconds;

            foreach (Row row in rows)
            {
                float alpha = row.Alpha(elapsed);
                if (alpha <= 0f) continue;

                Rectangle bounds = row.Bounds(panel);
                if (bounds.Width <= 0 || bounds.Height <= 0) continue;

                Rectangle destination = row.Shifted(bounds, elapsed, _slide);
                if (destination.Width <= 0 || destination.Height <= 0) continue;

                _matrix.Matrix33 = alpha;
                _attributes.SetColorMatrix(_matrix);

                graphics.DrawImage(
                    snapshot,
                    destination,
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    bounds.Height,
                    GraphicsUnit.Pixel,
                    _attributes);
            }
        }

        private sealed class Row
        {
            public Row(Control[] controls)
            {
                Controls = controls;
            }

            public Control[] Controls { get; private set; }

            public int Delay { get; set; }

            /// <summary>Текущие границы строки: прокрутка сдвигает контролы — строки едут вместе с ними.</summary>
            public Rectangle Bounds(Panel panel)
            {
                Rectangle bounds = Rectangle.Empty;

                foreach (Control control in Controls)
                {
                    bounds = bounds.IsEmpty ? control.Bounds : Rectangle.Union(bounds, control.Bounds);
                }

                if (bounds.IsEmpty) return bounds;

                bounds.Inflate(1, 1);
                return Rectangle.Intersect(bounds, panel.ClientRectangle);
            }

            public float Alpha(long elapsed)
            {
                float progress = Progress(elapsed);
                float inverse = 1f - progress;
                return 1f - inverse * inverse;
            }

            /// <summary>
            /// Строка выезжает слева: снимок панели сдвигается вправо на
            /// несколько пикселей и встаёт на место. Градиент у панели
            /// вертикальный, поэтому сдвиг по горизонтали не даёт шва.
            /// </summary>
            public Rectangle Shifted(Rectangle bounds, long elapsed, int slide)
            {
                float progress = Progress(elapsed);
                float eased = 1f - (1f - progress) * (1f - progress);
                int shift = (int)Math.Round((1f - eased) * slide);

                if (shift == 0) return bounds;

                Rectangle shifted = bounds;
                shifted.Offset(shift, 0);
                return shifted;
            }

            private float Progress(long elapsed)
            {
                float progress = (elapsed - Delay) / (float)FadeMs;
                if (progress <= 0f) return 0f;
                return progress >= 1f ? 1f : progress;
            }
        }
    }
}
