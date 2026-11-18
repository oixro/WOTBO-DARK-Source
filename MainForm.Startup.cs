using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Services.Startup;
using WOTBO.UI;

namespace WOTBO
{
    /// <summary>
    /// Вкладка «Автозагрузка»: Run/RunOnce и папки автозагрузки. Строка —
    /// переключатель: клик сразу пишет StartupApproved, как диспетчер задач.
    /// Задачи планировщика по умолчанию не парсятся и показываются только
    /// по клику на подпись.
    /// </summary>
    public partial class MainForm
    {
        private StartupService startupService;
        private ScrollPanel startupContent;
        private Label startupStatus;
        private Label startupTasksToggle;

        private bool startupBusy;
        private bool startupSuppressEvents;
        private bool startupTasksVisible;
        private bool startupTasksBusy;
        private int startupGeneration;

        private void SetupStartupTab()
        {
            startupService = new StartupService(AppServices.Processes, Log.Instance);

            Button unused;
            Button unusedRefresh;
            CreateListChrome(
                panel_startup,
                "Автозагрузка",
                "Выключение не удаляет запись — её можно вернуть.",
                out startupContent,
                out startupStatus,
                out unusedRefresh,
                out unused,
                null,
                false);

            // Список перечитывается каждый раз при открытии вкладки:
            // отдельная кнопка «Обновить» не нужна.
            panel_startup.VisibleChanged += delegate
            {
                if (panel_startup.Visible && !startupBusy)
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

            int generation = ++startupGeneration;
            startupBusy = true;
            startupTasksVisible = false;
            startupTasksBusy = false;
            startupStatus.Text = "Читаем автозагрузку…";

            try
            {
                List<StartupEntry> apps = await startupService.GetAsync();
                if (generation != startupGeneration)
                {
                    return;
                }

                ClearStartupRows();
                int top = AddStartupEntries(apps, Dpi.Px(2));
                startupTasksToggle = AddTasksToggle(top);
                PanelScroller.Refresh(startupContent);
                UpdateStartupStatus();

                if (panel_startup.Visible && !ScreenshotMode)
                {
                    panelReveal.Play(panel_startup);
                }

                startupStatus.Text = "Читаем UWP StartupTask…";
                List<StartupEntry> uwp = await startupService.GetUwpTasksAsync();
                if (generation != startupGeneration || startupTasksToggle == null)
                {
                    return;
                }

                // UWP-строки вставляем перед подписью, подпись уезжает вниз.
                int labelTop = startupTasksToggle.Top;
                startupSuppressEvents = true;
                foreach (StartupEntry entry in uwp)
                {
                    AddStartupRow(entry, labelTop);
                    labelTop += Dpi.Px(20);
                }
                startupSuppressEvents = false;
                startupTasksToggle.Top = labelTop;

                PanelScroller.Refresh(startupContent);
                UpdateStartupStatus();
            }
            catch (Exception exception)
            {
                startupStatus.Text = "Не удалось прочитать автозагрузку";
                Log.Error("Startup", "LoadStartup: " + exception.Message, exception);
            }
            finally
            {
                startupBusy = false;
            }
        }

        private int AddStartupEntries(List<StartupEntry> entries, int top)
        {
            startupSuppressEvents = true;
            foreach (StartupEntry entry in entries)
            {
                AddStartupRow(entry, top);
                top += Dpi.Px(20);
            }
            startupSuppressEvents = false;

            return top;
        }

        private Label AddTasksToggle(int top)
        {
            var label = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = false,
                BackColor = Color.Transparent,
                Cursor = SystemCursors.Hand,
                Font = startupContent.Font,
                ForeColor = Color.Silver,
                Height = Dpi.Px(17),
                Left = 0,
                Text = "Показать задачи из планировщика задач",
                Top = top,
                Width = Math.Max(80, startupContent.ClientSize.Width - 14),
            };

            label.Click += ToggleStartupTasks;
            startupContent.Controls.Add(label);
            return label;
        }

        private async void ToggleStartupTasks(object sender, EventArgs e)
        {
            var label = sender as Label;
            if (label == null || startupBusy || startupTasksBusy || startupTasksToggle == null)
            {
                return;
            }

            if (startupTasksVisible)
            {
                startupTasksVisible = false;
                RemoveTaskRows();
                label.Text = "Показать задачи из планировщика задач";
                PanelScroller.Refresh(startupContent);
                UpdateStartupStatus();
                return;
            }

            int generation = startupGeneration;
            startupTasksBusy = true;
            label.Enabled = false;
            label.Text = "Читаем задачи…";

            try
            {
                List<StartupEntry> tasks = await startupService.GetScheduledTasksAsync();
                if (generation != startupGeneration || label.IsDisposed)
                {
                    return;
                }

                int top = label.Top + Dpi.Px(20);
                startupSuppressEvents = true;
                foreach (StartupEntry task in tasks)
                {
                    AddStartupRow(task, top);
                    top += Dpi.Px(20);
                }
                startupSuppressEvents = false;

                startupTasksVisible = true;
                label.Text = "Скрыть задачи из планировщика задач";
                label.Enabled = true;
                PanelScroller.Refresh(startupContent);
                UpdateStartupStatus();
            }
            catch (Exception exception)
            {
                label.Text = "Показать задачи из планировщика задач";
                label.Enabled = true;
                startupStatus.Text = "Не удалось прочитать задачи";
                Log.Error("Startup", "tasks: " + exception.Message, exception);
            }
            finally
            {
                startupTasksBusy = false;
            }
        }

        private void RemoveTaskRows()
        {
            List<CheckBox> tasks = startupContent.Controls
                .OfType<CheckBox>()
                .Where(row => row.Tag is StartupEntry && ((StartupEntry)row.Tag).Kind == StartupLocation.ScheduledTask)
                .ToList();

            foreach (CheckBox row in tasks)
            {
                startupContent.Controls.Remove(row);
                row.Dispose();
            }
        }

        private void ClearStartupRows()
        {
            List<Control> rows = startupContent.Controls
                .Cast<Control>()
                .Where(control => (control is CheckBox) || control == startupTasksToggle)
                .ToList();

            foreach (Control control in rows)
            {
                startupContent.Controls.Remove(control);
                control.Dispose();
            }

            startupTasksToggle = null;
        }

        private void AddStartupRow(StartupEntry entry, int top)
        {
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
                Text = RowText(entry),
                Top = top,
                UseVisualStyleBackColor = true,
                Width = Math.Max(80, startupContent.ClientSize.Width - 14),
            };

            toolTip1.SetToolTip(row, entry.Command + "\r\n" + entry.Location);
            row.CheckedChanged += StartupRowChecked;
            startupContent.Controls.Add(row);
        }

        private static string RowText(StartupEntry entry)
        {
            if (entry.Kind == StartupLocation.ScheduledTask)
            {
                return "[задача] " + entry.Name;
            }

            if (entry.Kind == StartupLocation.UwpTask)
            {
                return "[UWP] " + entry.Name;
            }

            return entry.Name;
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
