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
            // В этом режиме форма живёт без Application.Run: без контекста
            // синхронизации продолжения async-загрузок уходят в пул потоков
            // и падают на добавлении контролов. Ставим WinForms-контекст —
            // WaitFor ниже крутит DoEvents и доставляет их обратно в UI-поток.
            if (SynchronizationContext.Current == null
                || !(SynchronizationContext.Current is WindowsFormsSynchronizationContext))
            {
                SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
            }

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

            // Вкладки «Приложения», «Очистка» и «Автозагрузка» строятся
            // в FinishStartup: дожидаемся их, иначе снимать нечего.
            WaitFor(() => appsContent != null && cleanupContent != null && startupContent != null, 15000);

            var sections = new List<KeyValuePair<string, Panel>>();
            sections.Add(new KeyValuePair<string, Panel>("00-welcome", null));
            sections.Add(new KeyValuePair<string, Panel>("01-main", panel1));
            sections.Add(new KeyValuePair<string, Panel>("02-interface", panel_ui_1));
            sections.Add(new KeyValuePair<string, Panel>("03-gpu", panel2));
            sections.Add(new KeyValuePair<string, Panel>("04-extra", panel_dop));
            sections.Add(new KeyValuePair<string, Panel>("05-apps", panel_apps));
            sections.Add(new KeyValuePair<string, Panel>("06-cleanup", panel_cleanup));
            sections.Add(new KeyValuePair<string, Panel>("07-startup", panel_startup));
            sections.Add(new KeyValuePair<string, Panel>("08-downloads", panel_sorry));
            sections.Add(new KeyValuePair<string, Panel>("09-pc-info", panel_5));

            foreach (KeyValuePair<string, Panel> section in sections)
            {
                if (section.Value == null)
                {
                    ShowWelcome();
                }
                else
                {
                    ShowSection(section.Value);
                }

                // Списки грузятся асинхронно: ждём данные, а не фиксированные
                // 350 мс — иначе на скриншот попадёт «Загрузка…».
                if (section.Value == panel_apps)
                {
                    WaitFor(() => appsLoaded && !appsBusy, 45000);
                }
                else if (section.Value == panel_cleanup)
                {
                    WaitFor(() => cleanupLoaded && !cleanupBusy && cleanupContent.Controls.Count > 0, 90000);
                }
                else if (section.Value == panel_startup)
                {
                    WaitFor(() => !startupBusy && startupContent.Controls.Count > 0, 20000);
                }
                else if (section.Value == panel_5)
                {
                    WaitFor(() => pcInfo != null && !string.IsNullOrEmpty(pcInfo.Text), 10000);
                }

                Pump(350);

                WOTBO.App.Logging.Log.Debug(
                    "Screenshots",
                    section.Key + ": visible=" + (section.Value == null || section.Value.Visible)
                    + " bounds=" + (section.Value == null ? welcomePanel.Bounds : section.Value.Bounds)
                    + " children=" + (section.Value == null ? welcomePanel.Controls.Count : section.Value.Controls.Count)
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
                int crop = Math.Max(2, Dpi.Scale(this, 2));
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

        /// <summary>Прокручивать очередь, пока условие не выполнится или не выйдет таймаут.</summary>
        private bool WaitFor(Func<bool> ready, int timeoutMs)
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeoutMs)
            {
                if (ready())
                {
                    return true;
                }

                Application.DoEvents();
                Thread.Sleep(20);
            }

            return ready();
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
