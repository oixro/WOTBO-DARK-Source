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

        public static void Attach(IEnumerable<Control> controls, Color normal, Color hover)
        {
            if (controls == null) return;

            foreach (Control control in controls)
            {
                if (control == null) continue;

                control.MouseHover += async (sender, args) => await FadeAsync(control, normal, hover);
                control.MouseLeave += async (sender, args) => await FadeAsync(control, hover, normal);
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
