using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>Привязка кнопок и подписей к панелям без отдельных обработчиков.</summary>
    public sealed class NavigationController
    {
        private readonly Form _form;
        private readonly Action<Panel> _navigate;

        public NavigationController(Form form, Action<Panel> navigate)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
        }

        public void Bind(Control trigger, Panel panel, Control navBar = null, params Control[] visible)
        {
            if (trigger == null) throw new ArgumentNullException(nameof(trigger));
            if (panel == null) throw new ArgumentNullException(nameof(panel));

            Control[] alwaysVisible = visible ?? new Control[0];
            trigger.Click += (sender, args) =>
            {
                _navigate(panel);

                foreach (Control control in alwaysVisible)
                {
                    if (control != null) control.Visible = true;
                }

                if (navBar != null)
                {
                    navBar.Location = Dpi.Scale(_form, 195, 284);
                    navBar.Visible = true;
                }
            };
        }
    }
}
