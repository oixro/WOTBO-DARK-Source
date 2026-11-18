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
    /// В отличие от твиков, здесь не чекбоксы с откатом, а список с
    /// отдельными кнопками — как в optimizerDuck.
    /// </summary>
    public partial class MainForm
    {
        private AppsService appsService;
        private CleanupService cleanupService;

        private CheckedListBox appsList;
        private Label appsStatus;
        private Button appsRefresh;
        private Button appsRemove;

        private CheckedListBox cleanupList;
        private Label cleanupStatus;
        private Button cleanupRefresh;
        private Button cleanupClean;

        private bool appsLoaded;
        private bool appsBusy;
        private bool cleanupBusy;

        private void SetupExtraTabs()
        {
            appsService = new AppsService(AppServices.Processes, Log.Instance);
            cleanupService = new CleanupService(Log.Instance);

            BuildAppsPanel();
            BuildCleanupPanel();

            panel_apps.VisibleChanged += delegate
            {
                if (panel_apps.Visible && !appsLoaded && !appsBusy)
                {
                    LoadAppsAsync();
                }
            };

            panel_cleanup.VisibleChanged += delegate
            {
                if (panel_cleanup.Visible && cleanupList.Items.Count == 0 && !cleanupBusy)
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
                out appsList,
                out appsStatus,
                out appsRefresh,
                out appsRemove);

            appsRefresh.Text = "Обновить";
            appsRemove.Text = "Удалить";
            appsRemove.Width = 110;

            appsRefresh.Click += delegate { LoadAppsAsync(); };
            appsRemove.Click += delegate { RemoveAppsAsync(); };
            appsList.ItemCheck += delegate
            {
                if (appsList.IsHandleCreated)
                {
                    appsList.BeginInvoke(new Action(UpdateAppsButtons));
                }
            };
        }

        private void BuildCleanupPanel()
        {
            CreateListChrome(
                panel_cleanup,
                "Глубокая очистка",
                "Занятые файлы пропускаются. Отката нет.",
                out cleanupList,
                out cleanupStatus,
                out cleanupRefresh,
                out cleanupClean);

            cleanupRefresh.Text = "Обновить";
            cleanupClean.Text = "Очистить";
            cleanupClean.Width = 110;
            cleanupClean.Enabled = false;

            cleanupRefresh.Click += delegate { ScanCleanupAsync(); };
            cleanupClean.Click += delegate { CleanSelectedAsync(); };
            cleanupList.ItemCheck += delegate
            {
                if (cleanupList.IsHandleCreated)
                {
                    cleanupList.BeginInvoke(new Action(UpdateCleanupButtons));
                }
            };
        }

        private void CreateListChrome(
            Panel panel,
            string title,
            string hint,
            out CheckedListBox list,
            out Label status,
            out Button refresh,
            out Button action)
        {
            panel.Controls.Clear();

            var titleLabel = new Label
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(6, 5),
                Text = title,
            };
            panel.Controls.Add(titleLabel);

            list = new CheckedListBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.FromArgb(35, 44, 55),
                BorderStyle = BorderStyle.None,
                CheckOnClick = true,
                Font = panel.Font,
                ForeColor = Color.White,
                FormattingEnabled = true,
                HorizontalScrollbar = true,
                IntegralHeight = false,
                Location = new Point(6, 28),
                Size = new Size(Math.Max(100, panel.ClientSize.Width - 12), Math.Max(100, panel.ClientSize.Height - 92)),
                TabIndex = 0,
            };
            panel.Controls.Add(list);

            status = new Label
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoEllipsis = true,
                BackColor = Color.Transparent,
                ForeColor = Color.Silver,
                Location = new Point(6, panel.ClientSize.Height - 58),
                Size = new Size(Math.Max(100, panel.ClientSize.Width - 12), 18),
                Text = hint,
            };
            panel.Controls.Add(status);

            refresh = CreateFlatButton(panel, "Обновить", 6, panel.ClientSize.Height - 34, 90);
            panel.Controls.Add(refresh);

            action = CreateFlatButton(panel, "Удалить", 102, panel.ClientSize.Height - 34, 90);
            panel.Controls.Add(action);
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
                Font = panel.Font,
                Size = new Size(width, 23),
                TabStop = false,
                Text = text,
                UseVisualStyleBackColor = false,
            };
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
                appsList.BeginUpdate();
                appsList.Items.Clear();

                foreach (AppPackage package in packages.OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase))
                {
                    appsList.Items.Add(package);
                }

                appsList.EndUpdate();
                appsStatus.Text = "Приложений: " + appsList.Items.Count;
            }
            catch (Exception exception)
            {
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

            var selected = appsList.CheckedItems.Cast<AppPackage>().ToList();
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
            appsList.Enabled = false;

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
            appsList.Enabled = true;
            appsRefresh.Enabled = true;
            appsList.Items.Clear();
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

        private void UpdateAppsButtons()
        {
            if (appsRemove != null)
            {
                appsRemove.Enabled = !appsBusy && appsList != null && appsList.CheckedItems.Count > 0;
            }
        }

        private async void ScanCleanupAsync()
        {
            if (cleanupBusy)
            {
                return;
            }

            cleanupBusy = true;
            cleanupRefresh.Enabled = false;
            cleanupClean.Enabled = false;
            cleanupList.Enabled = false;
            cleanupList.Items.Clear();
            cleanupStatus.Text = "Считаем размеры…";

            try
            {
                List<CleanupItem> items = cleanupService.CreateItems();
                for (int i = 0; i < items.Count; i++)
                {
                    cleanupStatus.Text = "Считаем размеры… " + (i + 1) + " из " + items.Count;
                    items[i].Size = await cleanupService.ScanAsync(items[i]);
                    cleanupList.Items.Add(items[i]);
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
                cleanupList.Enabled = true;
                UpdateCleanupButtons();
            }
        }

        private async void CleanSelectedAsync()
        {
            if (cleanupBusy)
            {
                return;
            }

            var selected = cleanupList.CheckedItems.Cast<CleanupItem>().ToList();
            if (selected.Count == 0)
            {
                return;
            }

            DialogResult answer = MessageBox.Show(
                "Очистить выбранные пункты (" + selected.Count + ")?\n\n"
                    + "Файлы будут удалены без корзины. Занятые файлы останутся на месте.",
                "WOTBO",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            cleanupBusy = true;
            cleanupRefresh.Enabled = false;
            cleanupClean.Enabled = false;
            cleanupList.Enabled = false;
            long freed = 0;

            foreach (CleanupItem item in selected)
            {
                cleanupStatus.Text = "Чистим: " + item.Name;
                freed += await cleanupService.CleanAsync(item);
            }

            cleanupBusy = false;
            cleanupList.Enabled = true;
            cleanupRefresh.Enabled = true;
            ScanCleanupAsync();

            MessageBox.Show(
                "Освобождено: " + CleanupService.FormatSize(freed) + ".",
                "WOTBO",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void UpdateCleanupButtons()
        {
            if (cleanupClean != null)
            {
                cleanupClean.Enabled = !cleanupBusy
                    && cleanupList != null
                    && cleanupList.CheckedItems.Count > 0;
            }
        }
    }
}
