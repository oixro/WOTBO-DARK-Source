using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Services.Apps;
using WOTBO.Services.Cleanup;
using WOTBO.UI;

namespace WOTBO
{
    /// <summary>
    /// Две списочные вкладки: установленные приложения и глубокая очистка.
    /// Строки — обычные чекбоксы в ScrollPanel, поэтому полоса прокрутки
    /// своя, как на остальных вкладках, а не системная белая.
    /// </summary>
    public partial class MainForm
    {
        private const int ListRowStep = 20;

        private AppsService appsService;
        private CleanupService cleanupService;

        private ScrollPanel appsContent;
        private Label appsStatus;
        private Button appsRefresh;
        private Button appsRemove;

        private ScrollPanel cleanupContent;
        private Label cleanupStatus;
        private Button cleanupRefresh;
        private Button cleanupClean;

        private bool appsLoaded;
        private bool appsBusy;
        private bool cleanupLoaded;
        private bool cleanupBusy;

        private void SetupExtraTabs()
        {
            appsService = new AppsService(AppServices.Processes, Log.Instance);
            cleanupService = new CleanupService(AppServices.Processes, Log.Instance);

            BuildAppsPanel();
            BuildCleanupPanel();
            SetupStartupTab();

            panel_apps.VisibleChanged += delegate
            {
                if (panel_apps.Visible && !appsLoaded && !appsBusy)
                {
                    LoadAppsAsync();
                }
            };

            panel_cleanup.VisibleChanged += delegate
            {
                if (panel_cleanup.Visible && !cleanupLoaded && !cleanupBusy)
                {
                    ScanCleanupAsync();
                }
            };
        }

        private void BuildAppsPanel()
        {
            CreateListChrome(
                panel_apps,
                "Установленные приложения",
                "Store/UWP-пакеты текущего пользователя. Удаление необратимо.",
                out appsContent,
                out appsStatus,
                out appsRefresh,
                out appsRemove);

            appsRemove.Text = "Удалить";
            appsRemove.Enabled = false;

            appsRefresh.Click += delegate { LoadAppsAsync(); };
            appsRemove.Click += delegate { RemoveAppsAsync(); };
        }

        private void BuildCleanupPanel()
        {
            CreateListChrome(
                panel_cleanup,
                "Глубокая очистка",
                "Занятые файлы пропускаются. Отката нет.",
                out cleanupContent,
                out cleanupStatus,
                out cleanupRefresh,
                out cleanupClean);

            cleanupClean.Text = "Очистить";
            cleanupClean.Enabled = false;

            cleanupRefresh.Click += delegate { ScanCleanupAsync(); };
            cleanupClean.Click += delegate { CleanSelectedAsync(); };
        }

        private void CreateListChrome(
            Panel panel,
            string title,
            string hint,
            out ScrollPanel content,
            out Label status,
            out Button refresh,
            out Button action,
            string actionText = "Удалить",
            bool withRefresh = true)
        {
            panel.Controls.Clear();

            var titleLabel = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(Dpi.Px(6), Dpi.Px(5)),
                Text = title,
            };
            panel.Controls.Add(titleLabel);

            content = new ScrollPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(Dpi.Px(6), Dpi.Px(28)),
                Name = panel.Name + "_content",
                Size = new Size(
                    Math.Max(100, panel.ClientSize.Width - Dpi.Px(12)),
                    Math.Max(100, panel.ClientSize.Height - Dpi.Px(92))),
                TabIndex = 0,
            };
            panel.Controls.Add(content);
            new PanelScroller(content);

            status = new Label
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                ForeColor = Color.Silver,
                Location = new Point(Dpi.Px(6), panel.ClientSize.Height - Dpi.Px(58)),
                Size = new Size(
                    Math.Max(100, panel.ClientSize.Width - Dpi.Px(12)),
                    Dpi.Px(18)),
                Text = hint,
            };
            panel.Controls.Add(status);

            refresh = null;
            if (withRefresh)
            {
                refresh = CreateFlatButton(
                    panel,
                    "Обновить",
                    Dpi.Px(6),
                    panel.ClientSize.Height - Dpi.Px(34),
                    77);
                panel.Controls.Add(refresh);
            }

            action = null;
            if (actionText != null)
            {
                action = CreateFlatButton(
                    panel,
                    actionText,
                    Dpi.Px(89),
                    panel.ClientSize.Height - Dpi.Px(34),
                    77);
                panel.Controls.Add(action);
            }
        }

        private Button CreateFlatButton(Panel panel, string text, int left, int top, int width)
        {
            return new Button
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackColor = Color.FromArgb(44, 57, 67),
                Cursor = SystemCursors.Hand,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Location = new Point(left, top),
                Size = new Size(Dpi.Px(width), Dpi.Px(23)),
                TabStop = false,
                Text = text,
                UseVisualStyleBackColor = false,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(39, 52, 59), MouseDownBackColor = Color.FromArgb(33, 46, 52) },
            };
        }

        private static void ClearRows(ScrollPanel content)
        {
            var rows = content.Controls
                .OfType<CheckBox>()
                .ToList();

            foreach (CheckBox row in rows)
            {
                content.Controls.Remove(row);
                row.Dispose();
            }
        }

        private void AddRow(ScrollPanel content, string text, string tooltip, object tag, EventHandler onChecked)
        {
            List<CheckBox> rows = content.Controls.OfType<CheckBox>().ToList();
            int step = Dpi.Px(ListRowStep);
            int top = rows.Count == 0 ? Dpi.Px(2) : rows.Max(item => item.Top) + step;

            var row = new CheckBox
            {
                AutoEllipsis = true,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
                Font = content.Font,
                ForeColor = Color.White,
                Height = Dpi.Px(17),
                Left = 0,
                Tag = tag,
                Text = text,
                Top = top,
                UseVisualStyleBackColor = true,
                Width = Math.Max(80, content.ClientSize.Width - 14),
            };

            row.CheckedChanged += onChecked;
            content.Controls.Add(row);

            if (!string.IsNullOrEmpty(tooltip))
            {
                toolTip1.SetToolTip(row, tooltip);
            }
        }

        private static List<CheckBox> CheckedRows(ScrollPanel content)
        {
            return content.Controls
                .OfType<CheckBox>()
                .Where(row => row.Checked)
                .ToList();
        }

        private void UpdateAppsButtons()
        {
            if (appsRemove != null && appsContent != null)
            {
                appsRemove.Enabled = !appsBusy && CheckedRows(appsContent).Count > 0;
            }
        }

        private void UpdateCleanupButtons()
        {
            if (cleanupClean != null && cleanupContent != null)
            {
                cleanupClean.Enabled = !cleanupBusy && CheckedRows(cleanupContent).Count > 0;
            }
        }

        private async void LoadAppsAsync()
        {
            if (appsBusy)
            {
                return;
            }

            appsBusy = true;
            appsLoaded = true;
            appsRefresh.Enabled = false;
            appsRemove.Enabled = false;
            appsStatus.Text = "Загрузка списка приложений…";

            try
            {
                List<AppPackage> packages = await appsService.GetAsync();
                ClearRows(appsContent);

                foreach (AppPackage package in packages.OrderBy(item => item.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                {
                    AddRow(
                        appsContent,
                        package.DisplayName,
                        package.Name + "\r\n" + package.FullName,
                        package,
                        delegate { UpdateAppsButtons(); });
                }

                PanelScroller.Refresh(appsContent);
                appsStatus.Text = "Приложений: " + packages.Count;
            }
            catch (Exception exception)
            {
                appsLoaded = false;
                appsStatus.Text = "Не удалось получить список";
                Log.Error("Apps", "LoadApps: " + exception.Message, exception);
            }
            finally
            {
                appsBusy = false;
                appsRefresh.Enabled = true;
                UpdateAppsButtons();
            }
        }

        private async void RemoveAppsAsync()
        {
            if (appsBusy)
            {
                return;
            }

            List<AppPackage> selected = CheckedRows(appsContent)
                .Select(row => row.Tag as AppPackage)
                .Where(item => item != null)
                .ToList();

            if (selected.Count == 0)
            {
                return;
            }

            DialogResult answer = MessageBox.Show(
                "Удалить выбранные приложения: " + selected.Count + "?\n\n"
                    + "Приложения можно снова установить из Microsoft Store, но настройки и данные "
                    + "удалённого приложения будут потеряны. Откатить это действие нельзя.\n\n"
                    + "Продолжить?",
                "WOTBO",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            appsBusy = true;
            appsRefresh.Enabled = false;
            appsRemove.Enabled = false;
            appsContent.Enabled = false;

            var failed = new List<string>();
            int done = 0;

            foreach (AppPackage package in selected)
            {
                appsStatus.Text = "Удаление " + (done + 1) + " из " + selected.Count + ": " + package.Name;
                bool ok = await appsService.RemoveAsync(package);
                if (!ok)
                {
                    failed.Add(package.Name);
                }
                else
                {
                    done++;
                }
            }

            appsBusy = false;
            appsContent.Enabled = true;
            appsRefresh.Enabled = true;
            ClearRows(appsContent);
            appsLoaded = false;
            LoadAppsAsync();

            if (failed.Count == 0)
            {
                MessageBox.Show("Удалено приложений: " + done + ".", "WOTBO");
            }
            else
            {
                MessageBox.Show(
                    "Удалено: " + done + ".\nНе удалось удалить: " + failed.Count
                        + "\n\n" + string.Join("\n", failed.Take(8))
                        + (failed.Count > 8 ? "\n…" : string.Empty),
                    "WOTBO",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async void ScanCleanupAsync()
        {
            if (cleanupBusy)
            {
                return;
            }

            cleanupLoaded = true;
            cleanupBusy = true;
            cleanupRefresh.Enabled = false;
            cleanupClean.Enabled = false;
            cleanupContent.Enabled = false;
            ClearRows(cleanupContent);
            cleanupStatus.Text = "Считаем размеры…";

            try
            {
                List<CleanupItem> items = cleanupService.CreateItems();
                for (int i = 0; i < items.Count; i++)
                {
                    cleanupStatus.Text = "Считаем размеры… " + (i + 1) + " из " + items.Count;
                    items[i].Size = await cleanupService.ScanAsync(items[i]);
                    AddRow(
                        cleanupContent,
                        items[i].ToString(),
                        items[i].Kind == CleanupKind.Dism
                            ? "DISM /StartComponentCleanup /ResetBase — необратимо, может занять 10–30 минут"
                            : (string.IsNullOrEmpty(items[i].Path) ? "Корзина" : items[i].Path),
                        items[i],
                        delegate { UpdateCleanupButtons(); });
                }

                PanelScroller.Refresh(cleanupContent);

                if (panel_cleanup.Visible && !ScreenshotMode)
                {
                    panelReveal.Play(panel_cleanup);
                }

                long total = items.Sum(item => item.Size);
                cleanupStatus.Text = "Всего можно освободить: " + CleanupService.FormatSize(total);
            }
            catch (Exception exception)
            {
                cleanupStatus.Text = "Ошибка подсчёта";
                Log.Error("Cleanup", "ScanCleanup: " + exception.Message, exception);
            }
            finally
            {
                cleanupBusy = false;
                cleanupRefresh.Enabled = true;
                cleanupContent.Enabled = true;
                UpdateCleanupButtons();
            }
        }

        private async void CleanSelectedAsync()
        {
            if (cleanupBusy)
            {
                return;
            }

            List<CleanupItem> selected = CheckedRows(cleanupContent)
                .Select(row => row.Tag as CleanupItem)
                .Where(item => item != null)
                .ToList();

            if (selected.Count == 0)
            {
                return;
            }

            string warning = selected.Any(item => item.Kind == CleanupKind.Dism)
                ? "\n\nDISM ResetBase необратим: после него нельзя удалить установленные обновления. Может занять 10–30 минут."
                : string.Empty;

            DialogResult answer = MessageBox.Show(
                "Очистить выбранные пункты (" + selected.Count + ")?\n\n"
                    + "Файлы будут удалены без корзины. Занятые файлы останутся на месте." + warning,
                "WOTBO",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            cleanupLoaded = true;
            cleanupBusy = true;
            cleanupRefresh.Enabled = false;
            cleanupClean.Enabled = false;
            cleanupContent.Enabled = false;
            long freed = 0;

            foreach (CleanupItem item in selected)
            {
                cleanupStatus.Text = "Чистим: " + item.Name;
                freed += await cleanupService.CleanAsync(item);
            }

            cleanupBusy = false;
            cleanupContent.Enabled = true;
            cleanupRefresh.Enabled = true;
            ScanCleanupAsync();

            MessageBox.Show(
                freed > 0
                    ? "Освобождено: " + CleanupService.FormatSize(freed) + "."
                    : "Очистка завершена.",
                "WOTBO",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
