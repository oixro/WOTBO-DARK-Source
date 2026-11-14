using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Services.Defender;
using WOTBO.Services.Processes;

namespace WOTBO.UI
{
    /// <summary>
    /// Окно результата, которое открывается после перезагрузки
    /// (<c>WOTBO.exe --defender-result apply|restore</c>).
    /// Показывает фактическое состояние системы, а не «команда выполнена».
    /// </summary>
    internal sealed class DefenderResultForm : Form
    {
        private const int RowHeight = 22;

        public DefenderResultForm(string mode)
        {
            bool restored = string.Equals(mode, "restore", StringComparison.OrdinalIgnoreCase);

            Text = restored ? "Восстановление Windows Defender" : "Отключение Windows Defender";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            ClientSize = new Size(470, 336);
            BackColor = Color.FromArgb(22, 22, 22);
            ForeColor = Color.White;
            Font = new Font("Tahoma", 8.25f, FontStyle.Regular);

            var title = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(16, 14, 438, 24),
                Text = restored ? "Windows Defender восстановлен" : "Windows Defender отключён",
                Font = new Font("Tahoma", 11.25f, FontStyle.Bold),
                ForeColor = Color.White,
            };

            var subtitle = new Label
            {
                AutoSize = false,
                Bounds = new Rectangle(16, 40, 438, 18),
                Text = "Проверка по фактическому состоянию системы:",
                ForeColor = Color.FromArgb(165, 165, 165),
            };

            Controls.Add(title);
            Controls.Add(subtitle);

            List<CheckRow> rows = BuildRows(restored);
            int top = 66;
            foreach (CheckRow row in rows)
            {
                var mark = new Label
                {
                    AutoSize = false,
                    Bounds = new Rectangle(16, top, 18, RowHeight),
                    Text = row.Passed ? "✓" : "✕",
                    Font = new Font("Tahoma", 9.75f, FontStyle.Bold),
                    ForeColor = row.Passed ? Color.FromArgb(96, 200, 120) : Color.FromArgb(220, 96, 96),
                };

                var text = new Label
                {
                    AutoSize = false,
                    Bounds = new Rectangle(38, top, 416, RowHeight),
                    Text = row.Text,
                    ForeColor = row.Passed ? Color.White : Color.FromArgb(230, 170, 170),
                    AutoEllipsis = true,
                };

                Controls.Add(mark);
                Controls.Add(text);
                top += RowHeight;
            }

            var ok = new Button
            {
                Text = "Закрыть",
                Bounds = new Rectangle(354, 292, 100, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 58, 70),
                ForeColor = Color.White,
                DialogResult = DialogResult.OK,
            };
            ok.FlatAppearance.BorderColor = Color.FromArgb(80, 92, 108);
            Controls.Add(ok);
            AcceptButton = ok;
        }

        private static List<CheckRow> BuildRows(bool restored)
        {
            var rows = new List<CheckRow>();
            var log = new AppLogger(AppPaths.LogFilePath);
            var processes = new ProcessService(log);

            DefenderVerification verification = DefenderStatus.Verify(processes, log, 60000);
            DefenderProtectionState protection = DefenderStatus.GetProtection();

            bool running = DefenderStatus.IsServiceRunning("WinDefend");
            int windefend = DefenderStatus.ReadStart("WinDefend") ?? -1;
            int wdfilter = DefenderStatus.ReadStart("WdFilter") ?? -1;
            int wdboot = DefenderStatus.ReadStart("WdBoot") ?? -1;
            int security = DefenderStatus.ReadStart("SecurityHealthService") ?? -1;
            int firewall = DefenderStatus.ReadStart("MpsSvc") ?? -1;
            bool mpRunning = verification != null
                && (verification.Processes ?? string.Empty).IndexOf("MsMpEng", StringComparison.OrdinalIgnoreCase) >= 0;

            if (restored)
            {
                rows.Add(new CheckRow("Служба WinDefend запущена", running));
                rows.Add(new CheckRow("Процесс MsMpEng работает", mpRunning));
                rows.Add(new CheckRow(
                    "Антивирус активен",
                    protection.Available && protection.AntivirusEnabled));
                rows.Add(new CheckRow(
                    "Защита в реальном времени включена",
                    protection.Available && protection.RealTimeProtection));
                rows.Add(new CheckRow("Брандмауэр работает", DefenderStatus.IsServiceRunning("MpsSvc")));
            }
            else
            {
                rows.Add(new CheckRow("Служба WinDefend отключена", windefend == 4 && !running));
                rows.Add(new CheckRow("Драйвер WdFilter отключён", wdfilter == 4));
                rows.Add(new CheckRow("Драйвер WdBoot отключён", wdboot == 4));
                rows.Add(new CheckRow("Процесс MsMpEng не запущен", !mpRunning));
                rows.Add(new CheckRow(
                    "Антивирус неактивен",
                    !protection.Available || !protection.AntivirusEnabled));
                rows.Add(new CheckRow(
                    "Защита в реальном времени выключена",
                    !protection.Available || !protection.RealTimeProtection));
                rows.Add(new CheckRow("Служба «Безопасность Windows» отключена", security == 4));
                rows.Add(new CheckRow("Брандмауэр Windows отключён", firewall == 4));
            }

            return rows;
        }

        private sealed class CheckRow
        {
            public CheckRow(string text, bool passed)
            {
                Text = text;
                Passed = passed;
            }

            public string Text { get; private set; }

            public bool Passed { get; private set; }
        }
    }
}
