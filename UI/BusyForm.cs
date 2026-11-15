using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>
    /// Модальное окно «идёт работа»: показывает бегущую полоску и текст,
    /// а само действие выполняет в фоне. Нужно для загрузок: раньше
    /// <c>WebClient.DownloadFile</c> вызывался прямо в UI-потоке и окно
    /// замирало на всё время скачивания (или до таймаута).
    /// </summary>
    public sealed class BusyForm : Form
    {
        private readonly Label _status;
        private readonly Label _hint;
        private bool _running;
        private bool _done;

        public BusyForm(string caption, string message)
        {
            Text = caption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(22, 22, 22);
            ForeColor = Color.White;
            Font = new Font("Tahoma", 8.25f, FontStyle.Regular);


            // AutoScaleMode.Font — тот же механизм, что в дизайнерских
            // формах: контролы тянутся под масштаб, шрифт растёт сам.
            // Размеры считаем сами через Dpi.Scale: WinForms-автомасштаб
            // в рантайм-формах даёт неверный коэффициент при 125-200 %.
            AutoScaleMode = AutoScaleMode.None;

            ClientSize = new Size(Scale(420), Scale(126));

            var title = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(Scale(14), Scale(12), Scale(392), Scale(20)),
                Text = caption,
                Font = new Font("Tahoma", 9.75f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true,
            };

            _status = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(Scale(14), Scale(38), Scale(392), Scale(18)),
                Text = message,
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoEllipsis = true,
            };

            var bar = new ProgressBar
            {
                Bounds = new Rectangle(Scale(14), Scale(62), Scale(392), Scale(14)),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 28,
            };

            _hint = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(Scale(14), Scale(84), Scale(392), Scale(24)),
                Text = "Не закрывайте окно.",
                ForeColor = Color.FromArgb(150, 150, 150),
            };

            Controls.Add(title);
            Controls.Add(_status);
            Controls.Add(bar);
            Controls.Add(_hint);

            FormClosing += OnFormClosing;
        }

        private static readonly float DpiFactor = ReadDpiFactor();

        private static float ReadDpiFactor()
        {
            try
            {
                using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
                {
                    return graphics.DpiX / 96f;
                }
            }
            catch
            {
                return 1f;
            }
        }

        private static int Scale(int value)
        {
            return (int)Math.Round(value * DpiFactor);
        }
        /// <summary>Ошибка фонового действия (если была).</summary>
        public Exception Error { get; private set; }

        /// <summary>Выполнить действие в фоне и закрыть окно по завершении.</summary>
        public void Run(Action work)
        {
            Shown += delegate
            {
                _running = true;

                Task.Run(delegate
                {
                    try
                    {
                        work();
                    }
                    catch (Exception exception)
                    {
                        Error = exception;
                    }
                    finally
                    {
                        CloseFromWorker();
                    }
                });
            };

            ShowDialog();
        }

        public void SetStatus(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<string>(SetStatus), text);
                }
                catch
                {
                }

                return;
            }

            _status.Text = text;
        }

        private void CloseFromWorker()
        {
            try
            {
                BeginInvoke(new Action(delegate
                {
                    _running = false;
                    _done = true;
                    Close();
                }));
            }
            catch
            {
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_running && !_done)
            {
                e.Cancel = true;
            }
        }
    }
}
