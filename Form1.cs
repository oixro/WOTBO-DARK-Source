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
        static string smartctl = tempfolder + @"\smartctl.exe";
        private CheckBox checkBox_tweak_tooltips;
        private PictureBox back_ui_tooltips;
        string curver = (Assembly.GetExecutingAssembly().GetName().Version.ToString(2)).Trim();
        string exename = AppDomain.CurrentDomain.FriendlyName;
        string exepath = Assembly.GetEntryAssembly().Location;
        string contexpath = @"C:\Windows\wotbo.exe";
        int mmMemory = 0;
        public string uilanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        string logfile = AppPaths.LogFilePath;
        public static string gpu;
        public bool nvidia;
        public static long capacity;
        static string path_NVidiaProfileInspectorDmW = tempfolder + @"\NVidiaProfileInspectorDmW";
        private NotifyIcon notifyIcon;

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
            #region двигаем окно
            new List<Control> { LabelHead, PanelHead, logo }.ForEach(x =>
            {
                x.MouseDown += (s, a) =>
                {
                    x.Capture = false; Capture = false; Message m = Message.Create(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); base.WndProc(ref m);
                };
            });
            #endregion
            #region плавное закрытие + окраска формы
            async void Exit() { for (Opacity = 1; Opacity > .0; Opacity -= .2) await Task.Delay(7); Close(); }
            ButtonClose.Click += (s, a) => Exit();
            FormPaint(Color.FromArgb(44, 57, 67), Color.FromArgb(35, 44, 55)); //old
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
            #region NotifyIcon
            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information, // Устанавливаем иконку (или любую другую)
                Visible = false // Сначала иконка не видима
            };
            #endregion
            #region label animations
            new List<Control> { label_main, label_interface, label_gpu, label_dop, label_progs, label_pc, label_site, label_winver, LabelHead, label_ver, label7 }.ForEach(x =>
            {
                x.MouseHover += async (s, a) =>
                {
                    // Начальный цвет (192, 192, 192)
                    Color startColor = Color.FromArgb(192, 192, 192);
                    // Конечный цвет (255, 255, 255)
                    Color endColor = Color.FromArgb(255, 255, 255);

                    // Длительность анимации в миллисекундах
                    int duration = 50;

                    // Количество шагов анимации
                    int steps = duration / 5;

                    for (int i = 0; i <= steps; i++)
                    {
                        // Интерполяция цвета
                        int r = startColor.R + (endColor.R - startColor.R) * i / steps;
                        int g = startColor.G + (endColor.G - startColor.G) * i / steps;
                        int b = startColor.B + (endColor.B - startColor.B) * i / steps;

                        x.ForeColor = Color.FromArgb(r, g, b);
                        await Task.Delay(15);
                    }
                };

                x.MouseLeave += async (s, a) =>
                {
                    // Начальный цвет (белый)
                    Color startColor = Color.FromArgb(255, 255, 255);
                    // Конечный цвет (чуть уже не белый)
                    Color endColor = Color.FromArgb(192, 192, 192);

                    // Длительность анимации в миллисекундах
                    int duration = 50;

                    // Количество шагов анимации
                    int steps = duration / 5;

                    for (int i = 0; i <= steps; i++)
                    {
                        // Интерполяция цвета
                        int r = startColor.R + (endColor.R - startColor.R) * i / steps;
                        int g = startColor.G + (endColor.G - startColor.G) * i / steps;
                        int b = startColor.B + (endColor.B - startColor.B) * i / steps;

                        x.ForeColor = Color.FromArgb(r, g, b);
                        await Task.Delay(15);
                    }
                };
            });
            #endregion
        }
        #endregion
        #region my void's
        #region анимка
        async public void ChangePanel(Panel NewPanel)
        {
            #region old style
            foreach (Control control in this.Controls)
            {
                if (control is Panel panel && panel != PanelMain && panel != PanelHead)
                {
                    panel.Visible = false;
                }
            }

            // Устанавливаем начальную позицию новой панели вне видимости (например, справа от формы)
            NewPanel.Location = new Point(405, 36); //
            NewPanel.Visible = true;

            // Параметры для плавной анимации
            int startX = 405;
            int endX = 105;
            int duration = 125; // увеличиваем продолжительность для более плавного движения
            int steps = 40;
            int delayPerStep = duration / steps;
            for (int i = 0; i <= steps; i++)
            {
                if (animation)
                {
                    float t = (float)i / steps; // нормализованное время от 0 до 1
                    float easedT = t < 0.5f ? 4f * t * t * t : (t - 1) * (2f * t - 2f) * (2f * t - 2f) + 1f; // easeInOutCubic
                    int currentX = (int)(startX + (endX - startX) * easedT);
                    NewPanel.Location = new Point(currentX, NewPanel.Location.Y);
                    await Task.Delay(delayPerStep);
                }
                else
                {
                    NewPanel.Location = new Point(105, 36);
                }




            }
            #endregion
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
            int duration = 10000;
            // Устанавливаем текст уведомления
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = message;
            notifyIcon.BalloonTipIcon = icon;

            // Показываем уведомление
            notifyIcon.Visible = true; // Делаем иконку видимой для показа уведомления
            notifyIcon.ShowBalloonTip(duration);

            // Настраиваем скрытие иконки через указанное время
            Timer timer = new Timer { Interval = duration + 500 }; // +500 мс, чтобы гарантировать завершение показа
            timer.Tick += (s, e) =>
            {
                notifyIcon.Visible = false; // Скрываем иконку
                timer.Stop(); // Останавливаем таймер
                timer.Dispose(); // Освобождаем ресурсы таймера
            };
            timer.Start();
        }

        // Красим форму
        public void FormPaint(Color color1, Color color2)
        {
            void OnPaintEventHandler(object s, PaintEventArgs a)
            {
                if (ClientRectangle == Rectangle.Empty)
                    return;

                var lgb = new LinearGradientBrush(ClientRectangle, Color.Empty, Color.Empty, 90);
                var cblend = new ColorBlend { Colors = new[] { color1, color1, color2, color2 }, Positions = new[] { 0, 0.106f, 0.106f, 1 } };

                lgb.InterpolationColors = cblend;
                a.Graphics.FillRectangle(lgb, ClientRectangle);
            }

            Paint -= OnPaintEventHandler;
            Paint += OnPaintEventHandler;

            Invalidate();
        }

        #endregion
        #region открытие
        private void SetupTweakTooltipsToggle()
        {
            back_ui_tooltips = new PictureBox
            {
                Name = "back_ui_tooltips",
                Location = new Point(0, 85),
                Size = new Size(15, 15),
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
                Location = new Point(20, 85),
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
            smartctl = workspace.Smartctl;
            #endregion
            #region отключаем чеки
            if (AppServices.TweakCatalog.Get("main.regpack").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.regpack", "regpack был применён");
                checkBox_reg.Enabled = false;
                back_main_1.Visible = true;
            }//regpack
            string reservedStorageState = await AppServices.ReservedStorage.GetStateAsync();
            if (AppServices.ReservedStorage.IsDisabled(reservedStorageState))
            {
                Log.Info("Tweak:main.reservedstorage", "ReservedStorage был применён");
                checkBox_dism.Enabled = false;
                back_main_2.Visible = true;
            } //old reserved
            if (AppServices.TweakCatalog.Get("main.hibernate").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.hibernate", "hiberfil'a нету");
                checkBox_gibernate.Enabled = false;
                back_main_3.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("main.powerscheme").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.powerscheme", "imported_powerscheme был применён");
                checkBox_scheme.Enabled = false;
                back_main_4.Visible = true;
            }
            if (AppServices.Tweaks.IsApplied("ui.animations"))
            {
                animation = false;
                Log.Info("Tweak:ui.animations", "анимации выключены");
                checkBox_animation.Enabled = false;
                back_animations.Visible = true;
            }//анимации
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
            if (AppServices.TweakCatalog.Get("main.mpo").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.mpo", "MPO был применён");
                checkBox_mpo.Enabled = false;
                back_main_6.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("main.usb").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.usb", "usbpowersaving был применён");
                checkBox_usb.Enabled = false;
                back_main_7.Visible = true;
            }
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
            if (AppServices.TweakCatalog.Get("main.mitigations").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.mitigations", "spectre and meltdown был применён");
                checkBox_mtiititit.Enabled = false;
                back_main_9.Visible = true;
            }
            if (AppServices.Tweaks.IsApplied("main.mmagent"))
            {
                Log.Info("Tweak:main.mmagent", "mmagent был применён");
                checkBox_mmagent.Enabled = false;
                back_main_10.Visible = true;
            }
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
            if (AppServices.TweakCatalog.Get("ui.buttons").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:ui.buttons", "buttons был применён");
                checkBoxUI_Buttons_1.Enabled = false;
                back_ui_1.Visible = true;
            }
            if (AppServices.Tweaks.IsApplied("ui.explorerdetails"))
            {
                Log.Info("Tweak:ui.explorerdetails", "litle_explorer_things был применён");
                checkBoxUI_Buttons_2.Enabled = false;
                back_ui_2.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("ui.jpegquality").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:ui.jpegquality", "JPEGImportQuality был применён");
                checkBoxUI_Buttons_4.Enabled = false;
                back_ui_4.Visible = true;
            } //wallpapers
            if (AppServices.TweakCatalog.Get("ui.contextmenu").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:ui.contextmenu", "contex11 был применён");
                checkBox_contex.Enabled = false;
                back_ui_6.Visible = true;
            } //contex11
            try
            {
                if (AppServices.TweakCatalog.Get("ui.win11header").Detect(AppServices.TweakContext) == TweakState.Applied)
                {
                    Log.Info("Tweak:ui.win11header", "shapka11 был применён");
                    checkBox_shapka.Enabled = false;
                    back_ui_7.Visible = true;
                }//shapka11
            }
            catch
            {

            } //shapka11
            if (AppServices.Tweaks.IsApplied("pro.homegallery"))
            {
                Log.Info("Tweak:pro.homegallery", "pro_1 был применён");
                checkBox_pro_1.Enabled = false;

            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(RegistryPaths.PriorityControl).GetValue("Win32PrioritySeparation")) != 0x00000002)
            {
                Log.Info("Tweak:pro.win32priority", "Win32PrioritySeparation был применён");
                checkBox_pro_11.Enabled = false;
                back_pro_11.Visible = true;
            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU")?.GetValue("NoAutoUpdate")) == 1)
            {
                Log.Info("Tweak:pro.windowsupdate", "NoAutoUpdate был применён");
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;

            }
            if (AppServices.TweakCatalog.Get("main.defender").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.defender", "defenderdisabled был применён");
                checkBox_disabledefender.Enabled = false;
                label_delete_defender.Enabled = true;
                back_main_12.Visible = true;
                label_kalorant.Visible = true;
            }
            if (File.Exists($@"C:\Windows\System32\imageres.dll_bak"))
            {
                Log.Info("Tweak:ui.bluefolders", "синие папки win 10 были применены");
                checkBox_bluefolders.Enabled = false;
                back_ui_5.Visible = true;
            }
            if (File.Exists($@"C:\Windows\SystemResources\imageres.dll.mun_bak"))
            {
                Log.Info("Tweak:ui.bluefolders", "синие папки win 11 были применены");
                checkBox_bluefolders.Enabled = false;
                back_ui_5.Visible = true;
            }
            if (Registry.ClassesRoot.OpenSubKey(@"DesktopBackground\Shell\WOTBO")?.GetValue("") != null)
            {
                Log.Info("Tweak:ui.wotbocontext", "wotbo in contex был применён");
                checkBox_wotboincontex.Enabled = false;
                back_ui_8.Visible = true;
            }
            if (!Directory.Exists(@"C:\ProgramData\Microsoft\Windows Defender"))
            {
                Log.Info("Tweak:main.defender", "defender удалён");
                //checkBox_disabledefender.Visible = false;
                checkBox_disabledefender.Enabled = false;
                back_main_12.Visible = false;
            }
            if (!File.Exists($@"{SysFolder.UserProfile}\AppData\Local\Microsoft\OneDrive\OneDrive.exe"))
            {
                Log.Info("Tweak:extra.onedrive", "OneDrive был удалён");
                checkBox_onedrive.Enabled = false;
            }
            if (exepath == contexpath)
            {
                Log.Info("App", "запущены из конткестного");
                back_ui_8.Visible = false;
            }
            if (AppServices.TweakCatalog.Get("pro.bufferbloat").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:pro.bufferbloat", "bufferbloat был применён");
                checkBox_pro_12.Enabled = false;
                back_pro_12.Visible = true;
            }
            if (File.Exists(@"C:\Windows\unlocker\Unlocker.exe"))
            {
                Log.Info("Tweak:pro.unlocker", "unclockewr был применён");
                checkBox_pro_13.Enabled = false;
                back_pro_13.Visible = true;
            }
            if (File.Exists(@"C:\Windows\AlwaysOnTop.exe"))
            {
                Log.Info("Tweak:pro.alwaysontop", "AlwaysOnTop был применён");
                checkBox_pro_14.Enabled = false;
                back_pro_14.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("ui.ffmpeg").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:ui.ffmpeg", "FFmpeg был применён");
                checkBox_ffmpeg.Enabled = false;
                back_ui_10.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("extra.iconcache").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:extra.iconcache", "Max Cached Icons был применён");
                checkBox_picture_cache.Enabled = false;
                back_dop_1.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("extra.mobiletraffic").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:extra.mobiletraffic", "DefaultTTL был применён");
                checkBox_mobile_traffic.Enabled = false;
                back_dop_2.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("extra.scoobe").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:extra.scoobe", "ScoobeSystemSettingEnabled был применён");
                checkBox_nastroyka.Enabled = false;
                back_dop_3.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("extra.stickykeys").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:extra.stickykeys", "StickyKeys был применён");
                checkBox_zalipanie.Enabled = false;
                back_dop_4.Visible = true;
            }
            if (File.Exists(@"C:\Windows\ExplorerBlurMica.dll"))
            {
                Log.Info("Tweak:ui.mica", "ExplorerBlurMica был применён");
                checkBox_mica.Enabled = false;
                back_ui_11.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("ui.explorerrestart").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:ui.explorerrestart", "restart explorer был применён");
                checkBox_explorer.Enabled = false;
                back_ui_9.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("extra.dwmpriority").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:extra.dwmpriority", "dwm.exe priority был применён");
                checkBox_dwm.Enabled = false;
                back_dop_6.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("pro.csrss").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:pro.csrss", "csrss.exe priority был применён");
                checkBox_CSRSS.Enabled = false;
                back_dop_7.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("main.audio").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.audio", "AllowHeadlessExecution BackgroundAudioPolicy был применён");
                checkBox_audio.Enabled = false;
                back_main_13.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("main.dwminput").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.dwminput", "DwmInputUsesIoCompletionPort был применён");
                checkBox_dwninput.Enabled = false;
                back_main_14.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("main.powerlatency").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:main.powerlatency", "Power\\ExitLatency был применён");
                checkBox_tolerate.Enabled = false;
                back_main_16.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Policies\Microsoft\Edge") != null)
            {
                Log.Info("Tweak:extra.edge", "Edge был применён");
                back_dop_edge.Visible = true;
                checkBox_edge.Checked = false;
                checkBox_edge.Enabled = false;
            }
            if (AppServices.TweakCatalog.Get("pro.windowsupdate").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                Log.Info("Tweak:pro.windowsupdate", "WindowsUpdate\\AU был применён");
                checkBox_pro_3.Checked = false;
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;
            }
            if (AppServices.TweakCatalog.Get("extra.movetemp").Detect(AppServices.TweakContext) == TweakState.Applied)
            {
                checkBox_move_temp.Enabled = false;
                back_dop_movetemp.Visible = true;
            }
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
                checkBox_directplay.Location = new Point(20, 45);
                back_gpu_1.Location = new Point(0, 45);
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
            if (checkBox_disabledefender.Checked)
            {
                using (TweakScope.Apply("main.defender"))
                {
                    MessageBox.Show("Защитник будет отключен!" +
                        "\nЗащита от эксполитов не будет работать." +
                        "\nОна нужна для работы некоторых античитов." +
                        "\nНо восстановить защитник можно через вкладку дополнительно.\n" +
                        "", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    AppServices.TweakCatalog.Get("main.defender").Apply(AppServices.TweakContext);
                    checkBox_disabledefender.Enabled = false;
                    checkBox_disabledefender.Checked = false;
                    label_kalorant.Visible = true;
                    back_main_12.Visible = true;
                    ShowNotification("Защитник", "Рекомендуется перезагрузить компьютер для полноценного отключения защитника!", ToolTipIcon.Warning);
                }
            }
            if (checkBox_reg.Checked)
            {
                using (TweakScope.Apply("main.regpack"))
                {
                    AppServices.TweakCatalog.Get("main.regpack").Apply(AppServices.TweakContext);
                    checkBox_reg.Enabled = false;
                    checkBox_reg.Checked = false;
                    back_main_1.Visible = true;
                    ShowNotification("Настройки Реестра", "Рекомендуется перезагрузить ПК после применения данного твика!", ToolTipIcon.Warning);
                }
            }
            if (checkBox_dism.Checked)
            {
                using (TweakScope.Apply("main.reservedstorage"))
                {
                    AppServices.Processes.StartCaptured(@"Dism /Online /Set-ReservedStorageState /State:Disabled");
                        checkBox_dism.Enabled = false;
                    checkBox_dism.Checked = false;
                    back_main_2.Visible = true;
                }
            }
            if (checkBox_gibernate.Checked)
            {
                using (TweakScope.Apply("main.hibernate"))
                {
                    AppServices.TweakCatalog.Get("main.hibernate").Apply(AppServices.TweakContext);
                        checkBox_gibernate.Enabled = false;
                    checkBox_gibernate.Checked = false;
                    back_main_3.Visible = true;

                }
            }
            if (checkBox_scheme.Checked)
            {
                using (TweakScope.Apply("main.powerscheme"))
                {
                    AppServices.TweakCatalog.Get("main.powerscheme").Apply(AppServices.TweakContext);
                        checkBox_scheme.Enabled = false;
                    checkBox_scheme.Checked = false;
                    back_main_4.Visible = true;
                }
            }
            if (checkBox_mpo.Checked)
            {
                using (TweakScope.Apply("main.mpo"))
                {
                    AppServices.TweakCatalog.Get("main.mpo").Apply(AppServices.TweakContext);
                    checkBox_mpo.Enabled = false;
                    checkBox_mpo.Checked = false;
                    back_main_6.Visible = true;
                }
            }
            if (checkBox_usb.Checked)
            {
                using (TweakScope.Apply("main.usb"))
                {
                    File.WriteAllText(tempfolder + @"\usbpowersaving.bat", Resources.usbpowersaving);
                    TweakScope.BackupRegFile($"{tempfolder}/usbpowersaving.cmd");
                    AppServices.Processes.StartCaptured($"regedit.exe /s {tempfolder}/usbpowersaving.cmd");
                    AppServices.TweakCatalog.Get("main.usb").Apply(AppServices.TweakContext);
                    checkBox_usb.Enabled = false;
                    checkBox_usb.Checked = false;
                    back_main_7.Visible = true;
                }
            }
            if (checkBox_mtiititit.Checked)
            {
                using (TweakScope.Apply("main.mitigations"))
                {
                    AppServices.TweakCatalog.Get("main.mitigations").Apply(AppServices.TweakContext);
                    checkBox_mtiititit.Enabled = false;
                    checkBox_mtiititit.Checked = false;
                    back_main_9.Visible = true;

                }
            }
            if (checkBox_mmagent.Checked)
            {
                using (TweakScope.Apply("main.mmagent"))
                {
                    File.WriteAllText(tempfolder + @"\prefetcher hdd.reg", Resources.prefetcher_hdd);
                    File.WriteAllText(tempfolder + @"\prefetcher ssd.reg", Resources.prefetcher_ssd);
                    //

                        if (!File.Exists($"{tempfolder}\\DefenderKiller.zip"))
                        {
                            AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/smartctl.exe", $"{tempfolder}\\smartctl.exe");
                        }


                    Process getramGB = Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "/command (Get-WmiObject Win32_PhysicalMemory).capacity | Measure-Object -Sum | Foreach {[int]($_.Sum/1GB)}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    });
                    string ramGB = ((getramGB.StandardOutput.ReadToEnd())).Trim();


                    string systemdisk = Environment.GetEnvironmentVariable("windir").Replace(@"\Windows", "");
                    //MessageBox.Show(systemdisk);
                    Process checkssd = Process.Start(new ProcessStartInfo
                    {
                        FileName = $"{smartctl}",
                        Arguments = $"-i {systemdisk}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    });
                    string ssd = ((checkssd.StandardOutput.ReadToEnd())).ToLower();
                    //MessageBox.Show(ssd);
                    bool isdiskssd = false;
                    if (ssd.Contains("solid state device"))
                        isdiskssd = true;
                    if ((ssd.Contains("ssd")))
                        isdiskssd = true;
                    if ((ssd.Contains("nvme")))
                        isdiskssd = true;
                    if (isdiskssd)
                    {
                        TweakScope.BackupRegFile($"{tempfolder}/prefetcher ssd.reg");
                        AppServices.Processes.StartCaptured($"regedit.exe /s {tempfolder}/prefetcher ssd.reg");
                    }
                    else
                    {
                        TweakScope.BackupRegFile($"{tempfolder}/prefetcher hdd.reg");
                        AppServices.Processes.StartCaptured($"regedit.exe /s {tempfolder}/prefetcher hdd.reg");
                    }
                    int ramgbINT = Convert.ToInt32(ramGB) * 32;
                    if (ramgbINT <= 128)
                    {
                        mmMemory = 128;
                    }
                    else if (ramgbINT >= 1024)
                    {
                        mmMemory = 1024;
                    }
                    else
                    {
                        mmMemory = ramgbINT;
                    }
                    if (win10 == false)
                    {
                        AppServices.Processes.StartPowerShell($"enable-mmagent -ApplicationPreLaunch");
                        AppServices.Processes.StartPowerShell($"enable-mmagent -MC");
                        AppServices.Processes.StartPowerShell($"disable-mmagent -PC");
                        AppServices.Processes.StartPowerShell($"set-mmagent -moaf {mmMemory}");
                    }
                    else
                    {
                        AppServices.Processes.StartPowerShell($"enable-mmagent -ApplicationPreLaunch");
                        AppServices.Processes.StartPowerShell($"disable-mmagent -MC");
                        AppServices.Processes.StartPowerShell($"disable-mmagent -PC");
                        AppServices.Processes.StartPowerShell($"set-mmagent -moaf {mmMemory}");
                    }

                        checkBox_mmagent.Enabled = false;
                    checkBox_mmagent.Checked = false;
                    back_main_10.Visible = true;
                }
            }
            if (checkBox_page.Checked)
            {
                using (TweakScope.Apply("main.pagefile"))
                {
                    try
                    {
                        AppServices.TweakCatalog.Get("main.pagefile").Apply(AppServices.TweakContext);
                        Log.Info("Tweak:main.pagefile", "[pagefile check] Параметры файла подкачки успешно изменены. Перезагрузите систему для применения изменений.");
                        checkBox_page.Enabled = false;
                        checkBox_page.Checked = false;
                        back_main_11.Visible = true;
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Tweak:main.pagefile", "[pagefile check] Произошла ошибка: " + ex.Message, ex);
                    }
                    ShowNotification("Файл подкачки", "Размер файла подкачки будет изменён после перезагрузки ПК", ToolTipIcon.Info);
                }
            }
            if (checkBox_mousefix.Checked)
            {
                using (TweakScope.Apply("main.mousefix"))
                {
                    AppServices.TweakCatalog.Get("main.mousefix").Apply(AppServices.TweakContext);
                    checkBox_mousefix.Enabled = false;
                    checkBox_mousefix.Checked = false;
                    back_main_5.Visible = true;
                }
            }

            //вторая
            if (checkBox_audio.Checked)
            {
                using (TweakScope.Apply("main.audio"))
                {
                    AppServices.Processes.RunSuper($@"regedit /s {tempfolder}\Audio_Lantency.reg", tempfolder + @"\su.exe");
                    AppServices.TweakCatalog.Get("main.audio").Apply(AppServices.TweakContext);
                    checkBox_audio.Checked = false;
                    checkBox_audio.Enabled = false;
                    back_main_13.Visible = true;
                }
            }
            if (checkBox_dwninput.Checked)
            {
                using (TweakScope.Apply("main.dwminput"))
                {
                    AppServices.TweakCatalog.Get("main.dwminput").Apply(AppServices.TweakContext);
                    checkBox_dwninput.Checked = false;
                    checkBox_dwninput.Enabled = false;
                    back_main_14.Visible = true;
                }
            }
            if (checkBox_tolerate.Checked)
            {
                using (TweakScope.Apply("main.powerlatency"))
                {
                    AppServices.TweakCatalog.Get("main.powerlatency").Apply(AppServices.TweakContext);
                    checkBox_tolerate.Checked = false;
                    checkBox_tolerate.Enabled = false;
                    back_main_16.Visible = true;
                }
            }
            if (checkBox_pro_11.Checked)
            {
                using (TweakScope.Apply("pro.win32priority"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.LocalMachine, RegistryPaths.PriorityControl);
                    Win32Priority Win32Priority = new Win32Priority();
                    Win32Priority.ShowDialog();
                    checkBox_pro_11.Checked = false;
                    checkBox_pro_11.Enabled = false;
                    back_pro_11.Visible = true;
                }
            } //Win32Priority
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
            if (checkBox_hdcp.Checked)
            {
                using (TweakScope.Apply("gpu.hdcp"))
                {
                    checkBox_hdcp.Checked = false;
                    checkBox_hdcp.Enabled = false;
                    AppServices.TweakCatalog.Get("gpu.hdcp").Apply(AppServices.TweakContext);
                    back_gpu_2.Visible = true;
                }
            }
            if (checkBox_dopNVIDIA_tweaks.Checked)
            {
                using (TweakScope.Apply("gpu.nvidia"))
                {
                    AppServices.TweakCatalog.Get("gpu.nvidia").Apply(AppServices.TweakContext);
                    checkBox_dopNVIDIA_tweaks.Checked = false;
                    checkBox_dopNVIDIA_tweaks.Enabled = false;
                    back_gpu_3.Visible = true;
                }
            }
            if (checkBox_directplay.Checked)
            {
                using (TweakScope.Apply("gpu.directplay"))
                {
                    AppServices.TweakCatalog.Get("gpu.directplay").Apply(AppServices.TweakContext);
                    checkBox_directplay.Checked = false;
                    checkBox_directplay.Enabled = false;
                    back_gpu_1.Visible = true;
                }
            }
            #endregion
            #region ui
            if (checkBoxUI_Buttons_1.Checked)
            {
                using (TweakScope.Apply("ui.buttons"))
                {
                    AppServices.TweakCatalog.Get("ui.buttons").Apply(AppServices.TweakContext);
                }
                checkBoxUI_Buttons_1.Enabled = false;
                checkBoxUI_Buttons_1.Checked = false;
                back_ui_1.Visible = true;
            }
            if (checkBoxUI_Buttons_2.Checked)
            {
                TweakScope.BackupRegFile($"{path_ui}/things.reg");
                AppServices.Processes.StartCaptured($"regedit.exe /s {path_ui}/things.reg");
                AppServices.Processes.StartCaptured(@"rd /s /q ""%userprofile%\3D Objects\""");
                if (!win10)
                {
                    AppServices.Processes.StartCaptured(@"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""UseCompactMode"" /t REG_DWORD /d ""00000001"" /f");
                }
                else
                {
                    TweakScope.BackupRegFile($"{path_ui}/Taskmgr_win10_new.reg");
                    AppServices.Processes.StartCaptured($"regedit.exe /s {path_ui}/Taskmgr_win10_new.reg");
                }

                AppServices.Processes.StartCaptured("taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
                checkBoxUI_Buttons_2.Enabled = false;
                checkBoxUI_Buttons_2.Checked = false;
                back_ui_2.Visible = true;
            }
            if (checkBoxUI_Buttons_4.Checked)
            {
                using (TweakScope.Apply("ui.jpegquality"))
                {
                    AppServices.TweakCatalog.Get("ui.jpegquality").Apply(AppServices.TweakContext);
                }

                checkBoxUI_Buttons_4.Enabled = false;
                checkBoxUI_Buttons_4.Checked = false;
                back_ui_4.Visible = true;
            }
            if (checkBox_bluefolders.Checked)
            {
                using (TweakScope.Apply("ui.bluefolders"))
                {
                    TweakScope.BackupFile(@"C:\Windows\System32\imageres.dll");
                    TweakScope.BackupFile(@"C:\Windows\SystemResources\imageres.dll.mun");
                    if (win10)
                    {
                        File.WriteAllText(tempfolder + @"\blueiconsw10.bat", Resources.blueiconsw10);
                            if (!File.Exists($"{tempfolder}\\imageres.zip"))
                            {
                                AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/imageres.zip", $"{tempfolder}\\imageres.zip");
                                ZipFile.ExtractToDirectory($"{tempfolder}\\imageres.zip", tempfolder);
                            }
                        await AppServices.Processes.StartCapturedAsync($"{tempfolder}\\blueiconsw10.bat");
                        checkBox_bluefolders.Checked = false;
                        checkBox_bluefolders.Enabled = false;
                        back_ui_5.Visible = true;
                        //AppServices.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, "bluefolders", 1);
                    }
                    else
                    {
                        File.WriteAllText(tempfolder + @"\blueicons.bat", Resources.blueicons);
                            if (!File.Exists($"{tempfolder}\\BlueIcon_Minimal.zip"))
                            {
                                AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/BlueIcon_Minimal.zip", $"{tempfolder}\\BlueIcon_Minimal.zip");
                                ZipFile.ExtractToDirectory($"{tempfolder}\\BlueIcon_Minimal.zip", tempfolder + "\\BlueIcon Minimal");
                            }
                        await AppServices.Processes.StartCapturedAsync($"{tempfolder}\\blueicons.bat");
                        checkBox_bluefolders.Checked = false;
                        checkBox_bluefolders.Enabled = false;
                        back_ui_5.Visible = true;
                        //AppServices.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, "bluefolders", 1);
                    }
                }
            }
            if (checkBox_contex.Checked)
            {
                using (TweakScope.Apply("ui.contextmenu"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}");
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
            if (checkBox_shapka.Checked)
            {
                using (TweakScope.Apply("ui.win11header"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.ClassesRoot, @"CLSID\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}");
                    AppServices.TweakCatalog.Get("ui.win11header").Apply(AppServices.TweakContext);
                    checkBox_shapka.Checked = false;
                    checkBox_shapka.Enabled = false;
                    back_ui_7.Visible = true;
                }
            }
            if (checkBox_wotboincontex.Checked)
            {
                using (TweakScope.Apply("ui.wotbocontext"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.ClassesRoot, @"DesktopBackground\Shell\WOTBO");
                    TweakScope.BackupFile(@"C:\Windows\gear.ico");
                    TweakScope.BackupFile(@"C:\Windows\wotbo.exe");
                    if (!File.Exists(@"C:\Windows\gear.ico"))
                    {
                        File.WriteAllBytes(tempfolder + @"\gear.zip", Resources.gear);
                        ZipFile.ExtractToDirectory(tempfolder + @"\gear.zip", "C:\\Windows");
                    }
                    RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\WOTBO", true);
                    AppServices.Registry.WriteValue(key, "", "WOTBO");
                    AppServices.Registry.WriteValue(key, "icon", @"C:\Windows\gear.ico");//вставить ico
                    key.CreateSubKey("command");
                    key.Close();
                    if (!File.Exists(@"C:\Windows\wotbo.exe"))
                    {
                        File.Copy(exepath, @"C:\Windows\wotbo.exe");
                    }
                    AppServices.Registry.WriteValue(RegistryHive.ClassesRoot, @"DesktopBackground\Shell\WOTBO\command", "", contexpath);
                    checkBox_wotboincontex.Checked = false;
                    checkBox_wotboincontex.Enabled = false;
                    back_ui_8.Visible = true;
                }
            }
            if (checkBox_explorer.Checked)
            {
                using (TweakScope.Apply("ui.explorerrestart"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.ClassesRoot, @"DesktopBackground\Shell\explorer");
                    TweakScope.BackupFile(@"c:\Windows\system32\explorer.bat");
                    AppServices.TweakCatalog.Get("ui.explorerrestart").Apply(AppServices.TweakContext);
                    checkBox_explorer.Checked = false;
                    checkBox_explorer.Enabled = false;
                    back_ui_9.Visible = true;
                }
            }
            if (checkBox_ffmpeg.Checked)
            {
                using (TweakScope.Apply("ui.ffmpeg"))
                {
                    ShowNotification("FFMPEG", "Ожидайте скачивания FFMPEG. Программа не зависла!", ToolTipIcon.Info);
                    AppServices.TweakCatalog.Get("ui.ffmpeg").Apply(AppServices.TweakContext);
                    checkBox_ffmpeg.Enabled = false;
                    checkBox_ffmpeg.Checked = false;
                    back_ui_10.Visible = true;
                }
            }
            if (checkBox_mica.Checked)
            {
                using (TweakScope.Apply("ui.mica"))
                {
                        if (!File.Exists($"{tempfolder}\\Release_x64.zip"))
                        {
                            AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/Release_x64.zip", $"{tempfolder}\\Release_x64.zip");
                            ZipFile.ExtractToDirectory($"{tempfolder}\\Release_x64.zip", tempfolder);
                        }
                    TweakScope.BackupFile(@"C:\Windows\ExplorerBlurMica.dll");
                    TweakScope.BackupFile(@"C:\Windows\config.ini");
                    File.Copy($@"{tempfolder}\Release\ExplorerBlurMica.dll", @"C:\Windows\ExplorerBlurMica.dll");
                    File.Copy($@"{tempfolder}\Release\config.ini", @"C:\Windows\config.ini");
                    AppServices.Processes.StartCaptured($"regsvr32 /s C:\\Windows\\ExplorerBlurMica.dll & taskkill /f /im explorer.exe & explorer.exe & timeout /t 1 & explorer.exe");
                    checkBox_mica.Enabled = false;
                    checkBox_mica.Checked = false;
                    back_ui_11.Visible = true;
                }
            }
            if (checkBox_animation.Checked)
            {
                using (TweakScope.Apply("ui.animations"))
                {
                    animation = false;
                        back_animations.Visible = true;
                    checkBox_animation.Checked = false;
                    checkBox_animation.Enabled = false;
                }
            }
            #endregion
            #region dop
            if (checkBox_onedrive.Checked)
            {
                using (TweakScope.Apply("extra.onedrive"))
                {
                    #region new method
                    // Массив с путями к файлу OneDriveSetup.exe
                    string[] OneDriveSetup = Directory.GetFiles(@"C:\Windows\System32", "OneDriveSetup.exe", SearchOption.TopDirectoryOnly)
                                                      .Concat(Directory.GetFiles(@"C:\Windows\SysWOW64", "OneDriveSetup.exe", SearchOption.TopDirectoryOnly))
                                                      .ToArray();

                    // Проверка наличия хотя бы одного пути к файлу
                    if (OneDriveSetup.Length > 0)
                    {
                        // Путь к первому найденному файлу
                        string OneDriveSetupPath = OneDriveSetup[0];
                        TweakScope.BackupFile(OneDriveSetupPath);
                        Log.Info("Tweak:extra.onedrive", $"путь к Onedrive найден - {OneDriveSetupPath}, переходим к удалению");

                        // Вызов команды для деинсталляции OneDrive
                        await AppServices.Processes.StartCapturedAsync($"{OneDriveSetupPath} /uninstall");
                    }
                    else
                    {
                        // Логирование ошибки, если файл не найден
                        Log.Warn("Tweak:extra.onedrive", "Файл OneDriveSetup.exe не найден");
                    }
                    #endregion
                    checkBox_onedrive.Checked = false;
                    checkBox_onedrive.Enabled = false;
                }
            }
            if (checkBox_compactos.Checked)
            {
                using (TweakScope.Apply("extra.compactos"))
                {
                    AppServices.TweakCatalog.Get("extra.compactos").Apply(AppServices.TweakContext);
                    checkBox_compactos.Checked = false;
                }
            }
            if (checkBox_WinSxS.Checked)
            {
                using (TweakScope.Apply("extra.winsxs"))
                {
                    AppServices.TweakCatalog.Get("extra.winsxs").Apply(AppServices.TweakContext);
                    checkBox_WinSxS.Checked = false;
                }
            }
            if (checkBox_temp.Checked)
            {
                using (TweakScope.Apply("extra.tempclean"))
                {
                    AppServices.TweakCatalog.Get("extra.tempclean").Apply(AppServices.TweakContext);
                    checkBox_temp.Checked = false;
                }
            }
            if (checkBox_updclean.Checked)
            {
                using (TweakScope.Apply("extra.updateclean"))
                {
                    AppServices.TweakCatalog.Get("extra.updateclean").Apply(AppServices.TweakContext);
                    checkBox_updclean.Checked = false;
                }
            }
            if (checkBox_picture_cache.Checked)
            {
                using (TweakScope.Apply("extra.iconcache"))
                {
                    AppServices.TweakCatalog.Get("extra.iconcache").Apply(AppServices.TweakContext);
                    checkBox_picture_cache.Checked = false;
                    checkBox_picture_cache.Enabled = false;
                    back_dop_1.Visible = true;
                }
            }
            if (checkBox_mobile_traffic.Checked)
            {
                using (TweakScope.Apply("extra.mobiletraffic"))
                {
                    AppServices.TweakCatalog.Get("extra.mobiletraffic").Apply(AppServices.TweakContext);
                    checkBox_mobile_traffic.Checked = false;
                    checkBox_mobile_traffic.Enabled = false;
                    back_dop_2.Visible = true;
                }
            }
            if (checkBox_nastroyka.Checked)
            {
                using (TweakScope.Apply("extra.scoobe"))
                {
                    AppServices.TweakCatalog.Get("extra.scoobe").Apply(AppServices.TweakContext);
                    checkBox_nastroyka.Checked = false;
                    checkBox_nastroyka.Enabled = false;
                    back_dop_3.Visible = true;
                }
            }
            if (checkBox_zalipanie.Checked)
            {
                using (TweakScope.Apply("extra.stickykeys"))
                {
                    AppServices.TweakCatalog.Get("extra.stickykeys").Apply(AppServices.TweakContext);
                    checkBox_zalipanie.Checked = false;
                    checkBox_zalipanie.Enabled = false;
                    back_dop_4.Visible = true;
                }
            }
            if (checkBox_dwm.Checked)
            {
                using (TweakScope.Apply("extra.dwmpriority"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.LocalMachine, RegistryPaths.ImageFileExecutionOptions + @"\dwm.exe");
                    AppServices.TweakCatalog.Get("extra.dwmpriority").Apply(AppServices.TweakContext);
                    checkBox_dwm.Checked = false;
                    checkBox_dwm.Enabled = false;
                    back_dop_6.Visible = true;
                }
            }
            if (checkBox_edge.Checked)
            {
                using (TweakScope.Apply("extra.edge"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge");
                    RegistryKey edge;
                    edge = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Edge");
                    AppServices.Registry.WriteValue(edge, "EdgeEnhanceImagesEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "EdgeWorkspacesEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "TyposquattingCheckerEnabled", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "RemoveDesktopShortcutDefault", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "AlternateErrorPagesEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SpotlightExperiencesAndRecommendationsEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "NewTabPageSearchBox", "redirect");
                    AppServices.Registry.WriteValue(edge, "DefaultSearchProviderEnabled", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "BasicAuthOverHttpEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "AllowCrossOriginAuthPrompt", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DisableAuthNegotiateCnameLookup", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "NativeMessagingUserLevelHosts", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "PasswordMonitorAllowed", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "StartupBoostEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "UseSystemPrintDialog", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SleepingTabsEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "RestoreOnStartup", 0x00000004, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "HomepageLocation", "www.google.com");
                    AppServices.Registry.WriteValue(edge, "NewTabPageLocation", "www.google.com");
                    AppServices.Registry.WriteValue(edge, "NewTabPagePrerenderEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "NewTabPageHideDefaultTopSites", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "AdsSettingForIntrusiveAdsSites", 0x00000002, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DownloadRestrictions", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "TabFreezingEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "InternetExplorerIntegrationTestingAllowed", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "InternetExplorerIntegrationLocalFileAllowed", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "PersonalizationReportingEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "BrowserNetworkTimeQueriesEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "LocalProvidersEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "AudioSandboxEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "WebWidgetIsEnabledOnStartup", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "UserFeedbackAllowed", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "FamilySafetySettingsEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ClickOnceEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DirectInvokeEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SSLErrorOverrideAllowed", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "BingAdsSuppression", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "TrackingPrevention", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "BrowserSignin", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "InternetExplorerIntegrationEnhancedHangDetection", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "InternetExplorerIntegrationLevel", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ConfigureOnlineTextToSpeech", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "VerticalTabsAllowed", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ConfigureFriendlyURLFormat", 0x00000004, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ExperimentationAndConfigurationServiceControl", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "IntensiveWakeUpThrottlingEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DnsOverHttpsMode", "off");
                    AppServices.Registry.WriteValue(edge, "DNSInterceptionChecksEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "TargetBlankImpliesNoOpener", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ComponentUpdatesEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "EnableDomainActionsDownload", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "NetworkPredictionOptions", 0x00000002, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "EnableOnlineRevocationChecks", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "RendererCodeIntegrityEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ResolveNavigationErrorsUseWebService", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "BackgroundTemplateListUpdatesEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SearchSuggestEnabled", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "CommandLineFlagSecurityWarningsEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SitePerProcess", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SpellcheckEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "EdgeCollectionsEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "WebWidgetAllowed", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "MetricsReportingEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ForceEphemeralProfiles", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ForceGoogleSafeSearch", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "GoToIntranetSiteForSingleWordEntryInAddressBar", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "HideFirstRunExperience", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "HideInternetExplorerRedirectUXForIncompatibleSitesEnabled", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "RelaunchNotification", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "RedirectSitesFromInternetExplorerPreventBHOInstall", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "PromotionalTabsEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DiagnosticData", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SendSiteInfoToImproveServices", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "TotalMemoryLimitMb", 0x00008192, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "WPADQuickCheckEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "EdgeShoppingAssistantEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "InternetExplorerIntegrationLocalFileShowContextMenu", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "AddressBarMicrosoftSearchInBingProviderEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ShowOfficeShortcutInFavoritesBar", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ShowMicrosoftRewards", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "BuiltInDnsClientEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ClearCachedImagesAndFilesOnExit", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ConfigureDoNotTrack", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ApplicationGuardFavoritesSyncEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ApplicationGuardTrafficIdentificationEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ApplicationGuardUploadBlockingEnabled", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "EnableMediaRouter", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "ShowCastIconInToolbar", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultCookiesSetting", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultFileSystemReadGuardSetting", 0x00000003, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultFileSystemWriteGuardSetting", 0x00000003, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultGeolocationSetting", 0x00000002, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultImagesSetting", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultInsecureContentSetting", 0x00000002, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultJavaScriptJitSetting", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultJavaScriptSetting", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultNotificationsSetting", 0x00000003, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultPluginsSetting", 0x00000003, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultPopupsSetting", 0x00000002, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultWebBluetoothGuardSetting", 0x00000003, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultWebHidGuardSetting", 0x00000002, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "DefaultWebUsbGuardSetting", 0x00000002, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "LegacySameSiteCookieBehaviorEnabled", 0x00000002, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "PreventSmartScreenPromptOverrideForFiles", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "PreventSmartScreenPromptOverride", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SmartScreenForTrustedDownloadsEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "NewSmartScreenLibraryEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SmartScreenDnsRequestsEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SmartScreenPuaEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SmartScreenEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "NewTabPageContentEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "NewTabPageQuickLinksEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "NewTabPageAllowedBackgroundTypes", 0x00000003, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "TyposquattingCheckerEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "BackgroundModeEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "EfficiencyMode", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SuppressUnsupportedOSWarning", 0x00000001, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "HubsSidebarEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "SiteSafetyServicesEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "EdgeFollowEnabled", 0x00000000, RegistryValueKind.DWord);
                    AppServices.Registry.WriteValue(edge, "LocalBrowserDataShareEnabled", 0x00000000, RegistryValueKind.DWord);
                    edge = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Edge\\RestoreOnStartupURLs");
                    AppServices.Registry.WriteValue(edge, "1", "www.google.com");
                    back_dop_edge.Visible = true;
                    checkBox_edge.Checked = false;
                    checkBox_edge.Enabled = false;
                }
            }
            if (checkBox_move_temp.Checked)
            {
                using (TweakScope.Apply("extra.movetemp"))
                {
                    AppServices.TweakCatalog.Get("extra.movetemp").Apply(AppServices.TweakContext);
                    checkBox_move_temp.Checked = false;
                    checkBox_move_temp.Enabled = false;
                    back_dop_movetemp.Visible = true;
                }
            }
            #endregion
            #region pro
            if (checkBox_pro_1.Checked)
            {
                using (TweakScope.Apply("pro.homegallery"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.LocalMachine, RegistryPaths.Explorer);
                    TweakScope.BackupRegistryKey(RegistryHive.ClassesRoot, @"CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}");
                    TweakScope.BackupRegistryKey(RegistryHive.CurrentUser, @"Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}");
                    {  //HomeDelete
                        AppServices.Processes.StartCaptured(@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v ""HubMode"" /t REG_DWORD /d ""00000001"" /f >nul 2>&1");
                        AppServices.Processes.RunSuper(@"cmd.exe /c reg add ""HKCR\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}"" /v ""System.IsPinnedToNameSpaceTree"" /t REG_DWORD /d ""00000000"" /f", tempfolder + @"\su.exe");
                        //GaleryDelete
                        AppServices.Processes.StartCaptured("reg add \"HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}\" /v \"System.IsPinnedToNameSpaceTree\" /t REG_DWORD /d \"00000000\" /f >nul 2>&1");
                        AppServices.Processes.RunSuper(@"cmd.exe /c reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}"" /v ""System.IsPinnedToNameSpaceTree"" /t REG_DWORD /d ""00000001"" /f >nul 2>&1", tempfolder + @"\su.exe");
                        //restart explorer
                        AppServices.Processes.StartCaptured("taskkill /f /im explorer.exe && timeout /t 1 && explorer.exe");
                                checkBox_pro_1.Checked = false;
                        checkBox_pro_1.Enabled = false;
                    }
                }
            } //disable main & gallery folder in explorer.exe
            if (checkBox_pro_3.Checked)
            {
                using (TweakScope.Apply("pro.windowsupdate"))
                {
                    AppServices.TweakCatalog.Get("pro.windowsupdate").Apply(AppServices.TweakContext);
                    checkBox_pro_3.Checked = false;
                    checkBox_pro_3.Enabled = false;
                    back_pro_3.Visible = true;
                }
            } //winupdates

            if (checkBox_CSRSS.Checked)
            {
                using (TweakScope.Apply("pro.csrss"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.LocalMachine, RegistryPaths.ImageFileExecutionOptions + @"\csrss.exe");
                    AppServices.TweakCatalog.Get("pro.csrss").Apply(AppServices.TweakContext);

                    checkBox_CSRSS.Checked = false;
                    checkBox_CSRSS.Enabled = false;
                    back_dop_7.Visible = true;
                    //AppServices.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, "pro_8", 1);
                }
            } //csrss
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
            if (checkBox_pro_12.Checked)
            {
                using (TweakScope.Apply("pro.bufferbloat"))
                {
                    DialogResult result = MessageBox.Show(
                        "Bufferbloat - явление, когда чрезмерная буферизация вызывает увеличение времени прохождения пакетов (Ping) и разброса задержки пакетов (Packetloss)\n\n" +
                        "Рекомендуется использовать только если у вас нестабильное соединение!\n(Например мобильный интернет и т.д)\n" +
                        "!Твик может незначительно снизить скорость интернета!\n" +
                        "В случае если у вас стало хуже, или же вам не нужен этот твик, просто откатите его.\n" +
                        "Применить?",
        "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        using (TweakScope.Apply("pro.bufferbloat"))
                        {
                            AppServices.TweakCatalog.Get("pro.bufferbloat").Apply(AppServices.TweakContext);
                        }
                        checkBox_pro_12.Checked = false;
                        checkBox_pro_12.Enabled = false;
                        back_pro_12.Visible = true;
                            }
                    else
                    {
                        checkBox_pro_12.Checked = false;
                    }
                }
            } //Bufferbloat
            if (checkBox_pro_13.Checked)
            {
                using (TweakScope.Apply("pro.unlocker"))
                {
                    TweakScope.BackupRegistryKey(RegistryHive.ClassesRoot, @"*\shell\Уничтожить");
                    TweakScope.BackupRegistryKey(RegistryHive.ClassesRoot, @"Directory\shell\Уничтожить");
                    TweakScope.BackupDirectory(@"C:\Windows\unlocker");
                        if (!Directory.Exists($"{tempfolder}\\unlocker.zip"))
                        {
                            AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/unlocker.zip", $"{tempfolder}\\unlocker.zip");
                            ZipFile.ExtractToDirectory($"{tempfolder}\\unlocker.zip", tempfolder);
                        }
                    if (!Directory.Exists($"C:\\Windows\\unlocker"))
                    {
                        Directory.Move($"{tempfolder}\\unlocker", $"C:\\Windows\\unlocker");
                    }
                    else
                    {
                        Directory.Delete($"C:\\Windows\\unlocker", true);
                        Directory.Move($"{tempfolder}\\unlocker", $"C:\\Windows\\unlocker");
                    }
                    AppServices.Registry.WriteValue(RegistryHive.ClassesRoot, @"*\shell\Уничтожить", "Icon", @"C:\Windows\unlocker\Icons\Unlocker.ico");
                    AppServices.Registry.WriteValue(RegistryHive.ClassesRoot, @"*\shell\Уничтожить\command", "", "\"C:\\Windows\\unlocker\\Unlocker.exe\" /contextMenu \"%1\"");
                    AppServices.Registry.WriteValue(RegistryHive.ClassesRoot, @"Directory\shell\Уничтожить", "", @"Уничтожить");
                    AppServices.Registry.WriteValue(RegistryHive.ClassesRoot, @"Directory\shell\Уничтожить", "Icon", @"C:\Windows\unlocker\Icons\Unlocker.ico");
                    AppServices.Registry.WriteValue(RegistryHive.ClassesRoot, @"Directory\shell\Уничтожить\command", "", "\"C:\\Windows\\unlocker\\Unlocker.exe\" /contextMenu \"%1\"");
                    checkBox_pro_13.Checked = false;
                    checkBox_pro_13.Enabled = false;
                    back_pro_13.Visible = true;
                }
            } //unlocker
            if (checkBox_pro_14.Checked)
            {
                using (TweakScope.Apply("pro.alwaysontop"))
                {
                    TweakScope.BackupFile(@"C:\Windows\AlwaysOnTop.exe");
                        if (!File.Exists(@"C:\Windows\AlwaysOnTop.exe"))
                        {
                            AppServices.Network.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/AlwaysOnTop.exe", @"C:\Windows\AlwaysOnTop.exe");
                        }
                    AppServices.Registry.WriteValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AlwaysOnTop", @"C:\Windows\AlwaysOnTop.exe");
                    Process.Start(@"C:\Windows\AlwaysOnTop.exe");
                    checkBox_pro_14.Checked = false;
                    checkBox_pro_14.Enabled = false;
                    back_pro_14.Visible = true;
                }
            }//alwaysontop
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
            panel_main_navigate.Location = new Point(195, 284);

        } //main        
        void button_main_1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel1);
            //panel1.Visible = true;
            //panel1.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = new Point(195, 284);
        }
        void button_main_2_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_main_2);
            //panel_main_2.Visible = true;
            //panel_main_2.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = new Point(195, 284);
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
            panel_ui_navigate.Location = new Point(195, 284);

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
            panel_dop_navigate.Location = new Point(195, 284);
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
            OpenUrl("https://www.hwinfo.com/files/hwi_820.zip");
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
            OpenUrl("https://www.faststonesoft.net/DN/FSViewerSetup79.exe");
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
            using (TweakScope.Rollback("main.regpack"))
            {
                AppServices.TweakCatalog.Get("main.regpack").Rollback(AppServices.TweakContext);
                checkBox_reg.Enabled = true;
                back_main_1.Visible = false;
            }
        } //regpack
        void back_main_2_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.reservedstorage"))
            {
                AppServices.Processes.StartCaptured("Dism.exe /Online /Set-ReservedStorageState /State:Enabled");
                //AppServices.Registry.DeleteValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, "ReservedStorageState");
                checkBox_dism.Enabled = true;
                checkBox_dism.Checked = false;
                back_main_2.Visible = false;
            }
        } //dism
        void back_main_3_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.hibernate"))
            {
                AppServices.TweakCatalog.Get("main.hibernate").Rollback(AppServices.TweakContext);
                //AppServices.Registry.DeleteValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, "hibernate");
                checkBox_gibernate.Enabled = true;
                checkBox_gibernate.Checked = false;
                back_main_3.Visible = false;
            }
        } //powercfg /h off
        void back_main_4_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.powerscheme"))
            {
                AppServices.TweakCatalog.Get("main.powerscheme").Rollback(AppServices.TweakContext);
                checkBox_scheme.Enabled = true;
                checkBox_scheme.Checked = false;
                back_main_4.Visible = false;
            }
        }//scheme
        void back_main_5_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.mousefix"))
            {
                AppServices.TweakCatalog.Get("main.mousefix").Rollback(AppServices.TweakContext);
                checkBox_mousefix.Enabled = true;
                checkBox_mousefix.Checked = false;
                back_main_5.Visible = false;
            }
        } //mouse
        void back_main_6_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.mpo"))
            {
                AppServices.TweakCatalog.Get("main.mpo").Rollback(AppServices.TweakContext);
                checkBox_mpo.Enabled = true;
                checkBox_mpo.Checked = false;
                back_main_6.Visible = false;
            }
        }//mpo
        void back_main_7_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.usb"))
            {
                File.WriteAllText(tempfolder + @"\unusbpowersaving.bat", Resources.unusbpowersaving);
                AppServices.Processes.StartCaptured($"regedit.exe /s {tempfolder}/un_usbpowersaving.cmd");
                AppServices.TweakCatalog.Get("main.usb").Rollback(AppServices.TweakContext);
                checkBox_usb.Enabled = true;
                checkBox_usb.Checked = false;
                back_main_7.Visible = false;
            }
        }//usbpowersaving
        void back_main_9_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.mitigations"))
            {
                AppServices.TweakCatalog.Get("main.mitigations").Rollback(AppServices.TweakContext);

                AppServices.Processes.RunSuper("ren %SYSTEMROOT%\\System32\\mcupdate_GenuineIntel.old mcupdate_GenuineIntel.dll", tempfolder + @"\su.exe");
                AppServices.Processes.RunSuper("ren %SYSTEMROOT%\\System32\\mcupdate_AuthenticAMD.old mcupdate_AuthenticAMD.dll", tempfolder + @"\su.exe");

                checkBox_mtiititit.Enabled = true;
                checkBox_mtiititit.Checked = false;
                back_main_9.Visible = false;
            }
        }//mititigiig
        void back_main_10_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.mmagent"))
            {
                checkBox_mmagent.Enabled = true;
                checkBox_mmagent.Checked = false;
                back_main_10.Visible = false;
            }
        } //mmagent не буду делать
        void back_main_11_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.pagefile"))
            {
                try
                {
                    AppServices.TweakCatalog.Get("main.pagefile").Rollback(AppServices.TweakContext);
                    Log.Info("Tweak:main.pagefile", "[pagefile check] Стандартные настройки файла подкачки восстановлены. Перезагрузите систему для применения изменений.");
                }
                catch (Exception ex)
                {
                    Log.Error("Tweak:main.pagefile", "[pagefile check] Произошла ошибка: " + ex.Message, ex);
                }
                checkBox_page.Enabled = true;
                checkBox_page.Checked = false;
                back_main_11.Visible = false;
            }
        } //page file
        void back_main_12_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.defender"))
            {
                AppServices.TweakCatalog.Get("main.defender").Rollback(AppServices.TweakContext);
                back_main_12.Visible = false;
                //MessageBox.Show("Для восстановления работы защитника выполните перезагрузку!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowNotification("Защитник Windows", "Работа защитника будет восстановлена после перезагруки ПК", ToolTipIcon.Warning);
                checkBox_disabledefender.Enabled = true;
                checkBox_disabledefender.Checked = false;

            }
        } //restoreDEF
        void back_main_13_Click(object sender, EventArgs e) //Audio_Lantency_delete.reg
        {
            using (TweakScope.Rollback("main.audio"))
            {
                AppServices.Processes.RunSuper($@"regedit /s {tempfolder}\Audio_Lantency_delete.reg", tempfolder + @"\su.exe");
                AppServices.TweakCatalog.Get("main.audio").Rollback(AppServices.TweakContext);
                checkBox_audio.Checked = false;
                checkBox_audio.Enabled = true;
                back_main_13.Visible = false;
            }
        }

        void back_main_14_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.dwminput"))
            {
                AppServices.TweakCatalog.Get("main.dwminput").Rollback(AppServices.TweakContext);
                checkBox_dwninput.Checked = false;
                checkBox_dwninput.Enabled = true;
                back_main_14.Visible = false;
            }
        }

        void back_main_16_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("main.powerlatency"))
            {
                AppServices.TweakCatalog.Get("main.powerlatency").Rollback(AppServices.TweakContext);
                checkBox_tolerate.Checked = false;
                checkBox_tolerate.Enabled = true;
                back_main_16.Visible = false;
            }
        }



        void back_ui_1_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.buttons"))
            {
                AppServices.TweakCatalog.Get("ui.buttons").Rollback(AppServices.TweakContext);
                checkBoxUI_Buttons_1.Enabled = true;
                checkBoxUI_Buttons_1.Checked = false;
                back_ui_1.Visible = false;
            }
        } //buttons
        void back_ui_2_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.explorerdetails"))
            {
                AppServices.Processes.StartCaptured($"regedit.exe /s {path_ui}/un_things.reg");
                if (!win10)
                {
                    AppServices.Processes.StartCaptured(@"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""UseCompactMode"" /t REG_DWORD /d ""00000000"" /f");
                }
                else
                {
                    AppServices.Processes.StartCaptured($"regedit.exe /s {path_ui}/Taskmgr_win10_default.reg");
                }

                AppServices.Processes.StartCaptured("taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
                checkBoxUI_Buttons_2.Enabled = true;
                checkBoxUI_Buttons_2.Checked = false;
                back_ui_2.Visible = false;
            }
        }//заглушка

        void back_ui_4_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.jpegquality"))
            {
                AppServices.TweakCatalog.Get("ui.jpegquality").Rollback(AppServices.TweakContext);
                checkBoxUI_Buttons_4.Enabled = true;
                checkBoxUI_Buttons_4.Checked = false;
                back_ui_4.Visible = false;
            }
        } //jpegwallappers
        void back_ui_5_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.bluefolders"))
            {
                if (win10)
                {
                    File.WriteAllText(tempfolder + @"\deficonsw10.bat", Resources.deficonsw10);
                    AppServices.Processes.StartCaptured($"{tempfolder}\\deficonsw10.bat");
                }
                else
                {
                    File.WriteAllText(tempfolder + @"\defblueicons.bat", Resources.defblueicons);
                    AppServices.Processes.StartCaptured($"{tempfolder}\\defblueicons.bat");
                }
                checkBox_bluefolders.Checked = false;
                checkBox_bluefolders.Enabled = true;
                //AppServices.Registry.DeleteValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, "bluefolders");
                back_ui_5.Visible = false;
            }
        }//bluefolders
        void back_ui_6_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.contextmenu"))
            {
                AppServices.TweakCatalog.Get("ui.contextmenu").Rollback(AppServices.TweakContext);
                AppServices.Processes.StartCaptured(@"taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
                checkBox_contex.Checked = false;
                checkBox_contex.Enabled = true;
                back_ui_6.Visible = false;
            }
        }//contex
        void back_ui_7_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.win11header"))
            {
                AppServices.TweakCatalog.Get("ui.win11header").Rollback(AppServices.TweakContext);
                checkBox_shapka.Checked = false;
                checkBox_shapka.Enabled = true;
                back_ui_7.Visible = false;
            }
        }//shapka
        void back_ui_8_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.wotbocontext"))
            {
                Registry.ClassesRoot.DeleteSubKeyTree(@"DesktopBackground\Shell\WOTBO");
                if (File.Exists(@"C:\Windows\gear.ico"))
                {
                    File.Delete(@"C:\Windows\gear.ico");
                }
                if (File.Exists(@"C:\Windows\wotbo.exe"))
                {
                    File.Delete(contexpath);
                }
                back_ui_8.Visible = false;
                checkBox_wotboincontex.Enabled = true;
            }
        }
        void back_ui_9_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.explorerrestart"))
            {
                AppServices.TweakCatalog.Get("ui.explorerrestart").Rollback(AppServices.TweakContext);
                back_ui_9.Visible = false;
                checkBox_explorer.Enabled = true;
            }
        }
        void back_ui_10_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.ffmpeg"))
            {
                AppServices.TweakCatalog.Get("ui.ffmpeg").Rollback(AppServices.TweakContext);
                back_ui_10.Visible = false;
                checkBox_ffmpeg.Enabled = true;
            }
        } //ffmpeg
        async void back_ui_11_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.mica"))
            {
                AppServices.Processes.StartCaptured($"regsvr32 /s /u C:\\Windows\\ExplorerBlurMica.dll & taskkill /f /im explorer.exe & explorer.exe & timeout /t 1 & explorer.exe");
                await Task.Delay(1000);
                File.Delete(@"C:\Windows\ExplorerBlurMica.dll");
                File.Delete(@"C:\Windows\config.ini");
                checkBox_mica.Enabled = true;
                checkBox_mica.Checked = false;
                back_ui_11.Visible = false;
            }
        }
        void back_animations_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("ui.animations"))
            {
                animation = true;
                back_animations.Visible = false;
                checkBox_animation.Enabled = true;
            }
        }
        void back_ui_tooltips_Click(object sender, EventArgs e)
        {
            RollbackTweakTooltips();
        }
        void back_gpu_1_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("gpu.directplay"))
            {
                AppServices.TweakCatalog.Get("gpu.directplay").Rollback(AppServices.TweakContext);
                back_gpu_1.Visible = false;
                checkBox_directplay.Checked = false;
                checkBox_directplay.Enabled = true;
            }
        }//directplay
        void back_gpu_2_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("gpu.hdcp"))
            {
                checkBox_hdcp.Checked = false;
                checkBox_hdcp.Enabled = true;
                AppServices.TweakCatalog.Get("gpu.hdcp").Rollback(AppServices.TweakContext);
                back_gpu_2.Visible = false;
            }
        }//hdcp
        void back_gpu_3_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("gpu.nvidia"))
            {
                AppServices.TweakCatalog.Get("gpu.nvidia").Rollback(AppServices.TweakContext);
                checkBox_dopNVIDIA_tweaks.Checked = false;
                checkBox_dopNVIDIA_tweaks.Enabled = true;
                back_gpu_3.Visible = false;
            }
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
            using (TweakScope.Rollback("pro.homegallery"))
            {

            }
        }
        void back_pro_3_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("pro.windowsupdate"))
            {
                AppServices.TweakCatalog.Get("pro.windowsupdate").Rollback(AppServices.TweakContext);
                checkBox_pro_3.Checked = false;
                checkBox_pro_3.Enabled = true;
                back_pro_3.Visible = false;
            }
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
            using (TweakScope.Rollback("pro.csrss"))
            {
                AppServices.TweakCatalog.Get("pro.csrss").Rollback(AppServices.TweakContext);
                checkBox_CSRSS.Checked = false;
                checkBox_CSRSS.Enabled = true;
                back_dop_7.Visible = false;
            }
        }
        void back_pro_9_Click(object sender, EventArgs e)
        {

        }
        void back_pro_11_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("pro.win32priority"))
            {
                AppServices.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.PriorityControl, "Win32PrioritySeparation", 0x00000002);
                checkBox_pro_11.Enabled = true;
                checkBox_pro_11.Checked = false;
                back_pro_11.Visible = false;
            }
        }

        void back_pro_12_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("pro.bufferbloat"))
            {
                AppServices.TweakCatalog.Get("pro.bufferbloat").Rollback(AppServices.TweakContext);
                checkBox_pro_12.Checked = false;
                checkBox_pro_12.Enabled = true;
                back_pro_12.Visible = false;
            }
        }
        void back_pro_13_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("pro.unlocker"))
            {
                if (Directory.Exists($"C:\\Windows\\unlocker"))
                {
                    Directory.Delete($"C:\\Windows\\unlocker", true);
                }
                Registry.ClassesRoot.DeleteSubKeyTree(@"*\shell\Уничтожить");
                Registry.ClassesRoot.DeleteSubKeyTree(@"Directory\shell\Уничтожить");
                checkBox_pro_13.Checked = false;
                checkBox_pro_13.Enabled = true;
                back_pro_13.Visible = false;
            }
        }
        async void back_pro_14_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("pro.alwaysontop"))
            {
                AppServices.Processes.StartCaptured("taskkill /f /im AlwaysOnTop.exe");
                AppServices.Registry.DeleteValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "AlwaysOnTop");
                checkBox_pro_14.Checked = false;
                checkBox_pro_14.Enabled = true;
                back_pro_14.Visible = false;
                await Task.Delay(1000);
                if (File.Exists(@"C:\Windows\AlwaysOnTop.exe"))
                {
                    File.Delete(@"C:\Windows\AlwaysOnTop.exe");
                }
            }
        }


        void back_dop_1_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("extra.iconcache"))
            {
                AppServices.TweakCatalog.Get("extra.iconcache").Rollback(AppServices.TweakContext);
                checkBox_picture_cache.Enabled = true;
                back_dop_1.Visible = false;
            }
        }

        void back_dop_2_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("extra.mobiletraffic"))
            {
                AppServices.TweakCatalog.Get("extra.mobiletraffic").Rollback(AppServices.TweakContext);
                checkBox_mobile_traffic.Enabled = true;
                back_dop_2.Visible = false;
            }
        }
        void back_dop_edge_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("extra.edge"))
            {
                Registry.LocalMachine.DeleteSubKeyTree($"SOFTWARE\\Policies\\Microsoft\\Edge");
                back_dop_edge.Visible = false;
                checkBox_edge.Checked = false;
                checkBox_edge.Enabled = true;
            }
        }
        void back_dop_3_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("extra.scoobe"))
            {
                AppServices.TweakCatalog.Get("extra.scoobe").Rollback(AppServices.TweakContext);
                checkBox_nastroyka.Enabled = true;
                back_dop_3.Visible = false;
            }
        }

        void back_dop_4_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("extra.stickykeys"))
            {
                AppServices.TweakCatalog.Get("extra.stickykeys").Rollback(AppServices.TweakContext);
                checkBox_zalipanie.Enabled = true;
                back_dop_4.Visible = false;
            }
        }
        void back_dop_6_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("extra.dwmpriority"))
            {
                AppServices.TweakCatalog.Get("extra.dwmpriority").Rollback(AppServices.TweakContext);
                checkBox_dwm.Checked = false;
                checkBox_dwm.Enabled = true;
                back_dop_6.Visible = false;
            }
        }
        void back_dop_movetemp_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("extra.movetemp"))
            {
                AppServices.TweakCatalog.Get("extra.movetemp").Rollback(AppServices.TweakContext);
                checkBox_move_temp.Checked = false;
                checkBox_move_temp.Enabled = true;
                back_dop_movetemp.Visible = false;
            }
        }

        #endregion
    }
}