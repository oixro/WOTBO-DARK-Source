using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using WOTBO.Services.Defender;

namespace WOTBO.UI
{
    /// <summary>
    /// Окно результата после автоматической перезагрузки
    /// (<c>WOTBO.exe --defender-result apply|restore</c>).
    ///
    /// Показывает фактические проверки. Заголовок зависит от результата:
    /// «готово» только если все обязательные проверки прошли. Для отката
    /// окно само обновляется, пока Defender не поднимется (Real-time protection
    /// включается финализацией через минуту после загрузки).
    /// </summary>
    internal sealed class DefenderResultForm : Form
    {
        private const int RowHeight = 22;
        private const int MaxRefreshTicks = 36;

        private readonly bool _restored;
        private readonly Label _title;
        private readonly Label _summary;
        private readonly List<RowView> _views = new List<RowView>();
        private readonly Timer _timer;
        private int _ticks;

        public DefenderResultForm(string mode)
        {
            _restored = string.Equals(mode, "restore", StringComparison.OrdinalIgnoreCase);

            Text = _restored ? "Восстановление Windows Defender" : "Отключение Windows Defender";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            ClientSize = new Size(480, 348);
            BackColor = Color.FromArgb(22, 22, 22);
            ForeColor = Color.White;
            Font = new Font("Tahoma", 8.25f, FontStyle.Regular);

            _title = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(16, 14, 448, 26),
                Text = "Проверка состояния…",
                Font = new Font("Tahoma", 11.25f, FontStyle.Bold),
                ForeColor = Color.White,
            };

            _summary = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(16, 42, 448, 18),
                Text = "Проверка по фактическому состоянию системы:",
                ForeColor = Color.FromArgb(165, 165, 165),
            };

            Controls.Add(_title);
            Controls.Add(_summary);

            List<CheckRow> rows = BuildRows();
            int top = 68;
            foreach (CheckRow row in rows)
            {
                var mark = new Label
                {
                    AutoSize = false,
                    Bounds = new Rectangle(16, top, 18, RowHeight),
                    Font = new Font("Tahoma", 9.75f, FontStyle.Bold),
                };

                var text = new Label
                {
                    AutoSize = false,
                    Bounds = new Rectangle(38, top, 426, RowHeight),
                    Text = row.Text,
                    AutoEllipsis = true,
                };

                Controls.Add(mark);
                Controls.Add(text);
                _views.Add(new RowView { Mark = mark, Text = text, Source = row });
                top += RowHeight;
            }

            var close = new Button
            {
                Text = "Закрыть",
                Bounds = new Rectangle(364, 304, 100, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 58, 70),
                ForeColor = Color.White,
            };
            close.FlatAppearance.BorderColor = Color.FromArgb(80, 92, 108);
            close.Click += (sender, args) => Close();
            Controls.Add(close);
            CancelButton = close;

            _timer = new Timer { Interval = 3000 };
            _timer.Tick += OnTick;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyRows(BuildRows());

            if (_restored)
            {
                _timer.Start();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            _ticks++;

            var rows = BuildRows();
            ApplyRows(rows);

            bool allPassed = true;
            foreach (CheckRow row in rows)
            {
                if (row.Critical && !row.Passed)
                {
                    allPassed = false;
                    break;
                }
            }

            if (allPassed || _ticks >= MaxRefreshTicks)
            {
                _timer.Stop();
            }
        }

        private void ApplyRows(List<CheckRow> rows)
        {
            if (rows.Count != _views.Count)
            {
                return;
            }

            int passed = 0;
            int criticalFailed = 0;

            for (int i = 0; i < rows.Count; i++)
            {
                CheckRow row = rows[i];
                RowView view = _views[i];
                view.Source = row;

                if (row.Passed)
                {
                    passed++;
                    view.Mark.Text = "✓";
                    view.Mark.ForeColor = Color.FromArgb(96, 200, 120);
                    view.Text.ForeColor = Color.White;
                }
                else if (row.Critical)
                {
                    criticalFailed++;
                    view.Mark.Text = "✕";
                    view.Mark.ForeColor = Color.FromArgb(220, 96, 96);
                    view.Text.ForeColor = Color.FromArgb(230, 170, 170);
                }
                else
                {
                    view.Mark.Text = "•";
                    view.Mark.ForeColor = Color.FromArgb(150, 150, 150);
                    view.Text.ForeColor = Color.FromArgb(190, 190, 190);
                }
            }

            _summary.Text = string.Format(
                "Проверок пройдено: {0} из {1}",
                passed,
                rows.Count);

            if (criticalFailed == 0)
            {
                _title.Text = _restored
                    ? "Windows Defender восстановлен"
                    : "Windows Defender отключён";
                _title.ForeColor = Color.FromArgb(120, 210, 140);
            }
            else
            {
                _title.Text = _restored
                    ? "Восстановление не завершено"
                    : "Отключение не завершено";
                _title.ForeColor = Color.FromArgb(230, 120, 120);
            }
        }

        private List<CheckRow> BuildRows()
        {
            var rows = new List<CheckRow>();

            DefenderProtectionState protection = DefenderStatus.GetProtection();
            bool windefend = DefenderStatus.IsServiceRunning("WinDefend");
            int windefendStart = DefenderStatus.ReadStart("WinDefend") ?? -1;
            int wdfilterStart = DefenderStatus.ReadStart("WdFilter") ?? -1;
            int wdbootStart = DefenderStatus.ReadStart("WdBoot") ?? -1;
            int securityStart = DefenderStatus.ReadStart("SecurityHealthService") ?? -1;
            int firewallStart = DefenderStatus.ReadStart("MpsSvc") ?? -1;

            bool engineRunning = false;
            try
            {
                engineRunning = Process.GetProcessesByName("MsMpEng").Length > 0;
            }
            catch
            {
            }

            bool antivirus = protection.Available && protection.AntivirusEnabled;
            bool realtime = protection.Available && protection.RealTimeProtection;

            if (_restored)
            {
                rows.Add(new CheckRow("Служба WinDefend запущена", windefend, true));
                rows.Add(new CheckRow("Движок MsMpEng работает", engineRunning, true));
                rows.Add(new CheckRow("Антивирус включён", antivirus, true));
                rows.Add(new CheckRow("Защита в реальном времени включена", realtime, true));
                rows.Add(new CheckRow("Брандмауэр Windows работает", firewallStart == 2 || firewallStart == 3, false));
            }
            else
            {
                rows.Add(new CheckRow("Служба WinDefend отключена", windefendStart == 4 && !windefend, true));
                rows.Add(new CheckRow("Драйвер WdFilter отключён", wdfilterStart == 4, true));
                rows.Add(new CheckRow("Драйвер WdBoot отключён", wdbootStart == 4, true));
                rows.Add(new CheckRow("Движок MsMpEng не запущен", !engineRunning, true));
                rows.Add(new CheckRow("Антивирус выключен", !antivirus, true));
                rows.Add(new CheckRow("Защита в реальном времени выключена", !realtime, true));
                rows.Add(new CheckRow("Служба «Безопасность Windows» отключена", securityStart == 4, true));
                rows.Add(new CheckRow("Брандмауэр Windows отключён", firewallStart == 4, true));
            }

            return rows;
        }

        private sealed class CheckRow
        {
            public CheckRow(string text, bool passed, bool critical)
            {
                Text = text;
                Passed = passed;
                Critical = critical;
            }

            public string Text { get; private set; }

            public bool Passed { get; private set; }

            /// <summary>Необязательная проверка: провал показывается точкой, не крестом.</summary>
            public bool Critical { get; private set; }
        }

        private sealed class RowView
        {
            public Label Mark { get; set; }

            public Label Text { get; set; }

            public CheckRow Source { get; set; }
        }
    }
}
