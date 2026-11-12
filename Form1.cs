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
using WOTBO.Services.Hardware;
using WOTBO.Services.Network;
using WOTBO.Services.Registry;
using WOTBO.Services.Tweaks;
using WOTBO.Services.Windows;
using WOTBO.UI;
namespace Project
{
    public partial class WOTBO : FormShadow
    {
        #region переменные
        string version = $"build - {Assembly.GetExecutingAssembly().GetName().Version.ToString(2)}";
        bool win10;
        static string tempfolder = AppPaths.WorkDirectory;
        static string path_regpack7z = tempfolder + @"\regpack.zip";
        static string path_regpack = tempfolder + @"\regpack";
        static string path_services = tempfolder + @"\services";
        static string path_uizip = tempfolder + @"\ui.zip";
        static string path_ui = tempfolder + @"\ui";
        static string path_dfkiller = tempfolder + @"\DFKiller";
        static string path_scheme = tempfolder + @"\scheme.pow";
        private CheckBox checkBox_tweak_tooltips;
        private PictureBox back_ui_tooltips;
        string curver = (Assembly.GetExecutingAssembly().GetName().Version.ToString(2)).Trim();
        string exename = AppDomain.CurrentDomain.FriendlyName;
        string exepath = Assembly.GetEntryAssembly().Location;
        string contexpath = @"C:\Windows\wotbo.exe";
        public string uilanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        string logfile = AppPaths.LogFilePath;
        public static string gpu;
        public bool nvidia;
        public static long capacity;
        static string path_NVidiaProfileInspectorDmW = tempfolder + @"\NVidiaProfileInspectorDmW";
        private readonly BalloonNotifier notifier;
        private readonly PanelNavigator panelNavigator;
        private readonly Dictionary<string, TweakUiBinder> tweakBinders =
            new Dictionary<string, TweakUiBinder>(StringComparer.OrdinalIgnoreCase);

        public static class SysFolder
        {
            public static string C => AppPaths.SystemDrive;
            public static string Temp => AppPaths.Temp;
            public static string UserProfile => AppPaths.UserProfile;
            public static string Local => AppPaths.LocalAppData;
            public static string Roaming => AppPaths.RoamingAppData;
            public static string Desktop => AppPaths.Desktop;
            public static string Windows => AppPaths.Windows;
            public static string System32 => AppPaths.System32;
            public static string SysWOW64 => AppPaths.SysWOW64;
            public static string ProgramFiles => AppPaths.ProgramFiles;
            public static string ProgramFilesX86 => AppPaths.ProgramFilesX86;
            public static string ProgramData => AppPaths.ProgramData;
        }
        public bool animation = true;
        #endregion
        #region default crap
        public WOTBO()
        {
            InitializeComponent();
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            notifier = new BalloonNotifier();
            panelNavigator = new PanelNavigator(this, PanelMain, PanelHead)
            {
                AnimationEnabled = () => animation,
            };
            SetupTweakBinders();

            #region двигаем окно
            WindowDragBehavior.Attach(new List<Control> { LabelHead, PanelHead, logo });
            #endregion
            #region плавное закрытие + окраска формы
            async void Exit() { for (Opacity = 1; Opacity > .0; Opacity -= .2) await Task.Delay(7); Close(); }
            ButtonClose.Click += (s, a) => Exit();
            FormPainter.Apply(this, Color.FromArgb(44, 57, 67), Color.FromArgb(35, 44, 55)); //old
            Size = new System.Drawing.Size(390, 305);
            CenterToScreen();
            Controls.OfType<Panel>().ToList().ForEach(x => x.GetType().GetProperty("DoubleBuffered")?.SetValue(x, true));

            #endregion
            #region LoadSystemInfoAsync
            LoadSystemInfoAsync();
            async void LoadSystemInfoAsync()
            {
                try
                {
                    Task<SystemInfoSnapshot> snapshotTask = AppServices.Wmi.GetSnapshotAsync();
                    string screenInfo = AppServices.Display.GetScreenInfo();
                    DateTime? installDate = AppServices.WindowsVersion.Detect().InstallDate;
                    string installDateText = installDate.HasValue
                        ? installDate.Value.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                        : "—";

                    SystemInfoSnapshot info = await snapshotTask;

                    this.Invoke((MethodInvoker)delegate
                    {
                        label7.Text = $"CPU: {info.CpuModel.Trim()}\n" +
                                      $"CPU base clock: {info.CpuFrequencyGhz} GHz\n" +
                                      $"MB: {info.Motherboard.Trim()}\n" +
                                      $"GPU: {info.Gpu.Trim()}\n" +
                                      $"ОЗУ: {info.RamSize.Trim()} {info.RamFrequency.Trim()}\n" +
                                      $"Display: {screenInfo.Trim()}\n" +
                                      $"OS: {label_winver.Text}\n" +
                                      $"Установлена: {installDateText}";
                        Log.Info("System", label7.Text);
                    });
                }
                catch (Exception ex)
                {
                    Log.Error("System", "не удалось получить информацию о ПК", ex);
                    label7.Text = "Произошла ошибка при получении информации о пк";
                }
            }
            #endregion
            #region key binds
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyValue == (char)Keys.F12)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = $"/command chcp 65001; Get-Content -Path \"{logfile}\" -Wait -Encoding UTF8",
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        CreateNoWindow = false
                    });
                }

                if (e.KeyValue == (char)Keys.F11)
                {
                    DialogResult result = MessageBox.Show("Выбрать все твики? \nОткажись если ты не знаешь что это!", "GOD MODE", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (result == DialogResult.Yes)
                    {


                        LabelHead.Text = "BRO, TELL ME, ARE YOU INSANE?";
                        button_new.Text = "LETS GO!";
                        void ChangeLabelForeColor(Control parent, Color color)
                        {
                            foreach (Control control in parent.Controls)
                            {
                                if (control is System.Windows.Forms.Label label)
                                {
                                    label.ForeColor = color; // Замените на нужный цвет
                                }
                                else if (control is CheckBox CheckBox)
                                {
                                    CheckBox.ForeColor = color; // Замените на нужный цвет
                                }
                                else if (control is Button Button)
                                {
                                    Button.ForeColor = color; // Замените на нужный цвет
                                }
                                else if (control.HasChildren)
                                {
                                    ChangeLabelForeColor(control, color); // Рекурсивно обрабатываем вложенные элементы
                                }
                            }
                        }
                        ChangeLabelForeColor(this, Color.Red); // Замените Color.Red на ваш цвет

                        Log.Warn("UI", "INSANE MODE!");
                        //main
                        checkBox_disabledefender.Checked = true;
                        checkBox_reg.Checked = true;
                        checkBox_dism.Checked = true;
                        checkBox_gibernate.Checked = true;
                        checkBox_scheme.Checked = true;
                        checkBox_mpo.Checked = true;
                        checkBox_usb.Checked = true;
                        checkBox_mtiititit.Checked = true;
                        checkBox_mmagent.Checked = true;
                        checkBox_page.Checked = true;
                        checkBox_mousefix.Checked = true;
                        checkBox_audio.Checked = true;
                        checkBox_dwninput.Checked = true;
                        checkBox_tolerate.Checked = true;

                        //gpu
                        checkBox_directplay.Checked = true;


                        //ui
                        checkBoxUI_Buttons_1.Checked = true;
                        checkBoxUI_Buttons_2.Checked = true;
                        checkBoxUI_Buttons_4.Checked = true;
                        checkBox_wotboincontex.Checked = true;
                        checkBox_explorer.Checked = true;

                        //dop
                        checkBox_onedrive.Checked = true;
                        checkBox_compactos.Checked = true;
                        checkBox_WinSxS.Checked = true;
                        checkBox_temp.Checked = true;
                        checkBox_updclean.Checked = true;
                        checkBox_picture_cache.Checked = true;
                        checkBox_mobile_traffic.Checked = true;
                        checkBox_nastroyka.Checked = true;
                        checkBox_zalipanie.Checked = true;
                        checkBox_dwm.Checked = true;
                        checkBox_edge.Checked = true;
                        checkBox_move_temp.Checked = true;

                        //pro
                        checkBox_pro_3.Checked = true;
                        checkBox_CSRSS.Checked = true;
                        checkBox_pro_11.Checked = true;
                        checkBox_pro_13.Checked = true;


                        if (!win10)
                        {
                            checkBox_shapka.Checked = true;
                            checkBox_contex.Checked = true;
                            checkBox_pro_1.Checked = true;
                        }
                    }
                }

            };
            #endregion
            #region label animations
            HoverAnimator.Attach(
                new List<Control> { label_main, label_interface, label_gpu, label_dop, label_progs, label_pc, label_site, label_winver, LabelHead, label_ver, label7 },
                Color.FromArgb(192, 192, 192),
                Color.FromArgb(255, 255, 255));
            #endregion
        }
        #endregion
        #region TweakUiBinder
        private TweakUiBinder Bind(
            string id,
            CheckBox checkBox,
            PictureBox rollback,
            string detectMessage = null,
            Action onDetected = null,
            Action beforeApply = null,
            Action inApply = null,
            Action afterApply = null,
            Action inRollback = null,
            Action afterRollback = null,
            Func<bool> confirmApply = null,
            bool disableOnApply = true,
            bool showRollbackOnApply = true,
            bool showRollbackOnDetect = true,
            bool uncheckOnDetect = false)
        {
            var binder = new TweakUiBinder(
                id,
                checkBox,
                rollback,
                detectMessage,
                onDetected,
                beforeApply,
                inApply,
                afterApply,
                inRollback,
                afterRollback,
                confirmApply,
                disableOnApply,
                showRollbackOnApply,
                showRollbackOnDetect,
                uncheckOnDetect);

            tweakBinders[id] = binder;
            return binder;
        }

        private TweakUiBinder Binder(string id)
        {
            return tweakBinders[id];
        }

        private void SetupTweakBinders()
        {
            Bind("main.defender", checkBox_disabledefender, back_main_12,
                "defenderdisabled был применён",
                onDetected: () =>
                {
                    label_delete_defender.Enabled = true;
                    label_kalorant.Visible = true;
                },
                beforeApply: () => MessageBox.Show(
                    "Защитник будет отключен!\nЗащита от эксполитов не будет работать.\n" +
                    "Она нужна для работы некоторых античитов.\n" +
                    "Но восстановить защитник можно через вкладку дополнительно.\n",
                    "", MessageBoxButtons.OK, MessageBoxIcon.Warning),
                afterApply: () =>
                {
                    label_kalorant.Visible = true;
                    ShowNotification("Защитник", "Рекомендуется перезагрузить компьютер для полноценного отключения защитника!", ToolTipIcon.Warning);
                },
                afterRollback: () => ShowNotification("Защитник Windows", "Работа защитника будет восстановлена после перезагруки ПК", ToolTipIcon.Warning));

            Bind("main.regpack", checkBox_reg, back_main_1, "regpack был применён",
                afterApply: () => ShowNotification("Настройки Реестра", "Рекомендуется перезагрузить ПК после применения данного твика!", ToolTipIcon.Warning));
            Bind("main.reservedstorage", checkBox_dism, back_main_2);
            Bind("main.hibernate", checkBox_gibernate, back_main_3, "hiberfil'a нету");
            Bind("main.powerscheme", checkBox_scheme, back_main_4, "imported_powerscheme был применён");
            Bind("main.mpo", checkBox_mpo, back_main_6, "MPO был применён");
            Bind("main.usb", checkBox_usb, back_main_7, "usbpowersaving был применён");
            Bind("main.mitigations", checkBox_mtiititit, back_main_9, "spectre and meltdown был применён");
            Bind("main.mmagent", checkBox_mmagent, back_main_10, "mmagent был применён");
            Bind("main.pagefile", checkBox_page, back_main_11);
            Bind("main.mousefix", checkBox_mousefix, back_main_5);
            Bind("main.audio", checkBox_audio, back_main_13, "AllowHeadlessExecution BackgroundAudioPolicy был применён");
            Bind("main.dwminput", checkBox_dwninput, back_main_14, "DwmInputUsesIoCompletionPort был применён");
            Bind("main.powerlatency", checkBox_tolerate, back_main_16, "Power\\ExitLatency был применён");
            Bind("pro.win32priority", checkBox_pro_11, back_pro_11, "Win32PrioritySeparation был применён",
                inApply: () => new Win32Priority().ShowDialog());

            Bind("gpu.hdcp", checkBox_hdcp, back_gpu_2);
            Bind("gpu.nvidia", checkBox_dopNVIDIA_tweaks, back_gpu_3);
            Bind("gpu.directplay", checkBox_directplay, back_gpu_1);

            Bind("ui.buttons", checkBoxUI_Buttons_1, back_ui_1, "buttons был применён");
            Bind("ui.explorerdetails", checkBoxUI_Buttons_2, back_ui_2, "litle_explorer_things был применён");
            Bind("ui.jpegquality", checkBoxUI_Buttons_4, back_ui_4, "JPEGImportQuality был применён");
            Bind("ui.bluefolders", checkBox_bluefolders, back_ui_5, "синие папки были применены");
            Bind("ui.contextmenu", checkBox_contex, back_ui_6, "contex11 был применён");
            Bind("ui.win11header", checkBox_shapka, back_ui_7, "shapka11 был применён");
            Bind("ui.wotbocontext", checkBox_wotboincontex, back_ui_8, "wotbo in contex был применён");
            Bind("ui.explorerrestart", checkBox_explorer, back_ui_9, "restart explorer был применён");
            Bind("ui.ffmpeg", checkBox_ffmpeg, back_ui_10, "FFmpeg был применён",
                beforeApply: () => ShowNotification("FFMPEG", "Ожидайте скачивания FFMPEG. Программа не зависла!", ToolTipIcon.Info));
            Bind("ui.mica", checkBox_mica, back_ui_11, "ExplorerBlurMica был применён");
            Bind("ui.animations", checkBox_animation, back_animations, "анимации выключены",
                onDetected: () => animation = false,
                afterApply: () => animation = false,
                afterRollback: () => animation = true);

            Bind("extra.onedrive", checkBox_onedrive, null, "OneDrive был удалён",
                showRollbackOnApply: false, showRollbackOnDetect: false);
            Bind("extra.compactos", checkBox_compactos, null,
                disableOnApply: false, showRollbackOnApply: false);
            Bind("extra.winsxs", checkBox_WinSxS, null,
                disableOnApply: false, showRollbackOnApply: false);
            Bind("extra.tempclean", checkBox_temp, null,
                disableOnApply: false, showRollbackOnApply: false);
            Bind("extra.updateclean", checkBox_updclean, null,
                disableOnApply: false, showRollbackOnApply: false);
            Bind("extra.iconcache", checkBox_picture_cache, back_dop_1, "Max Cached Icons был применён");
            Bind("extra.mobiletraffic", checkBox_mobile_traffic, back_dop_2, "DefaultTTL был применён");
            Bind("extra.scoobe", checkBox_nastroyka, back_dop_3, "ScoobeSystemSettingEnabled был применён");
            Bind("extra.stickykeys", checkBox_zalipanie, back_dop_4, "StickyKeys был применён");
            Bind("extra.dwmpriority", checkBox_dwm, back_dop_6, "dwm.exe priority был применён");
            Bind("extra.edge", checkBox_edge, back_dop_edge, "Edge был применён",
                uncheckOnDetect: true);
            Bind("extra.movetemp", checkBox_move_temp, back_dop_movetemp);

            Bind("pro.homegallery", checkBox_pro_1, back_pro_1, "pro_1 был применён",
                showRollbackOnApply: false, showRollbackOnDetect: false);
            Bind("pro.windowsupdate", checkBox_pro_3, back_pro_3);
            Bind("pro.csrss", checkBox_CSRSS, back_dop_7, "csrss.exe priority был применён");
            Bind("pro.bufferbloat", checkBox_pro_12, back_pro_12, "bufferbloat был применён",
                confirmApply: () => MessageBox.Show(
                    "Bufferbloat - явление, когда чрезмерная буферизация вызывает увеличение времени прохождения пакетов (Ping) и разброса задержки пакетов (Packetloss)\n\n" +
                    "Рекомендуется использовать только если у вас нестабильное соединение!\n(Например мобильный интернет и т.д)\n" +
                    "!Твик может незначительно снизить скорость интернета!\n" +
                    "В случае если у вас стало хуже, или же вам не нужен этот твик, просто откатите его.\n" +
                    "Применить?",
                    "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
            Bind("pro.unlocker", checkBox_pro_13, back_pro_13, "unclockewr был применён");
            Bind("pro.alwaysontop", checkBox_pro_14, back_pro_14, "AlwaysOnTop был применён",
                inRollback: () => AppServices.Processes.StartCaptured("taskkill /f /im AlwaysOnTop.exe"));
        }
        #endregion
        #region my void's
        #region анимка
        async public void ChangePanel(Panel NewPanel)
        {
            await panelNavigator.NavigateAsync(NewPanel);
        }
        #endregion
        public void OpenUrl(string url)
        {
            if (!AppServices.Processes.OpenUrl(url))
            {
                MessageBox.Show($"Не удалось открыть ссылку. \nВозможно у вас не установлен браузер по умолчанию.", "WOTBO");
            }
        }

        void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.CheckBox checkBox = sender as System.Windows.Forms.CheckBox;
            if (checkBox != null)
            {
                Log.Info("UI", $"{checkBox.Text} ({checkBox.Name}) изменен на {checkBox.CheckState}");
            }
        }
        void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            // Обработчик события Click для PictureBox
            PictureBox clickedPictureBox = sender as PictureBox;
            if (clickedPictureBox != null)
            {
                // Здесь можно написать код для выполнения действия при клике на картинку
                Log.Debug("UI", $"{clickedPictureBox.Name} нажат");
            }
        }
        void Label_MouseUp(object sender, MouseEventArgs e)
        {
            // Обработчик события Click для PictureBox
            System.Windows.Forms.Label Lbl = sender as System.Windows.Forms.Label;
            if (Lbl != null)
            {
                // Здесь можно написать код для выполнения действия при клике на картинку
                Log.Debug("UI", $"Label {Lbl.Text} был нажат");
            }
        }
        void Button_MouseUp(object sender, MouseEventArgs e)
        {
            // Обработчик события Click для PictureBox
            Button btn = sender as Button;
            if (btn != null)
            {
                // Здесь можно написать код для выполнения действия при клике на картинку
                Log.Debug("UI", $"Button {btn.Text},{btn.Name} был нажат");
            }
        }

        // Универсальный метод для отображения уведомления
        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.None)
        {
            notifier.Show(title, message, icon);
        }

        #endregion
        #region открытие
        private void SetupTweakTooltipsToggle()
        {
            back_ui_tooltips = new PictureBox
            {
                Name = "back_ui_tooltips",
                Location = Dpi.Scale(this, 0, 85),
                Size = Dpi.ScaleSize(this, 15, 15),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackgroundImage = back_animations.BackgroundImage,
                BackgroundImageLayout = ImageLayout.Stretch,
                TabStop = false,
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
                Location = Dpi.Scale(this, 20, 85),
                Font = panel_ui_2.Font,
                TabIndex = 100,
                UseVisualStyleBackColor = true,
            };
            checkBox_tweak_tooltips.CheckedChanged += CheckBox_CheckedChanged;

            panel_ui_2.Controls.Add(back_ui_tooltips);
            panel_ui_2.Controls.Add(checkBox_tweak_tooltips);

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

        async void Form1_Load(object sender, EventArgs e)
        {
            Log.Info("App", $"==============WOTBO {version} ==============");
            SetupTweakTooltipsToggle();
            TweakHoverInfo.Attach(this);
            #region проверка запущенна ли + eula
            if (!InstanceGuard.TryAcquire())
            {
                Log.Warn("App", "InstanceExists, relaunch");
                AppServices.Processes.StartCaptured($"taskkill /f /im {exename} && \"{exepath}\"");
                Log.Info("App", "cmd на перезапуск вызван, ждём когда приебашит меня");
            }
            if (Registry.CurrentUser.OpenSubKey(RegistryPaths.AppRoot)?.GetValue("eula") == null)
            {
                Log.Info("App", "eula null");
                Form2 eula = new Form2();
                eula.ShowDialog();
            }
            AppServices.Tweaks.MigrateLegacyFlags();
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
            path_dfkiller = workspace.DfKiller;
            path_scheme = workspace.Scheme;
            #endregion
            #region отключаем чеки
            Binder("main.regpack").Detect();//regpack
            string reservedStorageState = await AppServices.ReservedStorage.GetStateAsync();
            if (AppServices.ReservedStorage.IsDisabled(reservedStorageState))
            {
                Log.Info("Tweak:main.reservedstorage", "ReservedStorage был применён");
                checkBox_dism.Enabled = false;
                back_main_2.Visible = true;
            } //old reserved
            Binder("main.hibernate").Detect();
            Binder("main.powerscheme").Detect();
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
                return AppServices.TweakCatalog.Get("main.mousefix").Detect(AppServices.TweakContext) == TweakState.Applied;
            }
            UpdateMouseFixCheckBox();
            #endregion
            Binder("main.mpo").Detect();
            Binder("main.usb").Detect();
            if (AppServices.Tweaks.IsApplied("main.bcdedit"))
            {
                Log.Warn("Tweak:main.bcdedit", "bcdedit был применён, откатываем нахуй");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue tscsyncpolicy");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue bootux");
                AppServices.Processes.StartCaptured("bcdedit /set bootmenupolicy standard");
                AppServices.Processes.StartCaptured("bcdedit /set hypervisorlaunchtype Auto");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue tpmbootentropy");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue quietboot");
                AppServices.Processes.StartCaptured("bcdedit /set nx optin");
                AppServices.Processes.StartCaptured("bcdedit /set allowedinmemorysettings 0x17000077");
                AppServices.Processes.StartCaptured("bcdedit /set isolatedcontext Yes");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue vsmlaunchtype");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue vm");
                AppServices.Processes.StartCaptured("reg delete \"HKLM\\Software\\Policies\\Microsoft\\FVE\" /v \"DisableExternalDMAUnderLock\" /f");
                AppServices.Processes.StartCaptured("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"EnableVirtualizationBasedSecurity\" /f");
                AppServices.Processes.StartCaptured("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"HVCIMATRequired\" /f");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue firstmegabytepolicy");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue avoidlowmemory");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue nolowmem");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue configaccesspolicy");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue x2apicpolicy");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue usephysicaldestination");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue usefirmwarepcisettings");
                AppServices.Processes.StartCaptured("bcdedit /deletevalue uselegacyapicmode");
                AppServices.Tweaks.Remove("main.bcdedit");
            }
            Binder("main.mitigations").Detect();
            Binder("main.mmagent").Detect();
            #region pagefile
            if (AppServices.TweakCatalog.Get("main.pagefile").Detect(AppServices.TweakContext) == TweakState.Applied)
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
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU")?.GetValue("NoAutoUpdate")) == 1)
            {
                Log.Info("Tweak:pro.windowsupdate", "NoAutoUpdate был применён");
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;

            }
            Binder("main.defender").Detect();
            Binder("ui.bluefolders").Detect();
            Binder("ui.wotbocontext").Detect();
            if (!Directory.Exists(@"C:\ProgramData\Microsoft\Windows Defender"))
            {
                Log.Info("Tweak:main.defender", "defender удалён");
                //checkBox_disabledefender.Visible = false;
                checkBox_disabledefender.Enabled = false;
                back_main_12.Visible = false;
            }
            Binder("extra.onedrive").Detect();
            if (exepath == contexpath)
            {
                Log.Info("App", "запущены из конткестного");
                back_ui_8.Visible = false;
            }
            Binder("pro.bufferbloat").Detect();
            Binder("pro.unlocker").Detect();
            Binder("pro.alwaysontop").Detect();
            Binder("ui.ffmpeg").Detect();
            Binder("extra.iconcache").Detect();
            Binder("extra.mobiletraffic").Detect();
            Binder("extra.scoobe").Detect();
            Binder("extra.stickykeys").Detect();
            Binder("ui.mica").Detect();
            Binder("ui.explorerrestart").Detect();
            Binder("extra.dwmpriority").Detect();
            Binder("pro.csrss").Detect();
            Binder("main.audio").Detect();
            Binder("main.dwminput").Detect();
            Binder("main.powerlatency").Detect();
            Binder("extra.edge").Detect();
            if (AppServices.TweakCatalog.Get("pro.windowsupdate").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:pro.windowsupdate", "WindowsUpdate\\AU был применён");
                checkBox_pro_3.Checked = false;
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;
            }
            Binder("extra.movetemp").Detect();
            #endregion
            #region инет + обнова
            if (!Internet.OK())
            {
                Log.Warn("Network", $"инета нету");
                checkBox_bluefolders.Enabled = false;
                label_nvcleaninstall.Enabled = false;
                label_activate.Enabled = false;
                label_ddu.Enabled = false;
                label_progs.Enabled = false;
                label_delete_defender.Enabled = false;
                checkBox_mmagent.Enabled = false;
                checkBox_pro_13.Enabled = false;
                checkBox_pro_14.Enabled = false;
                checkBox_ffmpeg.Enabled = false;
                checkBox_mica.Enabled = false;
                label_cursors.Enabled = false;
                label_inspector.Enabled = false;
                label_site.Enabled = false;

                MessageBox.Show("Нет доступа в интернет!\nПроврека на обновления, и некоторые функции недоступны.", "Windows optimization tool by oixro (WOTBO)",
    MessageBoxButtons.OK, MessageBoxIcon.Warning);


            }
            else
            {
                try
                {
                    UpdateInfo update = await AppServices.Updates.CheckAsync(curver);
                    switch (update.Status)
                    {
                        case UpdateStatus.Actual:
                            Log.Info("Update", $"версия актуальная");
                            label_ver.Text = $"{version}";
                            break;

                        case UpdateStatus.Newer:
                            File.WriteAllText("lastchange.json", update.ChangeLog ?? string.Empty);
                            MessageBox.Show($"Текущая версия - {curver}\nДоступна новая - {update.RemoteVersion}\nБудет выполнено обновление\n\n" +
                            $"Список изменений:\n {update.ChangeLog}", "", MessageBoxButtons.OK);

                            Log.Info("Update", $"скачиваем новую");
                            AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/WOTBO.exe", "new.exe");
                            void updateCMD(string line)
                            {
                                try
                                {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = "cmd.exe",
                                        Arguments = $"/c {line}",
                                        WindowStyle = ProcessWindowStyle.Hidden
                                    });
                                }
                                catch
                                {
                                }
                            }
                            updateCMD($"taskkill /f /im \"{exename}\" &&" +
                                $" timeout /t 1 &&" +
                                $" del \"{exepath}\" /f /q &&" +
                                $" del \"{contexpath}\" /f /q &&" +
                                $" del \"lastchange.json\" /f /q &&" +
                                $" copy new.exe \"{contexpath}\" &&" +
                                $" ren new.exe \"{exename}\" &&" +
                                $" rd \"{exepath}\" /s /q &" +
                                $" \"{exepath}\"");
                            break;

                        case UpdateStatus.Dev:
                            Log.Info("Update", $"версия dev");
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
            #endregion
            #region позиция, размеры и плавный запуск
            for (Opacity = 0; Opacity < 1; Opacity += .1) await Task.Delay(7);
            Log.Info("UI", $"плавно открылись");
            #endregion
            #region получаем инфу о видеодырке
            gpu = AppServices.Gpu.GetName();
            if (AppServices.Gpu.IsNvidia(gpu))
            {
                nvidia = true;
                Log.Info("GPU", $"GPU IS NVIDIA? - {nvidia}, gpu is - {gpu}");

                if (AppServices.TweakCatalog.Get("gpu.hdcp").Detect(AppServices.TweakContext) == TweakState.Applied)
                {
                    Log.Info("Tweak:gpu.hdcp", $"hdcp disabled");
                    checkBox_hdcp.Checked = false;
                    checkBox_hdcp.Enabled = false;
                    back_gpu_2.Visible = true;
                } //hdcp check

                if (AppServices.TweakCatalog.Get("gpu.nvidia").Detect(AppServices.TweakContext) == TweakState.Applied)
                {
                    Log.Info("Tweak:gpu.nvidia", $"DisplayPowerSaving disabled");
                    checkBox_dopNVIDIA_tweaks.Enabled = false;
                    back_gpu_3.Visible = true;
                }//tweaksnvidia

                TweakState anselState = AppServices.TweakCatalog.Get("gpu.ansel").Detect(AppServices.TweakContext);
                if (anselState == TweakState.Applied)
                {
                    Log.Info("Tweak:gpu.ansel", $"anselstate == \"0\"");
                    checkBox_ansel.Enabled = false;
                    back_gpu_4.Visible = true;
                }
                else if (anselState == TweakState.NotApplied)
                {
                    checkBox_ansel.Enabled = true;
                }
            }
            else
            {
                Log.Info("GPU", "не nvidia");
                nvidia = false;
                checkBox_ansel.Visible = false;
                checkBox_dopNVIDIA_tweaks.Visible = false;
                checkBox_hdcp.Visible = false;
                label_inspector.Visible = false;
                checkBox_directplay.Location = Dpi.Scale(this, 20, 45);
                back_gpu_1.Location = Dpi.Scale(this, 0, 45);
            }//не nvidia
            #endregion
            #region распаковка в temp
            Log.Info("Temp", "распаковка в temp");
            Directory.CreateDirectory(tempfolder);
            Log.Info("Temp", $"tempfolder создан - {tempfolder}");
            File.WriteAllBytes(path_regpack7z, Resources.regpack);
            Log.Info("Temp", $"path_regpack7z создан - {path_regpack7z}");
            if (!Directory.Exists(path_regpack))
                ZipFile.ExtractToDirectory($"{path_regpack7z}", $"{path_regpack}");
            Log.Info("Temp", $"path_regpack7z распакован - {path_regpack}");
            File.WriteAllBytes(tempfolder + @"\su.exe", Resources.su);
            Log.Info("Temp", $"su.exe распакован");
            File.WriteAllText(tempfolder + @"\Audio_Lantency.reg", Resources.Audio_Lantency);
            Log.Info("Temp", $"Audio_Lantency.reg распакован");
            File.WriteAllText(tempfolder + @"\Audio_Lantency_delete.reg", Resources.Audio_Lantency_delete);
            Log.Info("Temp", $"Audio_Lantency_delete.reg распакован");
            File.WriteAllText(tempfolder + @"\updates.reg", Resources.updates);
            Log.Info("Temp", $"updates.reg распакован");
            if (!Directory.Exists(path_ui))
            {
                File.WriteAllBytes(path_uizip, Resources.ui);
                ZipFile.ExtractToDirectory($"{path_uizip}", $"{path_ui}");
                Log.Info("Temp", $"ui.zip распакован - {path_ui}");
            }
            if (!File.Exists(@"c:\Windows\system32\explorer.bat"))
            {
                File.WriteAllText(@"c:\Windows\system32\explorer.bat", Resources.explor);
            }
            #endregion
            #region old_crap_uninstaller
            if (File.Exists(@"C:\Windows\System32\drivers\BackgroundMonitoringServices.exe"))
            {
                Process[] processes = Process.GetProcessesByName("BackgroundMonitoringServices");
                if (processes.Length > 0)
                {
                    Log.Info("System", $"BackgroundMonitoringServices был найден - {processes.Length}");
                    AppServices.Processes.StartCaptured("taskkill /f /im BackgroundMonitoringServices.exe");
                    File.Delete(@"C:\Windows\System32\drivers\BackgroundMonitoringServices.exe");
                }
                else
                {
                    File.Delete(@"C:\Windows\System32\drivers\BackgroundMonitoringServices.exe");
                }
            }
            #endregion
        }
        #endregion
        #region закрытие
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log.Info("App", "closing");
            notifier.Dispose();
            if (!AppServices.Workspace.Cleanup())
            {
                Log.Warn("App", "освобождаем память и убиваемся");
                InstanceGuard.Release();
                AppServices.Processes.StartCaptured($"taskkill /f /im {exename}");
            }
            InstanceGuard.Release();
        }
        #endregion
        #region все твики
        async void button1_Click(object sender, EventArgs e)
        {
            #region main
            Binder("main.defender").Apply();
            Binder("main.regpack").Apply();
            Binder("main.reservedstorage").Apply();
            Binder("main.hibernate").Apply();
            Binder("main.powerscheme").Apply();
            Binder("main.mpo").Apply();
            Binder("main.usb").Apply();
            Binder("main.mitigations").Apply();
            Binder("main.mmagent").Apply();
            if (checkBox_page.Checked)
            {
                try
                {
                    Binder("main.pagefile").Apply();
                    Log.Info("Tweak:main.pagefile", "[pagefile check] Параметры файла подкачки успешно изменены. Перезагрузите систему для применения изменений.");
                }
                catch (Exception ex)
                {
                    Log.Error("Tweak:main.pagefile", "[pagefile check] Произошла ошибка: " + ex.Message, ex);
                }
                ShowNotification("Файл подкачки", "Размер файла подкачки будет изменён после перезагрузки ПК", ToolTipIcon.Info);
            }
            Binder("main.mousefix").Apply();

            //вторая
            Binder("main.audio").Apply();
            Binder("main.dwminput").Apply();
            Binder("main.powerlatency").Apply();
            Binder("pro.win32priority").Apply(); //Win32Priority
            #endregion
            #region gpu
            if (checkBox_ansel.Checked)
            {
                using (TweakScope.Apply("gpu.ansel"))
                {
                    AnselTweak ansel = (AnselTweak)AppServices.TweakCatalog.Get("gpu.ansel");
                    if (ansel.Detect(AppServices.TweakContext) == TweakState.Unavailable)
                    {
                        MessageBox.Show("Файл NvCameraEnable.exe не найден");
                    }
                    ansel.Apply(AppServices.TweakContext);
                    checkBox_ansel.Checked = false;
                    checkBox_ansel.Enabled = false;
                    back_gpu_4.Visible = true;
                }
            }
            Binder("gpu.hdcp").Apply();
            Binder("gpu.nvidia").Apply();
            Binder("gpu.directplay").Apply();
            #endregion
            #region ui
            Binder("ui.buttons").Apply();
            Binder("ui.explorerdetails").Apply();
            Binder("ui.jpegquality").Apply();
            Binder("ui.bluefolders").Apply();
            if (checkBox_contex.Checked)
            {
                using (TweakScope.Apply("ui.contextmenu"))
                {
                    if (!win10)
                    {
                        AppServices.TweakCatalog.Get("ui.contextmenu").Apply(AppServices.TweakContext);
                        AppServices.Processes.StartCaptured(@"taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
                        checkBox_contex.Checked = false;
                        checkBox_contex.Enabled = false;
                        back_ui_6.Visible = true;
                        await Task.Delay(3000);
                    }
                }
            }
            Binder("ui.win11header").Apply();
            Binder("ui.wotbocontext").Apply();
            Binder("ui.explorerrestart").Apply();
            Binder("ui.ffmpeg").Apply();
            Binder("ui.mica").Apply();
            Binder("ui.animations").Apply();
            #endregion
            #region dop
            Binder("extra.onedrive").Apply();
            Binder("extra.compactos").Apply();
            Binder("extra.winsxs").Apply();
            Binder("extra.tempclean").Apply();
            Binder("extra.updateclean").Apply();
            Binder("extra.iconcache").Apply();
            Binder("extra.mobiletraffic").Apply();
            Binder("extra.scoobe").Apply();
            Binder("extra.stickykeys").Apply();
            Binder("extra.dwmpriority").Apply();
            Binder("extra.edge").Apply();
            Binder("extra.movetemp").Apply();
            #endregion
            #region pro
            Binder("pro.homegallery").Apply(); //disable main & gallery folder in explorer.exe
            Binder("pro.windowsupdate").Apply(); //winupdates

            Binder("pro.csrss").Apply(); //csrss
            //if (checkBox_pro_10.Checked)
            //{
            //    Process.Start(new ProcessStartInfo
            //    {
            //        FileName = $"cleanmgr.exe",
            //        Arguments = "sageset:99"
            //    }).WaitForExit();
            //    Process.Start(new ProcessStartInfo
            //    {
            //        FileName = $"cleanmgr.exe",
            //        Arguments = "sagerun:99"
            //    }).WaitForExit();
            //    checkBox_pro_10.Checked = false;
            //} //cleanmgr
            Binder("pro.bufferbloat").Apply(); //Bufferbloat
            Binder("pro.unlocker").Apply(); //unlocker
            Binder("pro.alwaysontop").Apply();//alwaysontop
            #endregion

            if (checkBox_tweak_tooltips.Checked)
            {
                ApplyTweakTooltips();
            }
        }
        #region курсоры
        void label_cursors_Click_1(object sender, EventArgs e)
        {
                if (!File.Exists($"{tempfolder}\\cursors.zip"))
                {
                    AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/cursors.zip", $"{tempfolder}\\cursors.zip");
                    ZipFile.ExtractToDirectory($"{tempfolder}\\cursors.zip", tempfolder);
                }
            if (MessageBox.Show($"Установить светлый или тёмный курсор?\nДа - светлый\nНет - тёмный", "WOTBO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                InstallCursor($@"{tempfolder}\cursors\light\small\base\Install.inf");
            }
            else
            {
                InstallCursor($@"{tempfolder}\cursors\dark\small\base\Install.inf");
            }
        }

        public void InstallCursor(string installerFilePath)
        {
            string command = @"C:\WINDOWS\System32\rundll32.exe";
            string arguments = "setupapi,InstallHinfSection DefaultInstall 132 " + installerFilePath;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{command} {arguments}\"",
                Verb = "runas",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process process = new Process
            {
                StartInfo = startInfo
            };

            process.Start();
            process.WaitForExit();
        }
        #endregion
        #region activation
        void label_activate_Click(object sender, EventArgs e)
        {
            AppServices.Processes.StartPowerShell("irm https://get.activated.win | iex");
        }
        #endregion
        #region msi mode
        void label_msimode_Click(object sender, EventArgs e)
        {
            if (Internet.OK())
            {
                try
                {
                        if (!File.Exists($"{tempfolder}\\MSI_util_v3.exe"))
                        {
                            AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/MSI_util_v3.exe", $"{tempfolder}\\MSI_util_v3.exe");
                            ZipFile.ExtractToDirectory($"{tempfolder}\\cursors.zip", tempfolder);
                        }
                }
                catch { }
            }
            Form4 msimode = new Form4();
            Process.Start($"{tempfolder}\\MSI_util_v3.exe");
            msimode.ShowDialog();
        }
        #endregion
        #region defender killer
        void label_delete_defender_Click(object sender, EventArgs e)
        {

                if (!File.Exists($"{tempfolder}\\DefenderKiller.zip"))
                {
                    AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/DefenderKiller.zip", $"{tempfolder}\\DefenderKiller.zip");
                    ZipFile.ExtractToDirectory(tempfolder + @"\DefenderKiller.zip", path_dfkiller);
                }
                else
                {
                    File.Delete(tempfolder + @"\DefenderKiller.zip");
                    Process.Start($@"{path_dfkiller}\DefenderKiller.bat").WaitForExit();
                }
            Process.Start($@"{path_dfkiller}\DefenderKiller.bat").WaitForExit();
        }
        #endregion
        #region wotbo.pro
        void label_site_Click(object sender, EventArgs e)
        {
            OpenUrl("https://wotbo.pro/");
        }
        #endregion
        #region kalorant
        void label_kalorant_Click(object sender, EventArgs e)
        {
            TweakScope.BackupRegFile($"{path_regpack}/exploitscfg.reg");
            AppServices.Processes.StartCaptured($"regedit.exe /s {path_regpack}/exploitscfg.reg");
            label_kalorant.Visible = false;
        }
        #endregion
        #region inspector
        void label_inspector_Click(object sender, EventArgs e)
        {
            //inspector
            if (!Directory.Exists(path_NVidiaProfileInspectorDmW))
            {
                File.WriteAllBytes(tempfolder + @"\Base_Profile.nip", Resources.Base_Profile);
                    if (!File.Exists($"{tempfolder}\\NVidiaProfileInspectorDmW.zip"))
                    {
                        AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/NVidiaProfileInspectorDmW.zip",
                            $"{tempfolder}\\NVidiaProfileInspectorDmW.zip");
                    }
                ZipFile.ExtractToDirectory(tempfolder + @"\NVidiaProfileInspectorDmW.zip", tempfolder);
                AppServices.Processes.StartCaptured($"{path_NVidiaProfileInspectorDmW}\\nvidiaProfileInspector.exe \"{tempfolder}\\Base_Profile.nip\"");
            }
        }
        #endregion
        #region clean
        async void label_clean_Click(object sender, EventArgs e)
        {
            await Task.Run(() => RunCleanMgr());
            void RunCleanMgr()
            {
                // Запуск cleanmgr.exe с аргументом sageset:99
                var process = new Process();
                process.StartInfo.FileName = "cleanmgr.exe";
                process.StartInfo.Arguments = "sageset:99";
                process.Start();
                process.WaitForExit();

                // После завершения первого процесса, запускаем второй с аргументом sagerun:99
                var secondProcess = new Process();
                secondProcess.StartInfo.FileName = "cleanmgr.exe";
                secondProcess.StartInfo.Arguments = "sagerun:99";
                secondProcess.Start();
            }
        }
        #endregion
        #endregion
        #region перемещение по пунктам
        #region main    
        void label1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel1);
            //panel1.Visible = true;
            //panel1.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = Dpi.Scale(this, 195, 284);

        } //main        
        void button_main_1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel1);
            //panel1.Visible = true;
            //panel1.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = Dpi.Scale(this, 195, 284);
        }
        void button_main_2_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_main_2);
            //panel_main_2.Visible = true;
            //panel_main_2.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = Dpi.Scale(this, 195, 284);
        }
        #endregion
        #region ui
        void label_interface_Click(object sender, EventArgs e)
        {

            ChangePanel(panel_ui_1);
            //panel_ui_1.Visible = true;
            //panel_ui_1.Location = panel1.Location;
            button_ui_new_1.Visible = true;
            button_ui_new_2.Visible = true;
            panel_ui_navigate.Visible = true;
            panel_ui_navigate.Location = Dpi.Scale(this, 195, 284);

        }//ui
        void button_ui_1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_ui_1);
            //panel_ui_1.Visible = true;
            //panel_ui_1.Location = panel1.Location;
            panel_ui_navigate.Visible = true;
        }
        void button_ui_2_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_ui_2);
            //panel_ui_2.Visible = true;
            //panel_ui_2.Location = panel1.Location;
            panel_ui_navigate.Visible = true;
        }
        #endregion
        #region gpu
        void label2_Click(object sender, EventArgs e)
        {
            #region directplay
            bool directplay = AppServices.TweakCatalog.Get("gpu.directplay").Detect(AppServices.TweakContext) == TweakState.Applied;
            if (directplay)
            {
                checkBox_directplay.Enabled = false;
                back_gpu_1.Visible = true;
            }

            Log.Info("Tweak:gpu.directplay", $"DirectPlay установлен: {directplay}");
            #endregion
            ChangePanel(panel2);
        } //gpu
        #endregion
        #region dop
        void label3_Click(object sender, EventArgs e) //dop
        {
            ChangePanel(panel_dop);
            //panel_dop.Visible = true;
            //panel_dop.Location = panel1.Location;
            button_dop_new_1.Visible = true;
            button_dop_new_2.Visible = true;
            panel_dop_navigate.Visible = true;
            panel_dop_navigate.Location = Dpi.Scale(this, 195, 284);
        }
        void button_dop_new_1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_dop);
            //panel_dop.Visible = true;
            //panel_dop.Location = panel1.Location;
            panel_dop_navigate.Visible = true;
        }
        void button_dop_new_2_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_dop_2);
            //panel_dop_2.Visible = true;
            //panel_dop_2.Location = panel1.Location;
            panel_dop_navigate.Visible = true;
        }
        #endregion
        #region dowloads
        void label6_Click(object sender, EventArgs e) // проги
        {
            ChangePanel(panel_sorry);
        }
        #endregion
        #region pc info
        void label8_Click(object sender, EventArgs e) //pc info
        {
            ChangePanel(panel_5);
        }
        #endregion

        #endregion
        #region download progs
        void label_download_1_Click(object sender, EventArgs e)
        {
            OpenUrl("https://win10tweaker.ru/");
        }

        private async Task OpenLatestOrFallback(string key, string fallback)
        {
            string url = null;
            try
            {
                url = await AppServices.LatestVersions.GetProgramUrlAsync(key);
            }
            catch (Exception ex)
            {
                Log.Warn("Programs", "не удалось получить последнюю версию " + key + ": " + ex.Message);
            }

            OpenUrl(string.IsNullOrEmpty(url) ? fallback : url);
        }

        async void label_download_2_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("notepadpp", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe");
        }
        void label_download_3_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.msi.com/Landing/afterburner/graphics-cards");
        }
        async void label_download_4_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("7zip", "https://www.7-zip.org/a/7z2409-x64.exe");
        }
        void label_download_5_Click(object sender, EventArgs e)
        {
            OpenUrl("https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe");
        }
        void label_download_6_Click(object sender, EventArgs e)
        {
            OpenUrl("https://dl.comss.org/download/Visual-C-Runtimes-All-in-One-Nov-2024.zip");
        }
        void label_download_7_Click(object sender, EventArgs e)
        {
            OpenUrl("https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe");
        }
        async void label_download_8_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("sharex", "https://github.com/ShareX/ShareX/releases/download/v17.0.0/ShareX-17.0.0-setup.exe");
        }
        void label_download_9_Click(object sender, EventArgs e)
        {
            OpenUrl("https://telegram.org/dl/desktop/win64_portable");
        }
        void label_download_10_Click(object sender, EventArgs e)
        {
            OpenUrl("https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64");
        }
        async void label_download_11_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("obs", "https://cdn-fastly.obsproject.com/downloads/OBS-Studio-31.0.0-Windows-Installer.exe");
        }
        async void label_download_13_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("klite", "https://files2.codecguide.com/K-Lite_Codec_Pack_1875_Mega.exe");
        }
        async void label_download_14_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("qbittorrent", "https://deac-fra.dl.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-5.0.3/qbittorrent_5.0.3_x64_setup.exe?viasf=1");
        }
        void label_download_15_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.hwinfo.com/files/hwi64_860.exe");
        }
        void label_download_16_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.google.com/chrome/");
        }
        void label_download_17_Click(object sender, EventArgs e)
        {
            OpenUrl("https://static.centbrowser.com/win_stable/5.1.1130.129/centbrowser_5.1.1130.129_x64.exe");
        }
        async void label_download_18_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("wiztree", "https://diskanalyzer.com/files/wiztree_4_23_portable.zip");
        }
        void label_download_19_Click(object sender, EventArgs e)
        {
            OpenUrl("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi");
        }
        void label_download_20_Click(object sender, EventArgs e)
        {
            OpenUrl("https://anydesk.com/ru/downloads/thank-you?dv=win_exe");
        }
        void label_download_21_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.faststonesoft.net/DN/FSViewerSetup85.exe");
        }
        async void label_download_22_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("dotnet6", "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.28-windows-x64-installer");
        }
        async void label_download_23_Click(object sender, EventArgs e)
        {
            await OpenLatestOrFallback("crystaldiskinfo", "https://deac-riga.dl.sourceforge.net/project/crystaldiskinfo/9.2.3/CrystalDiskInfo9_2_3.exe");
        }
        void label_download_24_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.amd.com/en/support/download/drivers.html");
        }
        #endregion
        #region gpu downloads


        void label_ddu_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.wagnardsoft.com/display-driver-uninstaller-DDU-");
        }

        void label_nvcleaninstall_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.techpowerup.com/download/techpowerup-nvcleanstall/");
        }
        #endregion
        #region откаты
        void back_main_1_Click(object sender, EventArgs e)
        {
            Binder("main.regpack").Rollback();
        } //regpack
        void back_main_2_Click(object sender, EventArgs e)
        {
            Binder("main.reservedstorage").Rollback();
        } //dism
        void back_main_3_Click(object sender, EventArgs e)
        {
            Binder("main.hibernate").Rollback();
        } //powercfg /h off
        void back_main_4_Click(object sender, EventArgs e)
        {
            Binder("main.powerscheme").Rollback();
        }//scheme
        void back_main_5_Click(object sender, EventArgs e)
        {
            Binder("main.mousefix").Rollback();
        } //mouse
        void back_main_6_Click(object sender, EventArgs e)
        {
            Binder("main.mpo").Rollback();
        }//mpo
        void back_main_7_Click(object sender, EventArgs e)
        {
            Binder("main.usb").Rollback();
        }//usbpowersaving
        void back_main_9_Click(object sender, EventArgs e)
        {
            Binder("main.mitigations").Rollback();
        }//mititigiig
        void back_main_10_Click(object sender, EventArgs e)
        {
            Binder("main.mmagent").Rollback();
        } //mmagent не буду делать
        void back_main_11_Click(object sender, EventArgs e)
        {
            try
            {
                Binder("main.pagefile").Rollback();
                Log.Info("Tweak:main.pagefile", "[pagefile check] Стандартные настройки файла подкачки восстановлены. Перезагрузите систему для применения изменений.");
            }
            catch (Exception ex)
            {
                Log.Error("Tweak:main.pagefile", "[pagefile check] Произошла ошибка: " + ex.Message, ex);
                checkBox_page.Enabled = true;
                checkBox_page.Checked = false;
                back_main_11.Visible = false;
            }
        } //page file
        void back_main_12_Click(object sender, EventArgs e)
        {
            Binder("main.defender").Rollback();
        } //restoreDEF
        void back_main_13_Click(object sender, EventArgs e) //Audio_Lantency_delete.reg
        {
            Binder("main.audio").Rollback();
        }

        void back_main_14_Click(object sender, EventArgs e)
        {
            Binder("main.dwminput").Rollback();
        }

        void back_main_16_Click(object sender, EventArgs e)
        {
            Binder("main.powerlatency").Rollback();
        }



        void back_ui_1_Click(object sender, EventArgs e)
        {
            Binder("ui.buttons").Rollback();
        } //buttons
        void back_ui_2_Click(object sender, EventArgs e)
        {
            Binder("ui.explorerdetails").Rollback();
        }//заглушка

        void back_ui_4_Click(object sender, EventArgs e)
        {
            Binder("ui.jpegquality").Rollback();
        } //jpegwallappers
        void back_ui_5_Click(object sender, EventArgs e)
        {
            Binder("ui.bluefolders").Rollback();
        }//bluefolders
        void back_ui_6_Click(object sender, EventArgs e)
        {
            Binder("ui.contextmenu").Rollback();
        }//contex
        void back_ui_7_Click(object sender, EventArgs e)
        {
            Binder("ui.win11header").Rollback();
        }//shapka
        void back_ui_8_Click(object sender, EventArgs e)
        {
            Binder("ui.wotbocontext").Rollback();
        }
        void back_ui_9_Click(object sender, EventArgs e)
        {
            Binder("ui.explorerrestart").Rollback();
        }
        void back_ui_10_Click(object sender, EventArgs e)
        {
            Binder("ui.ffmpeg").Rollback();
        } //ffmpeg
        void back_ui_11_Click(object sender, EventArgs e)
        {
            Binder("ui.mica").Rollback();
        }
        void back_animations_Click(object sender, EventArgs e)
        {
            Binder("ui.animations").Rollback();
        }
        void back_ui_tooltips_Click(object sender, EventArgs e)
        {
            RollbackTweakTooltips();
        }
        void back_gpu_1_Click(object sender, EventArgs e)
        {
            Binder("gpu.directplay").Rollback();
        }//directplay
        void back_gpu_2_Click(object sender, EventArgs e)
        {
            Binder("gpu.hdcp").Rollback();
        }//hdcp
        void back_gpu_3_Click(object sender, EventArgs e)
        {
            Binder("gpu.nvidia").Rollback();
        }//settings nvidia
        void back_gpu_4_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("gpu.ansel"))
            {
                AnselTweak ansel = (AnselTweak)AppServices.TweakCatalog.Get("gpu.ansel");
                if (ansel.Detect(AppServices.TweakContext) == TweakState.Unavailable)
                {
                    MessageBox.Show("Файл NvCameraEnable.exe не найден");
                }
                ansel.Rollback(AppServices.TweakContext);
                checkBox_ansel.Checked = false;
                checkBox_ansel.Enabled = true;
                back_gpu_4.Visible = false;
            }
        }//ansel

        void back_pro_1_Click(object sender, EventArgs e)
        {
            Binder("pro.homegallery").Rollback();
        }
        void back_pro_3_Click(object sender, EventArgs e)
        {
            Binder("pro.windowsupdate").Rollback();
        }
        void back_pro_4_Click(object sender, EventArgs e)
        {

        }

        void back_pro_6_Click(object sender, EventArgs e)
        {

        }
        void back_pro_7_Click(object sender, EventArgs e)
        {

        }
        void back_pro_8_Click(object sender, EventArgs e)
        {
            Binder("pro.csrss").Rollback();
        }
        void back_pro_9_Click(object sender, EventArgs e)
        {

        }
        void back_pro_11_Click(object sender, EventArgs e)
        {
            Binder("pro.win32priority").Rollback();
        }

        void back_pro_12_Click(object sender, EventArgs e)
        {
            Binder("pro.bufferbloat").Rollback();
        }
        void back_pro_13_Click(object sender, EventArgs e)
        {
            Binder("pro.unlocker").Rollback();
        }
        void back_pro_14_Click(object sender, EventArgs e)
        {
            Binder("pro.alwaysontop").Rollback();
        }


        void back_dop_1_Click(object sender, EventArgs e)
        {
            Binder("extra.iconcache").Rollback();
        }

        void back_dop_2_Click(object sender, EventArgs e)
        {
            Binder("extra.mobiletraffic").Rollback();
        }
        void back_dop_edge_Click(object sender, EventArgs e)
        {
            Binder("extra.edge").Rollback();
        }
        void back_dop_3_Click(object sender, EventArgs e)
        {
            Binder("extra.scoobe").Rollback();
        }

        void back_dop_4_Click(object sender, EventArgs e)
        {
            Binder("extra.stickykeys").Rollback();
        }
        void back_dop_6_Click(object sender, EventArgs e)
        {
            Binder("extra.dwmpriority").Rollback();
        }
        void back_dop_movetemp_Click(object sender, EventArgs e)
        {
            Binder("extra.movetemp").Rollback();
        }

        #endregion
    }
}