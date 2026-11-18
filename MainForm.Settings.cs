using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

            gearButton = new PictureBox
            {
                BackColor = Color.Transparent,
                Cursor = SystemCursors.Hand,
                Location = new Point(Dpi.Px(6), Dpi.Px(6)),
                Name = "gearButton",
                Size = new Size(Dpi.Px(22), Dpi.Px(22)),
                SizeMode = PictureBoxSizeMode.Zoom,
                TabStop = false,
            };

            gearButton.Paint += PaintGear;

            gearButton.Click += delegate
            {
                ClearNavHighlight();
                ChangePanel(panel_settings);
            };

            PanelHead.Controls.Add(gearButton);
            gearButton.BringToFront();
        }

        /// <summary>Шестерёнка рисуется сама: иконка из ресурса на тёмной шапке терялась.</summary>
        private void PaintGear(object sender, PaintEventArgs e)
        {
            Control control = (Control)sender;
            Graphics graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            float cx = control.ClientSize.Width / 2f;
            float cy = control.ClientSize.Height / 2f;
            float radius = Math.Min(cx, cy) - 1f;
            float tooth = Math.Max(2f, radius * 0.32f);

            using (var pen = new Pen(Color.FromArgb(160, 173, 185), Math.Max(1.6f, radius * 0.18f)))
            {
                for (int i = 0; i < 8; i++)
                {
                    double angle = Math.PI * i / 4d;
                    float x1 = cx + (float)(Math.Cos(angle) * (radius - tooth));
                    float y1 = cy + (float)(Math.Sin(angle) * (radius - tooth));
                    float x2 = cx + (float)(Math.Cos(angle) * radius);
                    float y2 = cy + (float)(Math.Sin(angle) * radius);
                    graphics.DrawLine(pen, x1, y1, x2, y2);
                }

                float ring = radius - tooth + 0.5f;
                graphics.DrawEllipse(pen, cx - ring, cy - ring, ring * 2f, ring * 2f);
                graphics.DrawEllipse(pen, cx - radius * 0.28f, cy - radius * 0.28f, radius * 0.56f, radius * 0.56f);
            }
        }

        private void LayoutGear()
        {
            if (gearButton == null || PanelHead == null)
            {
                return;
            }

            int size = Dpi.Px(22);
            gearButton.Size = new Size(size, size);
            gearButton.Location = new Point(
                Dpi.Px(6),
                Math.Max(0, (PanelHead.ClientSize.Height - size) / 2));
            gearButton.Invalidate();
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
            panel_settings.Controls.Add(control);
        }

        private Control FindRollback(string name)
        {
            Control[] matches = Controls.Find(name, true);
            return matches.Length > 0 ? matches[0] : null;
        }
    }
}
