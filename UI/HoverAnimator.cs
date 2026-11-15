using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>Плавная смена цвета подписи при наведении.</summary>
    public static class HoverAnimator
    {
        private const int DurationMs = 50;
        private const int StepDelayMs = 15;

        /// <param name="resting">
        /// Цвет, к которому возвращаться после курсора. Нужен пунктам меню:
        /// активный раздел остаётся белым, а не становится серым, как только
        /// с него убрали курсор.
        /// </param>
        public static void Attach(IEnumerable<Control> controls, Color normal, Color hover, Func<Control, Color> resting = null)
        {
            if (controls == null) return;

            foreach (Control control in controls)
            {
                if (control == null) continue;

                Control target = control;

                target.MouseHover += async (sender, args) => await FadeAsync(target, target.ForeColor, hover);
                target.MouseLeave += async (sender, args) =>
                {
                    Color back = resting != null ? resting(target) : normal;
                    await FadeAsync(target, target.ForeColor, back);
                };
            }
        }

        private static async Task FadeAsync(Control control, Color from, Color to)
        {
            int steps = DurationMs / 5;
            for (int i = 0; i <= steps; i++)
            {
                int r = from.R + (to.R - from.R) * i / steps;
                int g = from.G + (to.G - from.G) * i / steps;
                int b = from.B + (to.B - from.B) * i / steps;
                control.ForeColor = Color.FromArgb(r, g, b);
                await Task.Delay(StepDelayMs);
            }
        }
    }
}
