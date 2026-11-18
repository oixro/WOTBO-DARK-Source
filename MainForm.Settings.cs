using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using WOTBO.Properties;
using WOTBO.UI;

namespace WOTBO
{
    /// <summary>
    /// Настройки самой WOTBO (не Windows): анимации, подсказки. Открываются
    /// шестерёнкой в шапке, чтобы не мешаться среди твиков.
    /// </summary>
    public partial class MainForm
    {
        private ScrollPanel panel_settings;
        private PictureBox gearButton;
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

            label_settings_title = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Font = new Font(Font, FontStyle.Bold),
                Location = Dpi.Scale(this, 20, 5),
                Name = "label_settings_title",
                Text = "Настройки WOTBO",
            };
            panel_settings.Controls.Add(label_settings_title);

            MoveToSettings(checkBox_animation, 30, 20);
            MoveToSettings(FindRollback("back_animations"), 30, 0);

            gearButton = new PictureBox
            {
                BackColor = Color.Transparent,
                Cursor = SystemCursors.Hand,
                Location = Dpi.Scale(this, 6, 6),
                Name = "gearButton",
                Size = Dpi.ScaleSize(this, 22, 22),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false,
            };

            try
            {
                using (var stream = new MemoryStream(Resources.gear))
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                using (Stream iconStream = archive.GetEntry("gear.ico").Open())
                using (var icon = new Icon(iconStream, 22, 22))
                {
                    gearButton.Image = icon.ToBitmap();
                }
            }
            catch
            {
                // без иконки останется пустой квадрат — настройки всё равно откроются
            }

            gearButton.Click += delegate
            {
                ClearNavHighlight();
                ChangePanel(panel_settings);
            };

            PanelHead.Controls.Add(gearButton);
            gearButton.BringToFront();
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

            control.Location = new Point(Dpi.Scale(this, left), Dpi.Scale(this, top));
            panel_settings.Controls.Add(control);
        }

        private Control FindRollback(string name)
        {
            Control[] matches = Controls.Find(name, true);
            return matches.Length > 0 ? matches[0] : null;
        }
    }
}
