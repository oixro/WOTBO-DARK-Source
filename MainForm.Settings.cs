using System;
using System.Drawing;
using System.Windows.Forms;
using WOTBO.UI;

namespace WOTBO
{
    /// <summary>
    /// Настройки самой WOTBO (не Windows): анимации, подсказки. Открываются
    /// кликом по иконке или названию в шапке.
    /// </summary>
    public partial class MainForm
    {
        private ScrollPanel panel_settings;
        private Label label_settings_title;

        private void SetupSettingsPanel()
        {
            panel_settings = new ScrollPanel
            {
                Name = "panel_settings",
                Location = panel1.Location,
                Size = panel1.Size,
                Visible = false,
            };
            Controls.Add(panel_settings);
            new PanelScroller(panel_settings);

            label_settings_title = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(Dpi.Px(20), Dpi.Px(5)),
                Name = "label_settings_title",
                Text = "Настройки WOTBO",
            };
            panel_settings.Controls.Add(label_settings_title);

            MoveToSettings(checkBox_animation, 30, 20);
            MoveToSettings(FindRollback("back_animations"), 30, 0);
        }

        private void ShowSettings()
        {
            ClearNavHighlight();
            ChangePanel(panel_settings);
        }

        /// <summary>Перенести существующий контрол на панель настроек.</summary>
        private void MoveToSettings(Control control, int top, int left)
        {
            if (control == null)
            {
                return;
            }

            Control parent = control.Parent;
            if (parent != null)
            {
                parent.Controls.Remove(control);
            }

            control.Location = new Point(Dpi.Px(left), Dpi.Px(top));

            var checkBox = control as CheckBox;
            if (checkBox != null)
            {
                checkBox.BackColor = Color.Transparent;
                checkBox.ForeColor = Color.White;
            }

            panel_settings.Controls.Add(control);
        }

        private Control FindRollback(string name)
        {
            Control[] matches = Controls.Find(name, true);
            return matches.Length > 0 ? matches[0] : null;
        }
    }
}
