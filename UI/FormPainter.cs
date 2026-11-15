using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>Градиентная окраска формы WOTBO.</summary>
    public static class FormPainter
    {
        /// <param name="split">
        /// Доля высоты, которую занимает верхняя полоса (шапка). По умолчанию
        /// 10.6 % — так окрашено главное окно.
        /// </param>
        public static void Apply(Form form, Color color1, Color color2, float split = 0.106f)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));

            void OnPaint(object sender, PaintEventArgs e)
            {
                if (form.ClientRectangle == Rectangle.Empty)
                {
                    return;
                }

                using (var brush = new LinearGradientBrush(form.ClientRectangle, Color.Empty, Color.Empty, 90))
                {
                    brush.InterpolationColors = new ColorBlend
                    {
                        Colors = new[] { color1, color1, color2, color2 },
                        Positions = new[] { 0, split, split, 1 },
                    };
                    e.Graphics.FillRectangle(brush, form.ClientRectangle);
                }
            }

            form.Paint -= OnPaint;
            form.Paint += OnPaint;
            form.Invalidate();
        }
    }
}
