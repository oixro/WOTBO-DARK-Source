using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>
    /// Окно загрузки файла: проценты, скачано/всего, скорость и кнопка
    /// «Отмена». Раньше DownloadFile вызывался в UI-потоке, и окно просто
    /// замирало до конца загрузки — без единого признака жизни.
    /// </summary>
    public sealed class DownloadForm : Form
    {
        private const int TickMs = 250;

        private readonly string _url;
        private readonly string _path;
        private readonly Label _percent;
        private readonly Label _size;
        private readonly Label _speed;
        private readonly ProgressBar _bar;
        private readonly Button _cancel;
        private readonly Timer _tick;

        private WebClient _client;
        private long _lastBytes;
        private DateTime _lastTime = DateTime.UtcNow;
        private double _speedMbps;
        private bool _finished;

        public DownloadForm(string caption, string url, string path)
        {
            _url = url;
            _path = path;

            Text = caption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(22, 22, 22);
            ForeColor = Color.White;
            Font = new Font("Tahoma", 8.25f, FontStyle.Regular);


            // Как в дизайнерских формах: AutoScaleMode.Font сам растягивает
            // контролы под масштаб Windows (125-200 %), а шрифт растёт в
            // пунктах. Dpi.Scale и AutoSize тут давали обрезанные окна.
            // Никакого AutoScale: размеры считаем сами через Dpi.Scale.
            // WinForms-автомасштаб в рантайм-формах даёт неверный коэффициент
            // (шрифт растёт на 1.75, а границы — на 1.15), из-за чего текст
            // обрезается.
            AutoScaleMode = AutoScaleMode.None;

            ClientSize = new Size(Dpi.Px(460), Dpi.Px(184));

            var title = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(Dpi.Px(14), Dpi.Px(12), Dpi.Px(432), Dpi.Px(20)),
                Text = caption,
                Font = new Font("Tahoma", 9.75f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true,
            };

            _bar = new ProgressBar
            {
                Bounds = new Rectangle(Dpi.Px(14), Dpi.Px(40), Dpi.Px(432), Dpi.Px(16)),
                Minimum = 0,
                Maximum = 100,
            };

            _percent = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(Dpi.Px(14), Dpi.Px(64), Dpi.Px(120), Dpi.Px(18)),
                Text = "0 %",
                ForeColor = Color.White,
            };

            _speed = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(Dpi.Px(320), Dpi.Px(64), Dpi.Px(126), Dpi.Px(18)),
                Text = "0,0 Мбит/с",
                ForeColor = Color.FromArgb(200, 200, 200),
                TextAlign = ContentAlignment.TopRight,
            };

            _size = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(Dpi.Px(14), Dpi.Px(88), Dpi.Px(432), Dpi.Px(18)),
                Text = "0,0 / 0,0 МБ",
                ForeColor = Color.FromArgb(200, 200, 200),
                TextAlign = ContentAlignment.MiddleCenter,
            };

            _cancel = new Button
            {
                Bounds = new Rectangle(Dpi.Px(346), Dpi.Px(124), Dpi.Px(100), Dpi.Px(28)),
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
            };
            _cancel.Click += delegate { CancelDownload(); };

            Controls.Add(title);
            Controls.Add(_bar);
            Controls.Add(_percent);
            Controls.Add(_speed);
            Controls.Add(_size);
            Controls.Add(_cancel);

            _tick = new Timer { Interval = TickMs };
            _tick.Tick += delegate { UpdateSpeed(); };

            Shown += delegate { StartDownload(); };
            FormClosing += OnFormClosing;
        }

        /// <summary>Ошибка загрузки, если была.</summary>
        public Exception Error { get; private set; }

        /// <summary>Пользователь нажал «Отмена».</summary>
        public bool Canceled { get; private set; }

        public void Run()
        {
            ShowDialog();
        }

        private void StartDownload()
        {
            try
            {
                string directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _client = new WebClient();
                _client.Headers[HttpRequestHeader.UserAgent] = "WOTBO";
                _client.DownloadProgressChanged += OnProgress;
                _client.DownloadFileCompleted += OnCompleted;

                _lastTime = DateTime.UtcNow;
                _lastBytes = 0;
                _tick.Start();

                _client.DownloadFileAsync(new Uri(_url), _path);
            }
            catch (Exception exception)
            {
                Error = exception;
                Finish();
            }
        }

        private void CancelDownload()
        {
            if (_finished) return;

            Canceled = true;

            try
            {
                if (_client != null) _client.CancelAsync();
            }
            catch
            {
            }

            Finish();
        }

        private void OnProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            if (_finished) return;

            int percent = e.ProgressPercentage;
            _bar.Value = percent < 0 ? 0 : (percent > 100 ? 100 : percent);
            _percent.Text = percent + " %";

            long total = e.TotalBytesToReceive;
            long received = e.BytesReceived;

            _size.Text = total > 0
                ? Mb(received) + " / " + Mb(total) + " МБ"
                : Mb(received) + " МБ";
        }

        private void UpdateSpeed()
        {
            if (_finished || _client == null) return;

            long bytes = 0;
            try
            {
                if (File.Exists(_path))
                {
                    bytes = new FileInfo(_path).Length;
                }
            }
            catch
            {
            }

            DateTime now = DateTime.UtcNow;
            double seconds = (now - _lastTime).TotalSeconds;

            if (seconds <= 0.05) return;

            double instant = (bytes - _lastBytes) * 8.0 / seconds / 1000.0 / 1000.0;
            _lastBytes = bytes;
            _lastTime = now;

            // Сглаживаем, чтобы цифра не скакала.
            _speedMbps = _speedMbps <= 0 ? instant : _speedMbps * 0.6 + instant * 0.4;

            _speed.Text = _speedMbps.ToString("0.0", CultureInfo.CurrentCulture) + " Мбит/с";
        }

        private void OnCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || Canceled)
            {
                Canceled = true;
                TryDelete();
                Finish();
                return;
            }

            if (e.Error != null)
            {
                Error = e.Error;
                TryDelete();
                Finish();
                return;
            }

            _bar.Value = 100;
            _percent.Text = "100 %";
            Finish();
        }

        private void Finish()
        {
            if (_finished) return;
            _finished = true;

            _tick.Stop();

            try
            {
                if (_client != null)
                {
                    _client.DownloadProgressChanged -= OnProgress;
                    _client.DownloadFileCompleted -= OnCompleted;
                    _client.Dispose();
                    _client = null;
                }
            }
            catch
            {
            }

            try
            {
                if (!IsDisposed) Close();
            }
            catch
            {
            }
        }

        private void TryDelete()
        {
            try
            {
                if (File.Exists(_path)) File.Delete(_path);
            }
            catch
            {
            }
        }

        private static string Mb(long bytes)
        {
            return (bytes / 1024.0 / 1024.0).ToString("0.0", CultureInfo.CurrentCulture);
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_finished)
            {
                e.Cancel = true;
                CancelDownload();
            }
        }
    }
}
