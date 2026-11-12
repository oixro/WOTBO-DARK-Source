using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>
    /// Показ панели с прежней анимацией WOTBO: скрыть остальные панели,
    /// поставить новую в (405,36) и сдвинуть к (105,36).
    /// </summary>
    public sealed class PanelNavigator
    {
        private const int BaseStartX = 405;
        private const int BaseEndX = 105;
        private const int BaseTop = 36;
        private const int DurationMs = 125;
        private const int Steps = 40;

        private readonly Form _form;
        private readonly Panel _main;
        private readonly Panel _head;
        private int _generation;

        public PanelNavigator(Form form, Panel main, Panel head)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            _form = form;
            _main = main;
            _head = head;
            AnimationEnabled = () => true;
        }

        /// <summary>Включает или выключает плавное перемещение панели.</summary>
        public Func<bool> AnimationEnabled { get; set; }

        public async Task NavigateAsync(Panel target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            int generation = ++_generation;

            foreach (Control control in _form.Controls)
            {
                Panel panel = control as Panel;
                if (panel != null && panel != _main && panel != _head)
                {
                    panel.Visible = false;
                }
            }

            int startX = Dpi.Scale(_form, BaseStartX);
            int endX = Dpi.Scale(_form, BaseEndX);
            int top = Dpi.Scale(_form, BaseTop);

            target.Location = new Point(startX, top);
            target.Visible = true;
            target.BringToFront();

            int delayPerStep = DurationMs / Steps;
            bool animate = AnimationEnabled();

            for (int i = 0; i <= Steps; i++)
            {
                if (generation != _generation)
                {
                    return;
                }

                if (animate)
                {
                    float t = (float)i / Steps;
                    float easedT = t < 0.5f
                        ? 4f * t * t * t
                        : (t - 1) * (2f * t - 2) * (2f * t - 2) + 1f;
                    int currentX = (int)(startX + (endX - startX) * easedT);
                    target.Location = new Point(currentX, target.Location.Y);
                    await Task.Delay(delayPerStep);
                }
                else
                {
                    target.Location = new Point(endX, top);
                }
            }

            target.BringToFront();
        }
    }
}
