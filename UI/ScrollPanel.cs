using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>
    /// Панель со списком твиков.
    ///
    /// Раньше панель была прозрачной: при прокрутке каждая строка
    /// перерисовывала фон формы, из-за чего оставались полосы от прежних
    /// позиций. Здесь фон рисуется сразу самой панелью — тем же градиентом,
    /// что и у формы, поэтому прокрутка идёт без артефактов.
    /// </summary>
    public sealed class ScrollPanel : Panel
    {
        private static readonly Color TopColor = Color.FromArgb(44, 57, 67);
        private static readonly Color BottomColor = Color.FromArgb(35, 44, 55);

        public ScrollPanel()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint,
                true);

            DoubleBuffered = true;
            BackColor = BottomColor;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Rectangle bounds = ClientRectangle;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            // Смещение внутри формы: градиент общий, поэтому панель рисует
            // свою долю от той же точки отсчёта.
            int offset = 0;
            int height = bounds.Height;

            Form form = FindForm();
            if (form != null)
            {
                offset = form.PointToClient(PointToScreen(Point.Empty)).Y;
                height = form.ClientSize.Height;
            }

            // Граница «шапки» — фиксированная высота, как у формы, а не
            // процент от высоты: иначе при вертикальном ресайзе она ползёт.
            float split = FormPainter.HeaderHeight > 0 && height > 0
                ? Math.Min(1f, FormPainter.HeaderHeight / height)
                : 0.106f;

            using (var brush = new LinearGradientBrush(
                new Rectangle(0, -offset, bounds.Width, height), Color.Empty, Color.Empty, 90))
            {
                brush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { TopColor, TopColor, BottomColor, BottomColor },
                    Positions = new[] { 0, split, split, 1 },
                };

                e.Graphics.FillRectangle(brush, bounds);
            }
        }
    }
}
