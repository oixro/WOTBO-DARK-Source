using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WOTBO.UI;

namespace WOTBO
{
    /// <summary>
    /// Режим <c>WOTBO.exe --export-screenshots &lt;dir&gt; [scale]</c>:
    /// открывает форму, по очереди переключает разделы и сохраняет PNG.
    /// Нужен для картинок на сайт — без кликов мышью и в любом масштабе.
    /// </summary>
    public partial class MainForm
    {
        public void ExportScreenshots(string directory, float scale)
        {
            Directory.CreateDirectory(directory);

            if (scale <= 0f)
            {
                scale = 1f;
            }

            if (Math.Abs(scale - 1f) > 0.01f)
            {
                SuspendLayout();
                Scale(new SizeF(scale, scale));
                ResumeLayout(true);
            }

            // Ставим окно в левый верхний угол, чтобы при съёме экрана
            // ничего не обрезалось краем монитора.
            StartPosition = FormStartPosition.Manual;
            Location = new Point(0, 0);

            // На картинках для сайта показываем обычную версию, без префикса dev.
            label_ver.Text = version;

            var sections = new List<KeyValuePair<string, Panel>>();
            sections.Add(new KeyValuePair<string, Panel>("01-main", panel1));
            sections.Add(new KeyValuePair<string, Panel>("03-interface", panel_ui_1));
            sections.Add(new KeyValuePair<string, Panel>("05-gpu", panel2));
            sections.Add(new KeyValuePair<string, Panel>("06-extra", panel_dop));
            sections.Add(new KeyValuePair<string, Panel>("08-downloads", panel_sorry));
            sections.Add(new KeyValuePair<string, Panel>("09-pc-info", panel_5));

            foreach (KeyValuePair<string, Panel> section in sections)
            {
                ShowSection(section.Value);
                Pump(350);

                WOTBO.App.Logging.Log.Debug(
                    "Screenshots",
                    section.Key + ": visible=" + section.Value.Visible
                    + " bounds=" + section.Value.Bounds
                    + " children=" + section.Value.Controls.Count
                    + " form=" + Width + "x" + Height
                    + " opacity=" + Opacity);

                Activate();
                BringToFront();
                Pump(120);

                // Панели прозрачные и рисуются поверх градиента формы, поэтому
                // DrawToBitmap/PrintWindow их не захватывают. Снимаем реальный
                // пиксель с экрана — так картинка совпадает с тем, что видит
                // пользователь.
                // 2 px рамки/тени окна срезаем, иначе по краям видна полоска рабочего стола.
                const int crop = 2;
                Rectangle bounds = new Rectangle(PointToScreen(Point.Empty), Size);
                bounds = new Rectangle(bounds.X + crop, bounds.Y + crop, bounds.Width - crop * 2, bounds.Height - crop * 2);
                using (var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    }

                    string file = Path.Combine(directory, section.Key + ".png");
                    bitmap.Save(file, ImageFormat.Png);
                    WOTBO.App.Logging.Log.Info("Screenshots", file + " " + bitmap.Width + "x" + bitmap.Height);
                }
            }
        }

        private const uint PwRenderFullContent = 2;

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

        /// <summary>
        /// Показать раздел без анимации: панели лежат поверх PanelMain и
        /// навигатор просто скрывает остальные и ставит нужную в (105, 36).
        /// </summary>
        private void ShowSection(Panel target)
        {
            foreach (Control control in Controls)
            {
                Panel panel = control as Panel;
                if (panel != null && panel != PanelMain && panel != PanelHead)
                {
                    panel.Visible = false;
                }
            }

            target.Location = new Point(Dpi.Scale(this, 105), Dpi.Scale(this, 36));
            target.Visible = true;
            target.BringToFront();

            // Подсветка раздела должна соответствовать снимаемой вкладке.
            HighlightNav(target);
        }

        /// <summary>Прокрутить очередь сообщений, чтобы отработали анимации.</summary>
        private static void Pump(int milliseconds)
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < milliseconds)
            {
                Application.DoEvents();
                Thread.Sleep(10);
            }
        }
    }
}
