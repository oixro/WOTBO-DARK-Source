using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO.Services.Defender;

namespace WOTBO.UI
{
    /// <summary>
    /// Модальное окно прогресса на время длинных операций с Defender
    /// (переименование платформы, задачи, экспорт реестра).
    /// Показывает последнюю строку лога движка.
    /// </summary>
    internal sealed class DefenderProgressForm : Form
    {
        private readonly Label _status;
        private readonly Label _hint;
        private readonly ProgressBar _bar;
        private readonly Timer _timer;
        private bool _running;

        public DefenderProgressForm(string caption)
        {
            Text = caption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(430, 126);
            BackColor = Color.FromArgb(22, 22, 22);
            ForeColor = Color.White;
            Font = new Font("Tahoma", 8.25f, FontStyle.Regular);

            var title = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(14, 12, 402, 20),
                Text = "Windows Defender",
                Font = new Font("Tahoma", 9.75f, FontStyle.Bold),
                ForeColor = Color.White,
            };

            _status = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(14, 38, 402, 18),
                Text = "Подготовка…",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoEllipsis = true,
            };

            _bar = new ProgressBar
            {
                Bounds = new Rectangle(14, 62, 402, 14),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 28,
            };

            _hint = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(14, 86, 402, 26),
                Text = "Не выключайте компьютер и не закрывайте окно.",
                ForeColor = Color.FromArgb(150, 150, 150),
            };

            Controls.Add(title);
            Controls.Add(_status);
            Controls.Add(_bar);
            Controls.Add(_hint);

            _timer = new Timer { Interval = 700 };
            _timer.Tick += OnTick;

            FormClosing += OnFormClosing;
        }

        /// <summary>Выполнить работу в фоне и закрыть окно по завершении.</summary>
        public void Run(Action work)
        {
            Shown += delegate
            {
                _running = true;
                _timer.Start();

                Task.Run(delegate
                {
                    try
                    {
                        work();
                    }
                    finally
                    {
                        CloseFromWorker();
                    }
                });
            };
        }

        public void SetStatus(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

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

        private void OnTick(object sender, EventArgs e)
        {
            string line = DefenderEngine.ReadLastLog(1);
            if (!string.IsNullOrEmpty(line))
            {
                SetStatus(line.Trim());
            }
        }

        private void CloseFromWorker()
        {
            try
            {
                BeginInvoke(new Action(delegate
                {
                    _running = false;
                    _timer.Stop();
                    Close();
                }));
            }
            catch
            {
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_running)
            {
                e.Cancel = true;
            }
        }
    }
}
