using System;
using System.Drawing;
using System.Windows.Forms;

namespace WOTBO.UI
{
    /// <summary>Всплывающее уведомление в трее с автоматическим скрытием.</summary>
    public sealed class BalloonNotifier : IDisposable
    {
        private const int DefaultDurationMs = 10000;

        private readonly NotifyIcon _icon;
        private Timer _timer;
        private bool _disposed;

        public BalloonNotifier()
        {
            _icon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                Visible = false,
            };
        }

        public void Show(string title, string message, ToolTipIcon icon = ToolTipIcon.None, int duration = DefaultDurationMs)
        {
            if (_disposed) return;

            _icon.BalloonTipTitle = title;
            _icon.BalloonTipText = message;
            _icon.BalloonTipIcon = icon;
            _icon.Visible = true;
            _icon.ShowBalloonTip(duration);

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            _timer = new Timer { Interval = duration + 500 };
            _timer.Tick += (sender, args) =>
            {
                _icon.Visible = false;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            };
            _timer.Start();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
