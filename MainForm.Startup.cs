using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WOTBO.App.Logging;
using WOTBO.Services.Startup;
using WOTBO.UI;

namespace WOTBO
{
    /// <summary>
    /// Вкладка «Автозагрузка»: Run/RunOnce и папки автозагрузки. Строка —
    /// переключатель: клик сразу пишет StartupApproved, как диспетчер задач.
    /// </summary>
    public partial class MainForm
    {
        private StartupService startupService;
        private ScrollPanel startupContent;
        private Label startupStatus;
        private Button startupRefresh;

        private bool startupLoaded;
        private bool startupBusy;
        private bool startupSuppressEvents;

        private void SetupStartupTab()
        {
            startupService = new StartupService(Log.Instance);

            Button unused;
            CreateListChrome(
                panel_startup,
                "Автозагрузка",
                "Выключение не удаляет запись — её можно вернуть.",
                out startupContent,
                out startupStatus,
                out startupRefresh,
                out unused,
                null);

            startupRefresh.Click += delegate { LoadStartupAsync(); };

            panel_startup.VisibleChanged += delegate
            {
                if (panel_startup.Visible && !startupLoaded && !startupBusy)
                {
                    LoadStartupAsync();
                }
            };
        }

        private async void LoadStartupAsync()
        {
            if (startupBusy)
            {
                return;
            }

            startupBusy = true;
            startupLoaded = true;
            startupRefresh.Enabled = false;
            startupStatus.Text = "Читаем автозагрузку…";

            try
            {
                List<StartupEntry> entries = await startupService.GetAsync();
                ClearRows(startupContent);
                startupSuppressEvents = true;

                foreach (StartupEntry entry in entries)
                {
                    AddStartupRow(entry);
                }

                startupSuppressEvents = false;
                PanelScroller.Refresh(startupContent);
                UpdateStartupStatus();

                if (panel_startup.Visible && !ScreenshotMode)
                {
                    panelReveal.Play(panel_startup);
                }
            }
            catch (Exception exception)
            {
                startupStatus.Text = "Не удалось прочитать автозагрузку";
                Log.Error("Startup", "LoadStartup: " + exception.Message, exception);
            }
            finally
            {
                startupBusy = false;
                startupRefresh.Enabled = true;
            }
        }

        private void AddStartupRow(StartupEntry entry)
        {
            List<CheckBox> rows = startupContent.Controls.OfType<CheckBox>().ToList();
            int step = Dpi.Px(20);
            int top = rows.Count == 0 ? Dpi.Px(2) : rows.Max(item => item.Top) + step;

            var row = new CheckBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = Color.Transparent,
                Checked = entry.Enabled,
                Font = startupContent.Font,
                ForeColor = Color.White,
                Height = Dpi.Px(17),
                Left = 0,
                Tag = entry,
                Text = entry.Name,
                Top = top,
                UseVisualStyleBackColor = true,
                Width = Math.Max(80, startupContent.ClientSize.Width - 14),
            };

            toolTip1.SetToolTip(row, entry.Command + "\r\n" + entry.Location);
            row.CheckedChanged += StartupRowChecked;
            startupContent.Controls.Add(row);
        }

        private async void StartupRowChecked(object sender, EventArgs e)
        {
            if (startupSuppressEvents)
            {
                return;
            }

            var row = sender as CheckBox;
            var entry = row == null ? null : row.Tag as StartupEntry;
            if (entry == null)
            {
                return;
            }

            bool wanted = row.Checked;
            row.Enabled = false;

            bool ok = await startupService.SetEnabledAsync(entry, wanted);
            row.Enabled = true;

            if (!ok)
            {
                startupSuppressEvents = true;
                row.Checked = !wanted;
                startupSuppressEvents = false;
                startupStatus.Text = "Не удалось изменить: " + entry.Name;
                return;
            }

            entry.Enabled = wanted;
            UpdateStartupStatus();
        }

        private void UpdateStartupStatus()
        {
            List<CheckBox> rows = startupContent.Controls.OfType<CheckBox>().ToList();
            int enabled = rows.Count(row => row.Checked);
            startupStatus.Text = "Записей: " + rows.Count + ", включено: " + enabled;
        }
    }
}
