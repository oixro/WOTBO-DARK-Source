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
namespace WOTBO
{
    public partial class MainForm : ShadowForm
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
        private readonly PanelReveal panelReveal;
        private Panel welcomePanel;
        private readonly Dictionary<string, TweakUiBinder> tweakBinders =
            new Dictionary<string, TweakUiBinder>(StringComparer.OrdinalIgnoreCase);
        private PcInfoView pcInfo;
        private bool _layoutReady;
        private CheckBox checkBox_tooltips;
        private PictureBox back_ui_tooltips_desc;

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

        /// <summary>Режим съёмки скриншотов: без тостов и проверки обновлений.</summary>
        public static bool ScreenshotMode { get; set; }
        #endregion
        #region default crap
        public MainForm()
        {
            var startupWatch = Stopwatch.StartNew();

            InitializeComponent();
            label_ver.Text = version;
            Log.Debug("Startup", $"InitializeComponent: {startupWatch.ElapsedMilliseconds} мс");

            AppIcon.Apply(this);
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            Dpi.Configure(PanelMain, 395f);
            Log.Debug("Startup", $"Dpi.Configure: {startupWatch.ElapsedMilliseconds} мс");

            notifier = new BalloonNotifier();
            panelReveal = new PanelReveal { Enabled = () => animation };
            FormClosed += delegate { panelReveal.Dispose(); };
            SetupTweakBinders();
            Log.Debug("Startup", $"SetupTweakBinders: {startupWatch.ElapsedMilliseconds} мс");

            ProgramLinks.Attach(this, OpenUrl);
            SetupNavigation();
            Log.Debug("Startup", $"ProgramLinks+Navigation: {startupWatch.ElapsedMilliseconds} мс");

            #region двигаем окно
            WindowDragBehavior.Attach(new List<Control> { LabelHead, PanelHead, logo });
            #endregion
            #region плавное закрытие + окраска формы
            async void Exit() { for (Opacity = 1; Opacity > .0; Opacity -= .2) await Task.Delay(7); Close(); }
            ButtonClose.Click += (s, a) => Exit();
            FormPainter.HeaderHeight = Dpi.Scale(this, 36);
            FormPainter.Apply(this, Color.FromArgb(44, 57, 67), Color.FromArgb(35, 44, 55)); //old
            Size = new System.Drawing.Size(390, 305);

            // Ресайз: рамки у окна нет, края ловит ShadowForm, а раскладку
            // пересчитываем здесь.
            Resizable = true;
            Resize += delegate { if (_layoutReady) LayoutForSize(); };
            ResizeEnd += delegate { if (pcInfo != null) pcInfo.Relayout(); };

            CenterToScreen();
            Controls.OfType<Panel>().ToList().ForEach(x => x.GetType().GetProperty("DoubleBuffered")?.SetValue(x, true));
            Log.Debug("Startup", $"painting+size: {startupWatch.ElapsedMilliseconds} мс");

            #endregion
            #region инфо о ПК
            pcInfo = new PcInfoView(panel_5, CopyPcReport);
            pcInfo.ShowLoading("Получаем информацию о ПК…");
            LoadSystemInfoAsync();
            async void LoadSystemInfoAsync()
            {
                try
                {
                    PcReport report = await AppServices.PcReport.BuildAsync();

                    this.Invoke((MethodInvoker)delegate
                    {
                        pcInfo.Render(report);
                        Log.Info("System", report.ToText());
                    });
                }
                catch (Exception ex)
                {
                    Log.Error("System", "не удалось получить информацию о ПК", ex);

                    this.Invoke((MethodInvoker)delegate
                    {
                        pcInfo.ShowError("Не удалось получить информацию о ПК");
                    });
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
                new List<Control> { label_main, label_interface, label_gpu, label_dop, label_apps, label_cleanup, label_progs, label_pc, label_site, label_winver, LabelHead, label_ver },
                Color.FromArgb(192, 192, 192),
                Color.FromArgb(255, 255, 255),
                NavRestingColor);
            #endregion
        }
        #endregion
        #region my void's
        #region анимка
        /// <summary>
        /// Открыть вкладку: сразу поставить её на место (без сдвига) и
        /// показать строки «лесенкой» — каждая следующая чуть позже.
        /// </summary>
        public void ChangePanel(Panel NewPanel)
        {
            if (NewPanel == null || NewPanel.IsDisposed) return;

            HighlightNav(NewPanel);
            HideWelcome();

            foreach (Control control in Controls)
            {
                Panel panel = control as Panel;
                if (panel == null) continue;
                if (panel == PanelMain || panel == PanelHead || panel == welcomePanel) continue;

                if (panel != NewPanel)
                {
                    panel.Visible = false;
                }
            }

            NewPanel.Bounds = ContentBounds();
            NewPanel.Visible = true;
            NewPanel.BringToFront();

            PanelScroller.Refresh(NewPanel);

            // «Инфо о ПК» пересобираем под текущую ширину панели: подписи
            // переносятся по словам и иначе остаются от прежнего размера окна.
            if (NewPanel == panel_5 && pcInfo != null)
            {
                pcInfo.Relayout();
            }

            // В режиме съёмки скриншотов анимация только мешает.
            // Списочные вкладки не анимируем построчно: у CheckedListBox
            // один контрол, а не строки-чекбоксы.
            if (!ScreenshotMode && NewPanel != panel_apps && NewPanel != panel_cleanup)
            {
                panelReveal.Play(NewPanel);
            }
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
            if (ScreenshotMode)
            {
                return;
            }

            notifier.Show(title, message, icon);
        }

        /// <summary>Отчёт о ПК в буфер обмена (кнопка на вкладке «Инфо о ПК»).</summary>
        private void CopyPcReport(string text)
        {
            try
            {
                Clipboard.SetText(text);
                ShowNotification("Информация о ПК", "Отчёт скопирован в буфер обмена", ToolTipIcon.Info);
            }
            catch (Exception exception)
            {
                Log.Error("System", "не удалось скопировать отчёт о ПК", exception);
                ShowNotification("Информация о ПК", "Не удалось скопировать отчёт", ToolTipIcon.Warning);
            }
        }

        /// <summary>
        /// Размер окна по фактическим границам PanelMain после автоскейла.
        /// Не 390×305, потому что PanelMain шире/выше и на 125–200 % край
        /// правой колонки мог обрезаться.
        /// </summary>
        private void ApplyWindowSize()
        {
            if (PanelMain == null) return;

            int minWidth = Dpi.Scale(this, 390);
            int minHeight = Dpi.Scale(this, 305);

            // Ширина фиксирована: окно тянется только по вертикали.
            int width = Math.Max(minWidth, PanelMain.Right);

            MinimumSize = new Size(width, minHeight);
            MaximumSize = new Size(width, Screen.FromControl(this).WorkingArea.Height);

            ClientSize = new Size(width, Math.Max(minHeight, PanelMain.Bottom));

            LayoutForSize();
            CenterHeader();

            _layoutReady = true;
        }

        /// <summary>Границы панели вкладки для текущего размера окна.</summary>
        private Rectangle ContentBounds()
        {
            int headHeight = Dpi.Scale(this, 36);
            int left = Dpi.Scale(this, 105);
            int rightGap = Dpi.Scale(this, 8);
            int footerGap = Dpi.Scale(this, 26);

            int height = Math.Max(Dpi.Scale(this, 100), ClientSize.Height - headHeight - footerGap);
            int width = Math.Max(Dpi.Scale(this, 200), ClientSize.Width - left - rightGap);

            return new Rectangle(left, headHeight, width, height);
        }

        /// <summary>
        /// Пересчёт раскладки под текущий размер окна: шапка на всю ширину,
        /// панели тянутся, футер прибит к низу, версия — к правому краю.
        /// Вызывается при старте, при ресайзе мышью и при смене DPI.
        /// </summary>
        private void LayoutForSize()
        {
            if (PanelMain == null || PanelHead == null) return;

            int headHeight = Dpi.Scale(this, 36);
            int left = Dpi.Scale(this, 105);
            int rightGap = Dpi.Scale(this, 8);
            int footerGap = Dpi.Scale(this, 26);

            // Края окна должны оставаться «голой» формой: шапка и панель
            // перекрывают WM_NCHITTEST, и тогда мышью их не ухватить.
            int edge = Dpi.Scale(this, 6);

            PanelHead.Location = new Point(0, edge);
            PanelHead.Width = ClientSize.Width;
            PanelHead.Height = Math.Max(Dpi.Scale(this, 24), headHeight - edge);

            // По бокам отступ больше не нужен: горизонтального ресайза нет.
            PanelMain.Location = new Point(0, headHeight);
            PanelMain.Width = ClientSize.Width;
            PanelMain.Height = Math.Max(Dpi.Scale(this, 120), ClientSize.Height - headHeight - edge);

            int contentWidth = Math.Max(Dpi.Scale(this, 200), ClientSize.Width - left - rightGap);
            int contentHeight = Math.Max(Dpi.Scale(this, 100), PanelMain.Height - footerGap);

            Panel[] panels = { panel1, panel_ui_1, panel_dop, panel2, panel_sorry, panel_5, panel_settings, panel_apps, panel_cleanup };
            foreach (Panel panel in panels)
            {
                if (panel == null) continue;

                panel.Location = new Point(left, headHeight);
                panel.Size = new Size(contentWidth, contentHeight);
            }

            if (welcomePanel != null)
            {
                welcomePanel.Location = new Point(left, headHeight);
                welcomePanel.Size = new Size(contentWidth, contentHeight);
            }

            // Футер: кнопка и подписи привязаны к низу панели.
            button_new.Top = PanelMain.Height - Dpi.Scale(this, 52);
            label_winver.Top = PanelMain.Height - Dpi.Scale(this, 19);
            label_ver.Top = PanelMain.Height - Dpi.Scale(this, 17);
            label_ver.Left = Math.Max(0, PanelMain.Width - label_ver.Width - Dpi.Scale(this, 8));

            ButtonClose.Left = Math.Max(0, PanelHead.Width - ButtonClose.Width - Dpi.Scale(this, 5));

            CenterHeader();
            LayoutGear();

            // После изменения размеров перерисовываем всё: иначе остаются
            // куски старого градиента на месте прежней панели.
            Invalidate(true);
        }

        /// <summary>
        /// Ставит иконку и название по центру шапки. Позиции в дизайнере
        /// фиксированные, поэтому после DPI-масштабирования их нужно
        /// пересчитать.
        /// </summary>
        private void CenterHeader()
        {
            if (PanelHead == null || logo == null || LabelHead == null) return;

            int gap = Dpi.Scale(this, 4);
            int total = logo.Width + gap + LabelHead.Width;
            int left = (PanelHead.ClientSize.Width - total) / 2;
            if (left < 0) left = 0;

            logo.Left = left;
            LabelHead.Left = left + logo.Width + gap;

            logo.Top = (PanelHead.ClientSize.Height - logo.Height) / 2;
            LabelHead.Top = (PanelHead.ClientSize.Height - LabelHead.Height) / 2;
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);

            if (!IsHandleCreated || PanelMain == null) return;

            Dpi.RefreshReference(PanelMain, 395f);
            FormPainter.HeaderHeight = Dpi.Scale(this, 36);
            ApplyWindowSize();
            CenterHeader();
            CenterToScreen();
            Log.Debug("UI", $"DPI changed {e.DeviceDpiOld}->{e.DeviceDpiNew}, client={ClientSize.Width}x{ClientSize.Height}");
        }

        #endregion
        #region закрытие
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
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
    }
}