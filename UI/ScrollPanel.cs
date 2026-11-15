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
        private static readonly Color Top = Color.FromArgb(44, 57, 67);
        private static readonly Color Bottom = Color.FromArgb(35, 44, 55);

        public ScrollPanel()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.AllPaintingInWmPaint,
                true);

            DoubleBuffered = true;
            BackColor = Bottom;
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

            using (var brush = new LinearGradientBrush(
                new Rectangle(0, -offset, bounds.Width, height), Color.Empty, Color.Empty, 90))
            {
                brush.InterpolationColors = new ColorBlend
                {
                    Colors = new[] { Top, Top, Bottom, Bottom },
                    Positions = new[] { 0, 0.106f, 0.106f, 1 },
                };

                e.Graphics.FillRectangle(brush, bounds);
            }
        }
    }
}
