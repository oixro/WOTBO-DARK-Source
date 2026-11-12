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
        private const int StartX = 405;
        private const int EndX = 105;
        private const int Top = 36;
        private const int DurationMs = 125;
        private const int Steps = 40;

        private readonly Form _form;
        private readonly Panel _main;
        private readonly Panel _head;

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

            foreach (Control control in _form.Controls)
            {
                Panel panel = control as Panel;
                if (panel != null && panel != _main && panel != _head)
                {
                    panel.Visible = false;
                }
            }

            target.Location = new Point(StartX, Top);
            target.Visible = true;

            int delayPerStep = DurationMs / Steps;
            bool animate = AnimationEnabled();

            for (int i = 0; i <= Steps; i++)
            {
                if (animate)
                {
                    float t = (float)i / Steps;
                    float easedT = t < 0.5f
                        ? 4f * t * t * t
                        : (t - 1) * (2f * t - 2) * (2f * t - 2) + 1f;
                    int currentX = (int)(StartX + (EndX - StartX) * easedT);
                    target.Location = new Point(currentX, target.Location.Y);
                    await Task.Delay(delayPerStep);
                }
                else
                {
                    target.Location = new Point(EndX, Top);
                }
            }
        }
    }
}
