using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Properties;
using WOTBO.Services.Defender;
using WOTBO.Services.Hardware;
using WOTBO.Services.Network;
using WOTBO.Services.Registry;
using WOTBO.Services.Tweaks;
using WOTBO.Services.Windows;
using WOTBO.UI;
namespace WOTBO
{
    public partial class MainForm
    {
        #region открытие
        private void SetupTweakTooltipsToggle()
        {
            back_ui_tooltips = new PictureBox
            {
                Name = "back_ui_tooltips",
                Location = new Point(Dpi.Px(0), Dpi.Px(50)),
                Size = new Size(Dpi.Px(15), Dpi.Px(15)),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackgroundImage = back_animations.BackgroundImage,
                BackgroundImageLayout = ImageLayout.Stretch,
                TabStop = false,
                Cursor = SystemCursors.Hand,
                Visible = false,
            };
            back_ui_tooltips.Click += back_ui_tooltips_Click;

            checkBox_tweak_tooltips = new CheckBox
            {
                Name = "checkBox_tweak_tooltips",
                Text = "Показывать подсказки о применённых твиках",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(Dpi.Px(20), Dpi.Px(50)),
                Font = panel_ui_1.Font,
                TabIndex = 100,
                UseVisualStyleBackColor = true,
            };
            checkBox_tweak_tooltips.CheckedChanged += CheckBox_CheckedChanged;

            panel_settings.Controls.Add(back_ui_tooltips);
            panel_settings.Controls.Add(checkBox_tweak_tooltips);

            SetupTooltipsToggle();

            bool enabled = AppSettings.TweakTooltipsEnabled;
            bool applied = AppServices.Tweaks.IsApplied("ui.tweakTooltips");

            // Приводим состояние в порядок, если настройка осталась от
            // прежней версии или state потерян.
            if (enabled && !applied)
            {
                AppServices.Tweaks.MarkApplied("ui.tweakTooltips", null);
                applied = true;
            }
            else if (applied && !enabled)
            {
                AppSettings.TweakTooltipsEnabled = true;
                enabled = true;
            }

            TweakHoverInfo.SetEnabled(enabled);
            back_ui_tooltips.Visible = applied;
            checkBox_tweak_tooltips.Enabled = !applied;
            checkBox_tweak_tooltips.Checked = false;
        }

        /// <summary>
        /// Твик «Отключить описание твика при наведении»: гасит toolTip1.
        /// Работает как остальные твики — через «Применить», со стрелкой отката.
        /// </summary>
        private void SetupTooltipsToggle()
        {
            back_ui_tooltips_desc = new PictureBox
            {
                Name = "back_ui_tooltips_desc",
                Location = new Point(Dpi.Px(0), Dpi.Px(70)),
                Size = new Size(Dpi.Px(15), Dpi.Px(15)),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackgroundImage = back_animations.BackgroundImage,
                BackgroundImageLayout = ImageLayout.Stretch,
                TabStop = false,
                Cursor = SystemCursors.Hand,
                Visible = false,
            };
            back_ui_tooltips_desc.Click += delegate { RollbackTooltipsDescription(); };

            checkBox_tooltips = new CheckBox
            {
                Name = "checkBox_tooltips",
                Text = "Отключить описание твика при наведении",
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(Dpi.Px(20), Dpi.Px(70)),
                Font = panel_ui_1.Font,
                TabIndex = 101,
                UseVisualStyleBackColor = true,
            };
            checkBox_tooltips.CheckedChanged += CheckBox_CheckedChanged;

            panel_settings.Controls.Add(back_ui_tooltips_desc);
            panel_settings.Controls.Add(checkBox_tooltips);

            bool hidden = !AppSettings.TooltipsEnabled;
            bool applied = AppServices.Tweaks.IsApplied("ui.tooltips");

            // Настройка и состояние могут разойтись (старая версия, ручная
            // правка реестра) — приводим к состоянию твика.
            if (hidden && !applied)
            {
                AppServices.Tweaks.MarkApplied("ui.tooltips", null);
                applied = true;
            }
            else if (applied && !hidden)
            {
                AppSettings.TooltipsEnabled = false;
            }

            toolTip1.Active = !applied;
            back_ui_tooltips_desc.Visible = applied;
            checkBox_tooltips.Enabled = !applied;
            checkBox_tooltips.Checked = false;
        }

        private void ApplyTooltipsDescription()
        {
            using (TweakScope.Apply("ui.tooltips"))
            {
                AppSettings.TooltipsEnabled = false;
                toolTip1.Active = false;
            }

            checkBox_tooltips.Checked = false;
            checkBox_tooltips.Enabled = false;
            back_ui_tooltips_desc.Visible = true;
        }

        private void RollbackTooltipsDescription()
        {
            using (TweakScope.Rollback("ui.tooltips"))
            {
                AppSettings.TooltipsEnabled = true;
                toolTip1.Active = true;
                checkBox_tooltips.Checked = false;
                checkBox_tooltips.Enabled = true;
                back_ui_tooltips_desc.Visible = false;
            }
        }

        private void ApplyTweakTooltips()
        {
            using (TweakScope.Apply("ui.tweakTooltips"))
            {
                AppSettings.TweakTooltipsEnabled = true;
                TweakHoverInfo.SetEnabled(true);
            }

            checkBox_tweak_tooltips.Checked = false;
            checkBox_tweak_tooltips.Enabled = false;
            back_ui_tooltips.Visible = true;
        }

        private void RollbackTweakTooltips()
        {
            using (TweakScope.Rollback("ui.tweakTooltips"))
            {
                AppSettings.TweakTooltipsEnabled = false;
                TweakHoverInfo.SetEnabled(false);
                checkBox_tweak_tooltips.Checked = false;
                checkBox_tweak_tooltips.Enabled = true;
                back_ui_tooltips.Visible = false;
            }
        }

        async void MainForm_Load(object sender, EventArgs e)
        {
            Log.Info("App", $"==============WOTBO {version} ==============");
            ApplyWindowSize();
            CenterToScreen();

            // Панели, которые создаются кодом, строим после финального
            // автоскейла: иначе при 125–200 % их размеры считаются от 96 DPI.
            SetupSettingsPanel();
            SetupExtraTabs();
            LayoutForSize();

            Log.Debug("UI", $"dpi aware={NativeMethods.DpiAwareness()}, dpi={DeviceDpi}, screen={Screen.FromControl(this).Bounds}, client={ClientSize.Width}x{ClientSize.Height}, autoscale={AutoScaleDimensions}");
            SetupTweakTooltipsToggle();
            TweakHoverInfo.Attach(this);

            // Строки новых твиков создаём после того, как окно получило
            // финальный размер и WinForms один раз пересчитал дизайнерские
            // координаты. Иначе новые строки остаются в 96-DPI, а старые
            // уезжают вместе с автоскейлом.
            SetupSimpleTweaks();
            foreach (Panel panel in new[] { panel1, panel_ui_1, panel2, panel_dop })
            {
                PanelScroller.Refresh(panel);
            }
            #region проверка запущенна ли + eula
            AppServices.Startup.EnsureSingleInstance(exename, exepath);
            if (!AppServices.Startup.IsEulaAccepted())
            {
                Log.Info("App", "eula null");
                EulaForm eula = new EulaForm();
                eula.ShowDialog();

                // Закрыли окно, не приняв условия (Alt+F4) — выходим,
                // иначе главное окно откроется без согласия.
                if (!AppServices.Startup.IsEulaAccepted())
                {
                    Log.Info("App", "EULA не принята — выход");
                    Close();
                    return;
                }
            }
            AppServices.Startup.MigrateState();
            #endregion
            #region проверка версии Windows
            WindowsVersionInfo winVersion = AppServices.WindowsVersion.Detect();

            if (winVersion.IsKnown && winVersion.IsBuildValid)
            {
                win10 = winVersion.IsWindows10;

                if (winVersion.IsWindows11)
                {
                    Log.Info("Windows", $"Windows is win 10? {win10}");
                    label_winver.Text = $"Windows 11 {winVersion.DisplayVersion} Build:{winVersion.Build}";
                }
                else if (winVersion.IsOldWindows10)
                {
                    Log.Info("Windows", $"Windows 10 <20H2 ");
                    MessageBox.Show("Рекомендуется использовать Windows 10 20H2 и выше! \nЭффективность настройки не гарантирована",
                        "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    label_winver.Text = $"(Old) Windows 10 {winVersion.DisplayVersion} Build:{winVersion.Build}";
                }
                else
                {
                    Log.Info("Windows", $"Windows 10 {winVersion.Build} ");
                    label_winver.Text = $"Windows 10 {winVersion.DisplayVersion} Build:{winVersion.Build}";
                }
            }
            else
            {
                MessageBox.Show("Не удалось получить информацию о версии Windows.", "Windows optimization tool by oixro (WOTBO)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            if (win10)
            {
                // Твики, которые на Windows 10 ничего не делают: классическое
                // контекстное меню и «шапка» проводника есть только в Win11.
                // Mica (ExplorerBlurMica) работает и на десятке, её не трогаем.
                checkBox_contex.Enabled = false;
                checkBox_pro_1.Enabled = false;
                checkBox_shapka.Enabled = false;
                Log.Info("Windows", $"чеки под win 11 для win10 отключены");
            }
            #endregion
            #region temp
            var workspace = AppServices.Workspace;
            workspace.Prepare();
            tempfolder = workspace.Root;
            path_regpack7z = workspace.RegpackZip;
            path_regpack = workspace.Regpack;
            path_services = workspace.Services;
            path_uizip = workspace.UiZip;
            path_ui = workspace.Ui;
            path_scheme = workspace.Scheme;

            AppServices.ResourceExtractor.Extract(workspace);
            #endregion
            // Detect-ы выполняем до показа окна: иначе чекбоксы и стрелки
            // появляются уже на глазах и окно «дёргается».
            DetectAllTweaks();

            #region инет + обнова
            // Сеть и GitHub не должны задерживать показ окна.
            _ = CheckForUpdatesAsync();
            #endregion
            #region позиция, размеры и плавный запуск
            if (SystemInformation.UserInteractive)
            {
                for (Opacity = 0; Opacity < 1; Opacity += .1) await Task.Delay(7);
            }
            else
            {
                // В неинтерактивной сессии Opacity бросает Win32Exception.
                Opacity = 1;
            }

            Log.Info("UI", $"плавно открылись");
            #endregion
            BeginInvoke(new Action(DetectHeavyTweaks));
            DetectGpuTweaks();
        }

        /// <summary>
        /// Все Detect-ы твиков. Выполняются после первого кадра, чтобы окно
        /// появлялось сразу; в логе видно, сколько занял каждый тяжёлый твик.
        /// </summary>
        private void DetectAllTweaks()
        {
            #region отключаем чеки
            var detectWatch = Stopwatch.StartNew();

            Binder("main.regpack").Detect();//regpack
            UpdateReservedStorageState();
            Binder("main.hibernate").Detect();
            Binder("ui.animations").Detect();//анимации
            #region mousefix
            void UpdateMouseFixCheckBox()
            {
                if (IsMouseFixKeyPresent())
                {
                    // Если ключ "mousefix" существует - отключаем чекбокс
                    checkBox_mousefix.Enabled = false;
                    back_main_5.Visible = true;
                    Log.Info("Tweak:main.mousefix", "Чекбокс отключен: параметр mousefix существует в реестре.");
                }
                else if (IsScale100Percent())
                {
                    // Если ключ "mousefix" не существует и масштаб равен 100% - включаем чекбокс
                    checkBox_mousefix.Enabled = true;
                    Log.Info("Tweak:main.mousefix", "Чекбокс включен: параметр mousefix не существует, и масштаб равен 100%.");
                }
                else
                {
                    // Если ключ "mousefix" не существует и масштаб не равен 100% - отключаем чекбокс
                    checkBox_mousefix.Enabled = false;
                    Log.Info("Tweak:main.mousefix", "Чекбокс отключен: параметр mousefix не существует, и масштаб не равен 100%.");
                }
            }
            bool IsScale100Percent()
            {
                using (Graphics g = this.CreateGraphics())
                {
                    float dpiX = g.DpiX;

                    // Рассчитываем масштаб в процентах
                    float scaleX = dpiX / 96 * 100;

                    // Возвращаем true, если масштаб равен 100%
                    return Math.Abs(scaleX - 100) < 0.01;
                }
            }
            bool IsMouseFixKeyPresent()
            {
                bool applied = AppServices.TweakCatalog.Get("main.mousefix").Detect(AppServices.TweakContext) == TweakState.Applied;
                AppServices.Tweaks.Reconcile("main.mousefix", applied);
                return applied;
            }
            UpdateMouseFixCheckBox();
            #endregion
            Binder("main.mpo").Detect();
            Binder("main.usb").Detect();
            AppServices.LegacyCleanup.Cleanup();
            Binder("main.mitigations").Detect();
            // WMI PS_MMAgent и netsh bufferbloat — отдельным проходом после fade.
            #region pagefile
            TweakState pagefileState = AppServices.TweakCatalog.Get("main.pagefile").Detect(AppServices.TweakContext);
            AppServices.Tweaks.Reconcile("main.pagefile", pagefileState == TweakState.Applied);
            if (pagefileState == TweakState.Applied)
            {
                Log.Info("Tweak:main.pagefile", "[pagefile check] Параметры файла подкачки уже установлены.");
                checkBox_page.Enabled = false;
                back_main_11.Visible = true;
            }
            else
            {
                Log.Info("Tweak:main.pagefile", "[pagefile check] Параметры файла подкачки отличаются от заданных или не установлены.");
            }
            #endregion
            Binder("ui.buttons").Detect();
            Binder("ui.explorerdetails").Detect();
            Binder("ui.jpegquality").Detect(); //wallpapers
            Binder("ui.contextmenu").Detect(); //contex11
            try
            {
                Binder("ui.win11header").Detect();//shapka11
            }
            catch
            {

            } //shapka11
            Binder("pro.homegallery").Detect();
            Binder("pro.win32priority").Detect();
            bool defenderApplied = Binder("main.defender").Detect();
            Binder("ui.bluefolders").Detect();
            Binder("ui.wotbocontext").Detect();
            if (defenderApplied && DefenderSafeMode.IsArmed)
            {
                Log.Warn("Tweak:main.defender", "ожидает перезагрузки в безопасный режим");
                ShowNotification(
                    "Защитник",
                    "Отключение Windows Defender продолжится после перезагрузки.",
                    ToolTipIcon.Warning);
            }
            Binder("extra.onedrive").Detect();
            if (exepath == contexpath)
            {
                Log.Info("App", "запущены из конткестного");
                back_ui_8.Visible = false;
            }

            Binder("pro.unlocker").Detect();
            Binder("pro.alwaysontop").Detect();
            Binder("ui.ffmpeg").Detect();
            Binder("extra.iconcache").Detect();
            Binder("extra.mobiletraffic").Detect();
            Binder("extra.scoobe").Detect();
            Binder("extra.stickykeys").Detect();
            Binder("ui.mica").Detect();
            Binder("ui.cursors").Detect();
            Binder("ui.explorerrestart").Detect();
            Binder("extra.dwmpriority").Detect();
            Binder("pro.csrss").Detect();
            Binder("main.audio").Detect();
            Binder("main.dwminput").Detect();
            Binder("main.powerlatency").Detect();
            Binder("extra.edge").Detect();
            Binder("pro.windowsupdate").Detect();
            Binder("extra.movetemp").Detect();
            Binder("main.startupdelay").Detect();
            Binder("main.mmcss").Detect();
            Binder("main.kbqueue").Detect();
            Binder("ui.cleancontext").Detect();
            Binder("ui.shortcutsuffix").Detect();
            Binder("gpu.gamebar").Detect();
            Binder("gpu.nvecc").Detect();
            Binder("gpu.nvpower").Detect();
            Binder("gpu.nvpowerhard").Detect();
            Binder("gpu.nvtelemetry").Detect();
            Binder("extra.driversearch").Detect();
            Binder("extra.backgroundapps").Detect();
            Binder("extra.searchweb").Detect();
            Binder("extra.maintenance").Detect();
            Binder("extra.sysrestore").Detect();
            Binder("extra.nagle").Detect();
            Binder("extra.appcompat").Detect();
            Binder("extra.kernellatency").Detect();
            Binder("main.ipilatency").Detect();
            Binder("extra.ndis").Detect();
            Binder("extra.sleepstudy").Detect();
            Binder("main.vbs").Detect();
            Binder("main.searchindex").Detect();
            Binder("extra.telemetry").Detect();
            Binder("extra.widgets").Detect();

            foreach (string id in _simpleTweakIds)
            {
                Binder(id).Detect();
            }
            #endregion
            detectWatch.Stop();
            Log.Debug("UI", $"detect loop: {detectWatch.ElapsedMilliseconds} мс");
        }

        /// <summary>
        /// Тяжёлые детекты (WMI и netsh), которые не должны задерживать показ
        /// окна. Запускаются после fade, обновляют только свои чекбоксы.
        /// </summary>
        private void DetectHeavyTweaks()
        {
            Binder("main.powerscheme").DetectInBackground();
            Binder("main.mmagent").DetectInBackground();
            Binder("pro.bufferbloat").DetectInBackground();
            Binder("extra.compactos").DetectInBackground();
        }

        /// <summary>GPU-детекты и WMI-запрос имени GPU — тоже после первого кадра.</summary>
        private void DetectGpuTweaks()
        {
            #region получаем инфу о видеодырке
            gpu = AppServices.Gpu.GetName();
            if (AppServices.Gpu.IsNvidia(gpu))
            {
                nvidia = true;
                Log.Info("GPU", $"GPU IS NVIDIA? - {nvidia}, gpu is - {gpu}");

                TweakState hdcpState = AppServices.TweakCatalog.Get("gpu.hdcp").Detect(AppServices.TweakContext);
                AppServices.Tweaks.Reconcile("gpu.hdcp", hdcpState == TweakState.Applied);
                if (hdcpState == TweakState.Applied)
                {
                    Log.Info("Tweak:gpu.hdcp", $"hdcp disabled");
                    checkBox_hdcp.Checked = false;
                    checkBox_hdcp.Enabled = false;
                    back_gpu_2.Visible = true;
                } //hdcp check

                TweakState nvidiaState = AppServices.TweakCatalog.Get("gpu.nvidia").Detect(AppServices.TweakContext);
                AppServices.Tweaks.Reconcile("gpu.nvidia", nvidiaState == TweakState.Applied);
                if (nvidiaState == TweakState.Applied)
                {
                    Log.Info("Tweak:gpu.nvidia", $"DisplayPowerSaving disabled");
                    checkBox_dopNVIDIA_tweaks.Enabled = false;
                    back_gpu_3.Visible = true;
                }//tweaksnvidia

                // Поиск NvCameraEnable.exe по DriverStore и его запуск — тяжёлые:
                // раньше это выполнялось в UI-потоке через GetResult() и на
                // машинах с NVIDIA окно могло зависнуть навсегда.
                Task.Run(delegate
                {
                    TweakState anselState;
                    var anselWatch = Stopwatch.StartNew();

                    try
                    {
                        anselState = AppServices.TweakCatalog.Get("gpu.ansel").Detect(AppServices.TweakContext);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Tweak:gpu.ansel", "detect: " + ex.Message, ex);
                        return;
                    }

                    if (anselState == TweakState.Unavailable)
                    {
                        Log.Debug("Tweak:gpu.ansel", "NvCameraEnable.exe не найден за " + anselWatch.ElapsedMilliseconds + " мс");
                        return;
                    }

                    Log.Debug("Tweak:gpu.ansel", "detect " + anselWatch.ElapsedMilliseconds + " мс");
                    AppServices.Tweaks.Reconcile("gpu.ansel", anselState == TweakState.Applied);

                    if (!IsHandleCreated || IsDisposed) return;

                    try
                    {
                        BeginInvoke(new Action(delegate
                        {
                            if (anselState == TweakState.Applied)
                            {
                                Log.Info("Tweak:gpu.ansel", "anselstate == \"0\"");
                                checkBox_ansel.Enabled = false;
                                back_gpu_4.Visible = true;
                            }
                            else
                            {
                                checkBox_ansel.Enabled = true;
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("Tweak:gpu.ansel", "не удалось обновить чекбокс: " + ex.Message);
                    }
                });
            }
            else
            {
                Log.Info("GPU", "не nvidia");
                nvidia = false;

                // Чужие строки не прячем и не двигаем: раскладка остаётся
                // стабильной, а применить их нельзя — чекбоксы выключены.
                label_inspector.Visible = false;

                // Ссылка Inspector скрыта — закрываем её строку, сдвигая
                // весь список на один шаг вверх. Иначе между «Скачать DDU»
                // и первой строкой остаётся пустая полоса.
                int step = RowStep(panel2);
                foreach (Control control in panel2.Controls)
                {
                    if (control == label_inspector || control.Name.EndsWith("_thumb", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (control.Top > label_inspector.Top)
                    {
                        control.Top -= step;
                    }
                }

                PanelScroller.Refresh(panel2);

                checkBox_ansel.Enabled = false;
                checkBox_dopNVIDIA_tweaks.Enabled = false;
                checkBox_hdcp.Enabled = false;
                checkBox_nvecc.Enabled = false;
                checkBox_nvpower.Enabled = false;
                checkBox_nvhard.Enabled = false;
                checkBox_nvtelemetry.Enabled = false;
            }//не nvidia
            #endregion
        }

        /// <summary>
        /// DISM /Get-ReservedStorageState — самый долгий одиночный вызов на
        /// старте (~190 мс). Запускается в фоне, чекбокс обновится по ответу.
        /// </summary>
        private async void UpdateReservedStorageState()
        {
            checkBox_dism.Checked = false;
            checkBox_dism.Enabled = false;
            back_main_2.Visible = false;

            try
            {
                string state = await AppServices.ReservedStorage.GetStateAsync();
                bool disabled = AppServices.ReservedStorage.IsDisabled(state);

                if (disabled)
                {
                    Log.Info("Tweak:main.reservedstorage", "ReservedStorage выключен в Windows — твик считается применённым");
                    AppServices.Tweaks.Reconcile("main.reservedstorage", true);

                    checkBox_dism.Checked = false;
                    checkBox_dism.Enabled = false;
                    back_main_2.Visible = true;
                }
                else
                {
                    Log.Info("Tweak:main.reservedstorage", "ReservedStorage включён — твик доступен");
                    AppServices.Tweaks.Reconcile("main.reservedstorage", false);

                    checkBox_dism.Checked = false;
                    checkBox_dism.Enabled = true;
                    back_main_2.Visible = false;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Tweak:main.reservedstorage", "не удалось получить состояние: " + ex.Message);
                checkBox_dism.Enabled = true;
            }
        }

        /// <summary>
        /// Проверка обновления и доступности сети. Раньше ждалась до показа
        /// окна (~700 мс), теперь выполняется после первого кадра.
        /// </summary>
        private async Task CheckForUpdatesAsync()
        {
            if (ScreenshotMode)
            {
                return;
            }

            // Уступаем кадру: DNS-проверка внутри Internet.OK() синхронная
            // и иначе задержит первый показ окна.
            await Task.Yield();

            bool online = await Task.Run(() => Internet.OK());

            if (!online)
            {
                Log.Warn("Network", "инета нету");
                checkBox_bluefolders.Enabled = false;
                label_nvcleaninstall.Enabled = false;
                label_activate.Enabled = false;
                label_ddu.Enabled = false;
                label_progs.Enabled = false;
                checkBox_mmagent.Enabled = false;
                checkBox_pro_13.Enabled = false;
                checkBox_pro_14.Enabled = false;
                checkBox_ffmpeg.Enabled = false;
                checkBox_mica.Enabled = false;
                checkBox_cursors.Enabled = false;
                label_inspector.Enabled = false;
                label_site.Enabled = false;

                MessageBox.Show(
                    "Нет доступа в интернет!\nПроверка обновлений и некоторые функции недоступны.",
                    "Windows optimization tool by oixro (WOTBO)",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                UpdateInfo update = await AppServices.UpdateCoordinator.CheckAsync(curver);
                Log.Info("Update", $"локальная {curver}, у GitHub {update.RemoteVersion}: {update.Status}");

                switch (update.Status)
                {
                    case UpdateStatus.Actual:
                        Log.Info("Update", "версия актуальная");
                        label_ver.Text = $"{version}";
                        break;

                    case UpdateStatus.Newer:
                        string changes = Shorten(update.ChangeLog, 1200);

                        // Версия пришла с зеркала — значит raw.githubusercontent.com
                        // недоступен, и скачать exe автоматически не выйдет.
                        if (update.FromMirror)
                        {
                            MessageBox.Show(
                                $"Текущая версия - {curver}\nДоступна новая - {update.RemoteVersion}\n\n"
                                + "Автоматическое обновление недоступно: основной источник заблокирован.\n"
                                + "Скачайте новую версию вручную: " + UpdateCoordinator.ManualDownloadUrl
                                + (changes.Length > 0 ? "\n\nСписок изменений:\n" + changes : string.Empty),
                                "Обновление WOTBO",
                                MessageBoxButtons.OK);
                            break;
                        }

                        MessageBox.Show(
                            $"Текущая версия - {curver}\nДоступна новая - {update.RemoteVersion}\nБудет выполнено обновление"
                            + (changes.Length > 0 ? "\n\nСписок изменений:\n" + changes : string.Empty),
                            "Обновление WOTBO",
                            MessageBoxButtons.OK);

                        try
                        {
                            await AppServices.UpdateCoordinator.InstallAsync(update, exename, exepath, contexpath);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Update", "не удалось установить обновление", ex);
                            MessageBox.Show(
                                "Не удалось скачать обновление.\nПроверьте подключение к интернету и попробуйте позже.\n\n"
                                + "Скачать вручную: " + UpdateCoordinator.ManualDownloadUrl
                                + "\n\n" + ex.Message,
                                "Обновление WOTBO",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                        }
                        break;

                    case UpdateStatus.Dev:
                        Log.Info("Update", "версия dev");
                        label_ver.Text = $"d{version}";
                        break;

                    case UpdateStatus.NoInternet:
                        Log.Warn("Update", "инета нету при проверке обновления");
                        break;

                    case UpdateStatus.Error:
                        Log.Warn("Update", "ошибка проверки обновления: " + update.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Update", "проверка обновления не удалась", ex);
            }
        }
        /// <summary>Длинный список изменений не должен раздувать окно на весь экран.</summary>
        private static string Shorten(string text, int limit)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            string trimmed = text.Trim();
            if (trimmed.Length <= limit) return trimmed;

            return trimmed.Substring(0, limit).TrimEnd() + "\n…";
        }
        #endregion
    }
}
