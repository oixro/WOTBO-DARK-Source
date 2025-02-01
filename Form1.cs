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
using WOTBO.Properties;
namespace Project
{
    public partial class WOTBO : FormShadow
    {
        #region переменные
        string version = $"build - {Assembly.GetExecutingAssembly().GetName().Version.ToString(2)}";
        bool win10;
        static string tempfolder = @"c:\Windows\oixro";
        static string path_regpack7z = tempfolder + @"\regpack.zip";
        static string path_regpack = tempfolder + @"\regpack";
        static string path_services = tempfolder + @"\services";
        static string path_uizip = tempfolder + @"\ui.zip";
        static string path_ui = tempfolder + @"\ui";
        static string path_dfkiller = tempfolder + @"\DFKiller";
        static string path_scheme = tempfolder + @"\scheme.pow";
        static string smartctl = tempfolder + @"\smartctl.exe";
        string curver = (Assembly.GetExecutingAssembly().GetName().Version.ToString(2)).Trim();
        string exename = AppDomain.CurrentDomain.FriendlyName;
        string exepath = Assembly.GetEntryAssembly().Location;
        string contexpath = @"C:\Windows\wotbo.exe";
        int mmMemory = 0;
        public string uilanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        string logfile = $@"{Environment.ExpandEnvironmentVariables("%temp%")}\wotbo.log";
        public static string gpu;
        public bool nvidia;
        public static Process getReservedStorage = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = "/c chcp 65001 && DISM /Online /Get-ReservedStorageState",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
        });
        static public string ReservedStorage = (getReservedStorage.StandardOutput.ReadToEnd().Trim());
        public static long capacity;
        static string path_NVidiaProfileInspectorDmW = tempfolder + @"\NVidiaProfileInspectorDmW";
        private NotifyIcon notifyIcon;

        public static class SysFolder
        {
            public static string C = Environment.ExpandEnvironmentVariables("%systemdrive%");
            public static string Temp = Environment.ExpandEnvironmentVariables("%temp%");
            public static string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            public static string Local = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Local";
            public static string Roaming = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Roaming";
            public static string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            public static string Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            public static string System32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            public static string SysWOW64 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            public static string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            public static string ProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            public static string ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
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
                await Task.Run(() =>
                {
                    try
                    {
                        string cpuModel = GetStringFromWMI("Win32_Processor", "Name");
                        double cpuFrequency = GetDoubleFromWMI("Win32_Processor", "MaxClockSpeed") / 1000.0;
                        string gpuModel = GetStringFromWMI("Win32_VideoController", "Name");
                        string ramSize = GetRAMSize();
                        string screenInfo = GetScreenInfo();
                        string ramFrequency = GetRAMFrequency();
                        string motherBoard = GetStringFromWMI("Win32_BaseBoard", "Product");

                        this.Invoke((MethodInvoker)delegate
                        {
                            label7.Text = $"CPU: {cpuModel.Trim()}\n" +
                                          $"CPU base clock: {cpuFrequency} GHz\n" +
                                          $"MB: {motherBoard.Trim()}\n" +
                                          $"GPU: {gpuModel.Trim()}\n" +
                                          $"ОЗУ: {ramSize.Trim()} {ramFrequency.Trim()}\n" +
                                          $"Display: {screenInfo.Trim()}\n" +
                                          $"OS: {label_winver.Text}";
                            writelog(label7.Text);
                        });
                    }
                    catch
                    {
                        label7.Text = "Произошла ошибка при получении информации о пк";
                    }
                });
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

                        writelog("INSANE MODE!");
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
        #region new method of WMI
        string GetStringFromWMI(string className, string propertyName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM {className}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj[propertyName]?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                return "";
            }
            return "";
        }
        private double GetDoubleFromWMI(string className, string propertyName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM {className}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return Convert.ToDouble(obj[propertyName]);
                    }
                }
            }
            catch
            {
                return -1.0;
            }
            return -1.0;
        }
        string GetRAMSize()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ulong totalMemory = (ulong)obj["TotalPhysicalMemory"];
                        return $"{Math.Round((double)totalMemory / (1024 * 1024 * 1024), 0)} GB";
                    }
                }
            }
            catch
            {
                return "";
            }
            return "";
        }
        string GetRAMFrequency()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        uint speed = (uint)obj["Speed"];
                        if (speed > 0)
                        {
                            return $"{speed} MHz";
                        }
                    }
                }
            }
            catch
            {
                return "";
            }
            return "";
        }
        string GetScreenInfo()
        {
            try
            {
                Screen primaryScreen = Screen.PrimaryScreen;
                if (primaryScreen != null)
                {
                    var bounds = primaryScreen.Bounds;
                    int refreshRate = GetRefreshRate(bounds);
                    return $"{bounds.Width}x{bounds.Height} {refreshRate} Hz";
                }
            }
            catch
            {
                return "";
            }
            return "";
        }
        int GetRefreshRate(System.Drawing.Rectangle bounds)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int refreshRate = GetDeviceCaps(hdc, 116); // VREFRESH
            ReleaseDC(IntPtr.Zero, hdc);
            return refreshRate;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        #endregion
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
        public void hcmd(string line)
        {
            try
            {
                writelog("[CMD] Command: " + line);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c chcp 65001 & {line}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process proc = new Process())
                {
                    proc.StartInfo = psi;

                    proc.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            writelog("[CMD] Output: " + e.Data);
                        }
                    };

                    proc.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            writelog("[CMD] Error: " + e.Data);
                        }
                    };

                    proc.Start();

                    proc.BeginOutputReadLine();
                    proc.BeginErrorReadLine();
                }
            }
            catch (Exception ex)
            {
                writelog("[CMD] Error: " + ex.Message);
            }
        }
        public async Task hcmdAsync(string line)
        {
            await Task.Run(() =>
            {
                hcmd(line); // Вызов вашей команды внутри Task.Run
            });
        }
        void scmd(string line)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {line}"
                });
            }
            catch { }
        }
        void powershell(string line)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"/command {line}",
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch { }
        }
        void supercmd(string line)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $@"/c {tempfolder}\su.exe /wrs cmd.exe /c {line}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true // Скрыть окно CMD
                };

                using (Process proc = new Process())
                {
                    proc.StartInfo = psi;
                    proc.Start();

                    // Читаем стандартный вывод и ошибки
                    string output = proc.StandardOutput.ReadToEnd();
                    string error = proc.StandardError.ReadToEnd();
                    proc.WaitForExit();

                    // Объединяем вывод и ошибки
                    string result = output + error;

                    // Выводим результат через функцию writelog
                    writelog(result);
                }
            }
            catch (Exception ex)
            {
                writelog("[CMD] Произошла ошибка: " + ex.Message);
            }
        }
        void writelog(string line)
        {
            // Запускаем асинхронную запись в лог
            Task.Run(() =>
            {
                lock (logLock) // Гарантия потокобезопасности
                {
                    using (StreamWriter logs = new StreamWriter(logfile, true, Encoding.UTF8))
                    {
                        // Получение текущего времени
                        DateTime now = DateTime.Now;

                        // Форматирование времени с миллисекундами
                        string formattedTime = now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                        logs.WriteLine($"{formattedTime} - {line}");
                    }
                }
            });
        }
        public void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url));
            }
            catch
            {
                MessageBox.Show($"Не удалось открыть ссылку. \nВозможно у вас не установлен браузер по умолчанию.", "WOTBO");
            }
        }

        // Глобальный объект для блокировки
        private static readonly object logLock = new object();

        void CheckBox_CheckedChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.CheckBox checkBox = sender as System.Windows.Forms.CheckBox;
            if (checkBox != null)
            {
                writelog($"{checkBox.Text} ({checkBox.Name}) изменен на {checkBox.CheckState}");
            }
        }
        void PictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            // Обработчик события Click для PictureBox
            PictureBox clickedPictureBox = sender as PictureBox;
            if (clickedPictureBox != null)
            {
                // Здесь можно написать код для выполнения действия при клике на картинку
                writelog($"{clickedPictureBox.Name} нажат");
            }
        }
        void Label_MouseUp(object sender, MouseEventArgs e)
        {
            // Обработчик события Click для PictureBox
            System.Windows.Forms.Label Lbl = sender as System.Windows.Forms.Label;
            if (Lbl != null)
            {
                // Здесь можно написать код для выполнения действия при клике на картинку
                writelog($"Label {Lbl.Text} был нажат");
            }
        }
        void Button_MouseUp(object sender, MouseEventArgs e)
        {
            // Обработчик события Click для PictureBox
            Button btn = sender as Button;
            if (btn != null)
            {
                // Здесь можно написать код для выполнения действия при клике на картинку
                writelog($"Button {btn.Text},{btn.Name} был нажат");
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
        async void Form1_Load(object sender, EventArgs e)
        {
            writelog($"==============WOTBO {version} ==============");
            #region проверка запущенна ли + eula
            if (!InstanceChecker.TakeMemory())
            {
                writelog("InstanceExists, relaunch");
                hcmd($"taskkill /f /im {exename} && \"{exepath}\"");
                writelog("cmd на перезапуск вызван, ждём когда приебашит меня");
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("eula") == null)
            {
                writelog("eula null");
                Form2 eula = new Form2();
                eula.ShowDialog();
            }
            #endregion
            #region проверка версии Windows
            using (RegistryKey winver = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
            {
                string buildnumber = winver.GetValue("CurrentBuild") as string;
                string displayVersion = winver.GetValue("DisplayVersion") as string;

                if (buildnumber != null && displayVersion != null)
                {
                    ushort buildNumberValue = Convert.ToUInt16(buildnumber);

                    if (buildNumberValue >= 22000) // Windows 11
                    {
                        win10 = false;
                        writelog($"Windows is win 10? {win10}");
                        label_winver.Text = $"Windows 11 {displayVersion} Build:{buildnumber}";
                    }
                    else if (buildNumberValue <= 19042) // Windows 10 < 20H2
                    {
                        writelog($"Windows 10 <20H2 ");
                        win10 = true;
                        MessageBox.Show("Рекомендуется использовать Windows 10 20H2 и выше! \nЭффективность настройки не гарантирована",
                            "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        label_winver.Text = $"(Old) Windows 10 {displayVersion} Build:{buildnumber}";
                    }
                    else if (buildNumberValue > 19042) // Windows 10 >= 20H2
                    {
                        writelog($"Windows 10 {buildnumber} ");
                        win10 = true;
                        label_winver.Text = $"Windows 10 {displayVersion} Build:{buildnumber}";
                    }
                    else
                    {
                        MessageBox.Show("Программа предназначена для Windows 10 и 11!\nРабота на других системах не поддерживается!", "Windows optimization tool by oixro (WOTBO)",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
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
                    writelog($"чеки под win 11 для win10 отключены");
                }
            }
            #endregion
            #region temp
            if (Directory.Exists(tempfolder))
            {
                writelog($"tempfolder - \"{tempfolder}\" exists");
                string[] files = Directory.GetFiles(tempfolder);
                writelog($"пытаюсь всё удалить из {tempfolder}");
                try
                {
                    Directory.Delete(tempfolder, true);
                    writelog($"Удалена папка: {tempfolder}");
                }
                catch (Exception ex)
                {
                    writelog($"не удалось очистить {tempfolder}");
                    writelog(ex.Message);
                    tempfolder = @"c:\Windows\oixro" + $"_{Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())).Remove(5)}"; writelog($"я теперь тут ошиваюсь - {tempfolder}");
                    path_regpack7z = tempfolder + @"\regpack.zip"; writelog($"new path_regpack7z - {path_regpack7z}");
                    path_regpack = tempfolder + @"\regpack"; writelog($"new path_regpack - {path_regpack}");
                    path_services = tempfolder + @"\services"; writelog($"new path_services - {path_services}");
                    path_uizip = tempfolder + @"\ui.zip"; writelog($"new path_uizip - {path_uizip}");
                    path_ui = tempfolder + @"\ui"; writelog($"new path_ui - {path_ui}");
                    path_dfkiller = tempfolder + @"\DFKiller"; writelog($"new path_dfkiller - {path_dfkiller}");
                    path_scheme = tempfolder + @"\scheme.pow"; writelog($"new path_scheme - {path_scheme}");
                    smartctl = tempfolder + @"\smartctl.exe"; writelog($"new smartctl - {smartctl}");
                }
            }
            else
            {
                writelog("tempfolder нету");
            }

            #endregion
            #region отключаем чеки
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("regpack")) == 1)
            {
                writelog("regpack был применён");
                checkBox_reg.Enabled = false;
                back_main_1.Visible = true;
            }//regpack
            if (ReservedStorage.Contains("Reserved storage is disabled.") | ReservedStorage.Contains("‡ аҐ§ҐаўЁа®ў ­­®Ґ еа ­Ё«ЁйҐ ®вЄ«озҐ­®."))
            {
                writelog("ReservedStorage был применён");
                checkBox_dism.Enabled = false;
                back_main_2.Visible = true;
            } //old reserved
            if (!File.Exists(@"C:\hiberfil.sys"))
            {
                writelog("hiberfil'a нету");
                checkBox_gibernate.Enabled = false;
                back_main_3.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("imported_powerscheme")) == 1)
            {
                writelog("imported_powerscheme был применён");
                checkBox_scheme.Enabled = false;
                back_main_4.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("animations")) == 1)
            {
                animation = false;
                writelog("анимации выключены");
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
                    writelog("Чекбокс отключен: параметр mousefix существует в реестре.");
                }
                else if (IsScale100Percent())
                {
                    // Если ключ "mousefix" не существует и масштаб равен 100% - включаем чекбокс
                    checkBox_mousefix.Enabled = true;
                    writelog("Чекбокс включен: параметр mousefix не существует, и масштаб равен 100%.");
                }
                else
                {
                    // Если ключ "mousefix" не существует и масштаб не равен 100% - отключаем чекбокс
                    checkBox_mousefix.Enabled = false;
                    writelog("Чекбокс отключен: параметр mousefix не существует, и масштаб не равен 100%.");
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
                const string registryPath = @"Software\oixro\wotbo";

                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            // Проверяем, существует ли значение "mousefix" в ключе реестра
                            object value = key.GetValue("mousefix");
                            return value != null; // Возвращаем true, если значение существует
                        }
                    }
                }
                catch (Exception ex)
                {
                    writelog($"Ошибка при чтении реестра: {ex.Message}");
                }

                return false; // Возвращаем false, если ключ или значение не найдены
            }
            UpdateMouseFixCheckBox();
            #endregion
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm").GetValue("OverlayTestMode")) == 5)
            {
                writelog("MPO был применён");
                checkBox_mpo.Enabled = false;
                back_main_6.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("usbpowersaving")) == 1)
            {
                writelog("usbpowersaving был применён");
                checkBox_usb.Enabled = false;
                back_main_7.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("bcdedit")) == 1)
            {
                writelog("bcdedit был применён, откатываем нахуй");
                hcmd("bcdedit /deletevalue tscsyncpolicy");
                hcmd("bcdedit /deletevalue bootux");
                hcmd("bcdedit /set bootmenupolicy standard");
                hcmd("bcdedit /set hypervisorlaunchtype Auto");
                hcmd("bcdedit /deletevalue tpmbootentropy");
                hcmd("bcdedit /deletevalue quietboot");
                hcmd("bcdedit /set nx optin");
                hcmd("bcdedit /set allowedinmemorysettings 0x17000077");
                hcmd("bcdedit /set isolatedcontext Yes");
                hcmd("bcdedit /deletevalue vsmlaunchtype");
                hcmd("bcdedit /deletevalue vm");
                hcmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\FVE\" /v \"DisableExternalDMAUnderLock\" /f");
                hcmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"EnableVirtualizationBasedSecurity\" /f");
                hcmd("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"HVCIMATRequired\" /f");
                hcmd("bcdedit /deletevalue firstmegabytepolicy");
                hcmd("bcdedit /deletevalue avoidlowmemory");
                hcmd("bcdedit /deletevalue nolowmem");
                hcmd("bcdedit /deletevalue configaccesspolicy");
                hcmd("bcdedit /deletevalue x2apicpolicy");
                hcmd("bcdedit /deletevalue usephysicaldestination");
                hcmd("bcdedit /deletevalue usefirmwarepcisettings");
                hcmd("bcdedit /deletevalue uselegacyapicmode");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("bcdedit");
            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management")?.GetValue("FeatureSettings")) == 1)
            {
                writelog("spectre and meltdown был применён");
                checkBox_mtiititit.Enabled = false;
                back_main_9.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("mmagent")) == 1)
            {
                writelog("mmagent был применён");
                checkBox_mmagent.Enabled = false;
                back_main_10.Visible = true;
            }
            #region pagefile
            //writelog("pagefile был применён");
            try
            {
                // Открываем раздел реестра
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"))
                {
                    if (key != null)
                    {
                        // Читаем текущее значение PagingFiles
                        object value = key.GetValue("PagingFiles");
                        if (value != null)
                        {
                            string[] pagingFiles = (string[])value;

                            // Флаг для отслеживания соответствия параметров
                            bool settingsMatch = false;

                            // Проходим по всем настройкам файла подкачки
                            foreach (string pagingFile in pagingFiles)
                            {
                                // Разбираем строку настроек
                                string[] parts = pagingFile.Split(' ');
                                if (parts.Length >= 3)
                                {
                                    string path = parts[0];
                                    int minSize = int.Parse(parts[1]);
                                    int maxSize = int.Parse(parts[2]);

                                    // Проверяем, соответствуют ли размеры заданным
                                    if (path.Equals(@"C:\pagefile.sys", StringComparison.OrdinalIgnoreCase)
                                        && minSize == 16 && maxSize == 32768)
                                    {
                                        settingsMatch = true;
                                        break;
                                    }
                                }
                            }

                            if (settingsMatch)
                            {
                                writelog("[pagefile check] Параметры файла подкачки уже установлены.");
                                checkBox_page.Enabled = false;
                                back_main_11.Visible = true;
                            }
                            else
                            {
                                writelog("[pagefile check] Параметры файла подкачки отличаются от заданных.");
                            }
                        }
                        else
                        {
                            writelog("[pagefile check] Значение PagingFiles не установлено.");
                        }
                    }
                    else
                    {
                        writelog("[pagefile check] Не удалось открыть ключ реестра.");
                    }
                }
            }
            catch (Exception ex)
            {
                writelog("[pagefile check] Произошла ошибка: " + ex.Message);
            }


            #endregion
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("buttons")) == 1)
            {
                writelog("buttons был применён");
                checkBoxUI_Buttons_1.Enabled = false;
                back_ui_1.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("litle_explorer_things")) == 1)
            {
                writelog("litle_explorer_things был применён");
                checkBoxUI_Buttons_2.Enabled = false;
                back_ui_2.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop").GetValue("JPEGImportQuality")) == 100)
            {
                writelog("JPEGImportQuality был применён");
                checkBoxUI_Buttons_4.Enabled = false;
                back_ui_4.Visible = true;
            } //wallpapers
            if (Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32") != null)
            {
                writelog("contex11 был применён");
                checkBox_contex.Enabled = false;
                back_ui_6.Visible = true;
            } //contex11
            try
            {
                if (Registry.ClassesRoot.OpenSubKey(@"CLSID\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}\InProcServer32").GetValue("").ToString().Contains("FixByVlado"))
                {
                    writelog("shapka11 был применён");
                    checkBox_shapka.Enabled = false;
                    back_ui_7.Visible = true;
                }//shapka11
            }
            catch
            {

            } //shapka11
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pro_1")) == 1)
            {
                writelog("pro_1 был применён");
                checkBox_pro_1.Enabled = false;

            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\PriorityControl").GetValue("Win32PrioritySeparation")) != 0x00000002)
            {
                writelog("Win32PrioritySeparation был применён");
                checkBox_pro_11.Enabled = false;
                back_pro_11.Visible = true;
            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU")?.GetValue("NoAutoUpdate")) == 1)
            {
                writelog("NoAutoUpdate был применён");
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;

            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("defenderdisabled")) == 1)
            {
                writelog("defenderdisabled был применён");
                checkBox_disabledefender.Enabled = false;
                label_delete_defender.Enabled = true;
                back_main_12.Visible = true;
                label_kalorant.Visible = true;
            }
            if (File.Exists($@"C:\Windows\System32\imageres.dll_bak"))
            {
                writelog("синие папки win 10 были применены");
                checkBox_bluefolders.Enabled = false;
                back_ui_5.Visible = true;
            }
            if (File.Exists($@"C:\Windows\SystemResources\imageres.dll.mun_bak"))
            {
                writelog("синие папки win 11 были применены");
                checkBox_bluefolders.Enabled = false;
                back_ui_5.Visible = true;
            }
            if (Registry.ClassesRoot.OpenSubKey(@"DesktopBackground\Shell\WOTBO")?.GetValue("") != null)
            {
                writelog("wotbo in contex был применён");
                checkBox_wotboincontex.Enabled = false;
                back_ui_8.Visible = true;
            }
            if (!Directory.Exists(@"C:\ProgramData\Microsoft\Windows Defender"))
            {
                writelog("defender удалён");
                //checkBox_disabledefender.Visible = false;
                checkBox_disabledefender.Enabled = false;
                back_main_12.Visible = false;
            }
            if (!File.Exists($@"{SysFolder.UserProfile}\AppData\Local\Microsoft\OneDrive\OneDrive.exe"))
            {
                writelog("OneDrive был удалён");
                checkBox_onedrive.Enabled = false;
            }
            if (exepath == contexpath)
            {
                writelog("запущены из конткестного");
                back_ui_8.Visible = false;
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("buffer") != null)
            {
                writelog("bufferbloat был применён");
                checkBox_pro_12.Enabled = false;
                back_pro_12.Visible = true;
            }
            if (File.Exists(@"C:\Windows\unlocker\Unlocker.exe"))
            {
                writelog("unclockewr был применён");
                checkBox_pro_13.Enabled = false;
                back_pro_13.Visible = true;
            }
            if (File.Exists(@"C:\Windows\AlwaysOnTop.exe"))
            {
                writelog("AlwaysOnTop был применён");
                checkBox_pro_14.Enabled = false;
                back_pro_14.Visible = true;
            }
            if (Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\SystemFileAssociations\.mp4\shell\FFmpeg")?.GetValue("icon") != null)
            {
                writelog("FFmpeg был применён");
                checkBox_ffmpeg.Enabled = false;
                back_ui_10.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true)?.GetValue("Max Cached Icons") != null)
            {
                writelog("Max Cached Icons был применён");
                checkBox_picture_cache.Enabled = false;
                back_dop_1.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true)?.GetValue("DefaultTTL") != null)
            {
                writelog("DefaultTTL был применён");
                checkBox_mobile_traffic.Enabled = false;
                back_dop_2.Visible = true;
            }
            if (Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement", true)?.GetValue("ScoobeSystemSettingEnabled") != null)
            {
                writelog("ScoobeSystemSettingEnabled был применён");
                checkBox_nastroyka.Enabled = false;
                back_dop_3.Visible = true;
            }
            if (Convert.ToString(Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys", true)?.GetValue("Flags")) == "506")
            {
                writelog("StickyKeys был применён");
                checkBox_zalipanie.Enabled = false;
                back_dop_4.Visible = true;
            }
            if (File.Exists(@"C:\Windows\ExplorerBlurMica.dll"))
            {
                writelog("ExplorerBlurMica был применён");
                checkBox_mica.Enabled = false;
                back_ui_11.Visible = true;
            }
            if (Registry.ClassesRoot.OpenSubKey(@"DesktopBackground\Shell\explorer\") != null)
            {
                writelog("restart explorer был применён");
                checkBox_explorer.Enabled = false;
                back_ui_9.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\dwm.exe") != null)
            {
                writelog("dwm.exe priority был применён");
                checkBox_dwm.Enabled = false;
                back_dop_6.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe") != null)
            {
                writelog("csrss.exe priority был применён");
                checkBox_CSRSS.Enabled = false;
                back_dop_7.Visible = true;
            }
            if (Convert.ToString(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\BackgroundModel\BackgroundAudioPolicy")?.GetValue("AllowHeadlessExecution")) == "1")
            {
                writelog("AllowHeadlessExecution BackgroundAudioPolicy был применён");
                checkBox_audio.Enabled = false;
                back_main_13.Visible = true;
            }
            if (Convert.ToString(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows")?.GetValue("DwmInputUsesIoCompletionPort")) == "0")
            {
                writelog("DwmInputUsesIoCompletionPort был применён");
                checkBox_dwninput.Enabled = false;
                back_main_14.Visible = true;
            }
            if (Convert.ToString(Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power")?.GetValue("ExitLatency")) == "1")
            {
                writelog("Power\\ExitLatency был применён");
                checkBox_tolerate.Enabled = false;
                back_main_16.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Policies\Microsoft\Edge") != null)
            {
                writelog("Edge был применён");
                back_dop_edge.Visible = true;
                checkBox_edge.Checked = false;
                checkBox_edge.Enabled = false;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU") != null)
            {
                writelog("WindowsUpdate\\AU был применён");
                checkBox_pro_3.Checked = false;
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;
            }
            if (Convert.ToString(Registry.CurrentUser.OpenSubKey("Environment")?.GetValue("TEMP")) == $@"{Environment.ExpandEnvironmentVariables("%systemdrive%")}\Temp")
            {
                checkBox_move_temp.Enabled = false;
                back_dop_movetemp.Visible = true;
            }
            #endregion
            #region инет + обнова
            if (!Internet.OK())
            {
                writelog($"инета нету");
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
                    using (WebClient wc = new WebClient())
                    {
                        if (Internet.OK())
                        {
                            string verjson = (wc.DownloadString("https://raw.githubusercontent.com/oixro/WOTBO/main/ver.json").Trim());
                            if (Convert.ToDouble(curver, CultureInfo.InvariantCulture) == (Convert.ToDouble(verjson, CultureInfo.InvariantCulture)))
                            {
                                writelog($"версия актуальная");
                                label_ver.Text = $"{version}";
                            }
                            else
                            {
                                if (Convert.ToDouble(curver, CultureInfo.InvariantCulture) <= (Convert.ToDouble(verjson, CultureInfo.InvariantCulture)))
                                {
                                    wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/lastchange.json", "lastchange.json");

                                    MessageBox.Show($"Текущая версия - {curver}\nДоступна новая - {verjson}\nБудет выполнено обновление\n\n" +
                                    $"Список изменений:\n {File.ReadAllText("lastchange.json")}", "", MessageBoxButtons.OK);

                                    writelog($"скачиваем новую");
                                    wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/WOTBO.exe", "new.exe");
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
                                }
                            }
                            if (Convert.ToDouble(curver, CultureInfo.InvariantCulture) > (Convert.ToDouble(verjson, CultureInfo.InvariantCulture)))
                            {
                                writelog($"версия dev");
                                label_ver.Text = $"d{version}";
                            }

                        }
                    }
                }
                catch { } //release
            }
            #endregion
            #region позиция, размеры и плавный запуск
            for (Opacity = 0; Opacity < 1; Opacity += .1) await Task.Delay(7);
            writelog($"плавно открылись");
            #endregion
            #region получаем инфу о видеодырке
            #region old method
            foreach (var mo in new ManagementObjectSearcher("select * from win32_VideoController").Get())
                gpu = (string)mo["name"];
            if (gpu.Contains("NVIDIA"))
            {
                nvidia = true;
                writelog($"GPU IS NVIDIA? - {nvidia}, gpu is - {gpu}");
                if (Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000")?.GetValue("RMHdcpKeyglobZero") != null)
                {
                    writelog($"hdcp disabled");
                    checkBox_hdcp.Checked = false;
                    checkBox_hdcp.Enabled = false;
                    back_gpu_2.Visible = true;
                } //hdcp check
                if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak", true)?.GetValue("DisplayPowerSaving") != null)
                {
                    writelog($"DisplayPowerSaving disabled");
                    checkBox_dopNVIDIA_tweaks.Enabled = false;
                    back_gpu_3.Visible = true;
                }//tweaksnvidia
                try
                {
                    string[] ansel = Directory.GetFiles(@"C:\Windows\System32\DriverStore\", "NvCameraEnable.exe", SearchOption.AllDirectories);
                    if (ansel.Length > 0)
                    {
                        string anselPath = ansel[0];
                        Process getanselstate = Process.Start(new ProcessStartInfo
                        {
                            FileName = $"{anselPath}",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                        });
                        string anselstate = (getanselstate.StandardOutput.ReadToEnd().Trim());
                        if (anselstate == "0")
                        {
                            writelog($"anselstate == \"0\"");
                            checkBox_ansel.Enabled = false;
                            back_gpu_4.Visible = true;
                        }
                        else
                        {
                            checkBox_ansel.Enabled = true;
                        }
                    }
                }
                catch { }//ansel
            }
            else
            {
                writelog("не nvidia");
                nvidia = false;
                checkBox_ansel.Visible = false;
                checkBox_dopNVIDIA_tweaks.Visible = false;
                checkBox_hdcp.Visible = false;
                label_inspector.Visible = false;
                checkBox_directplay.Location = new Point(20, 45);
                back_gpu_1.Location = new Point(0, 45);
            }//не nvidia
            #endregion
            #endregion
            #region распаковка в temp
            writelog("распаковка в temp");
            Directory.CreateDirectory(tempfolder);
            writelog($"tempfolder создан - {tempfolder}");
            File.WriteAllBytes(path_regpack7z, Resources.regpack);
            writelog($"path_regpack7z создан - {path_regpack7z}");
            if (!Directory.Exists(path_regpack))
                ZipFile.ExtractToDirectory($"{path_regpack7z}", $"{path_regpack}");
            writelog($"path_regpack7z распакован - {path_regpack}");
            File.WriteAllBytes(tempfolder + @"\su.exe", Resources.su);
            writelog($"su.exe распакован");
            File.WriteAllText(tempfolder + @"\Audio_Lantency.reg", Resources.Audio_Lantency);
            writelog($"Audio_Lantency.reg распакован");
            File.WriteAllText(tempfolder + @"\Audio_Lantency_delete.reg", Resources.Audio_Lantency_delete);
            writelog($"Audio_Lantency_delete.reg распакован");
            File.WriteAllText(tempfolder + @"\updates.reg", Resources.updates);
            writelog($"updates.reg распакован");
            if (!Directory.Exists(path_ui))
            {
                File.WriteAllBytes(path_uizip, Resources.ui);
                ZipFile.ExtractToDirectory($"{path_uizip}", $"{path_ui}");
                writelog($"ui.zip распакован - {path_ui}");
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
                    writelog($"BackgroundMonitoringServices был найден - {processes.Length}");
                    hcmd("taskkill /f /im BackgroundMonitoringServices.exe");
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
            writelog("closing");
            if (Directory.Exists(tempfolder))
            {
                try
                {
                    writelog($"удаляем tempfolder -  \"{tempfolder}\"");
                    Directory.Delete(tempfolder, true);
                }
                catch
                {
                    writelog("освобождаем память и убиваемся");
                    InstanceChecker.ReleaseMemory();
                    hcmd($"taskkill /f /im {exename}");
                }
            }
            InstanceChecker.ReleaseMemory();
        }
        #endregion
        #region все твики
        async void button1_Click(object sender, EventArgs e)
        {
            #region main
            if (checkBox_disabledefender.Checked)
            {

                MessageBox.Show("Защитник будет отключен!" +
                    "\nЗащита от эксполитов не будет работать." +
                    "\nОна нужна для работы некоторых античитов." +
                    "\nНо восстановить защитник можно через вкладку дополнительно.\n" +
                    "", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                #region new method
                if (win10)
                {
                    supercmd($"regedit.exe /s {path_regpack}/w10/win10defenderX.reg");
                }
                else
                {
                    supercmd($"regedit.exe /s {path_regpack}/w11/win11defenderX.reg");
                    #region webthreatdefusersvc_XXXXX
                    // Путь к основной ветке реестра
                    string baseKeyPath = @"SYSTEM\CurrentControlSet\Services";

                    // Открываем основную ветку реестра
                    using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(baseKeyPath))
                    {
                        if (baseKey != null)
                        {
                            // Ищем подветку с нужным префиксом
                            foreach (string subKeyName in baseKey.GetSubKeyNames())
                            {
                                if (subKeyName.StartsWith("webthreatdefusersvc_"))
                                {
                                    // Открываем найденную ветку
                                    using (RegistryKey targetKey = baseKey.OpenSubKey(subKeyName, true))
                                    {
                                        if (targetKey != null)
                                        {
                                            // Устанавливаем параметры в ветке
                                            targetKey.SetValue("LaunchProtected", "00000004", RegistryValueKind.DWord);
                                            targetKey.SetValue("Start", "00000004", RegistryValueKind.DWord);
                                            targetKey.SetValue("Start_old", "00000004", RegistryValueKind.DWord);
                                            writelog($"Параметр установлен в ветке {subKeyName}");
                                        }
                                    }
                                    break; // Предполагаем, что нужная ветка одна, и можно прекратить поиск
                                }
                            }
                        }
                    }
                    #endregion
                }
                supercmd($"regedit.exe /s {path_regpack}/antimalwareserviceexecutable/!AIO_OFF.reg");
                checkBox_disabledefender.Enabled = false;
                checkBox_disabledefender.Checked = false;
                label_kalorant.Visible = true;
                back_main_12.Visible = true;
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("defenderdisabled", 1);
                ShowNotification("Защитник", "Рекомендуется перезагрузить компьютер для полноценного отключения защитника!", ToolTipIcon.Warning);
                #endregion
            }
            if (checkBox_reg.Checked)
            {
                if (win10)
                {
                    hcmd($"regedit.exe /s {path_regpack}/w10/w10_AIO.reg");
                }
                else
                {
                    supercmd($"regedit.exe /s {path_regpack}/w11/w11_AIO.reg");
                }
                supercmd($"regedit.exe /s {path_regpack}/foldernetworkX.reg");
                hcmd($"regedit.exe /s {path_regpack}/REG_AIO.reg");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("regpack", 1);
                RegistryKey key;
                key = Registry.CurrentUser.CreateSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\VideoSettings");
                key?.SetValue("EnableAutoEnhanceDuringPlayback", 0x00000000, RegistryValueKind.DWord);
                key.Close();
                checkBox_reg.Enabled = false;
                checkBox_reg.Checked = false;
                back_main_1.Visible = true;
                ShowNotification("Настройки Реестра", "Рекомендуется перезагрузить ПК после применения данного твика!", ToolTipIcon.Warning);
            }
            if (checkBox_dism.Checked)
            {
                hcmd(@"Dism /Online /Set-ReservedStorageState /State:Disabled");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("ReservedStorageState", 1);
                checkBox_dism.Enabled = false;
                checkBox_dism.Checked = false;
                back_main_2.Visible = true;
            }
            if (checkBox_gibernate.Checked)
            {
                hcmd("powercfg -h off");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("hibernate", 1);
                checkBox_gibernate.Enabled = false;
                checkBox_gibernate.Checked = false;
                back_main_3.Visible = true;

            }
            if (checkBox_scheme.Checked)
            {
                hcmd("powercfg -restoredefaultschemes");
                File.WriteAllBytes(path_scheme, Resources.scheme);
                string guidstring = Guid.NewGuid().ToString();
                writelog(guidstring);
                hcmd($"powercfg -import {path_scheme} {guidstring}");
                hcmd($"powercfg -setactive \"{guidstring}\"");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("imported_powerscheme", 1);
                checkBox_scheme.Enabled = false;
                checkBox_scheme.Checked = false;
                back_main_4.Visible = true;
            }
            if (checkBox_mpo.Checked)
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm", true).SetValue("OverlayTestMode", 5);
                checkBox_mpo.Enabled = false;
                checkBox_mpo.Checked = false;
                back_main_6.Visible = true;
            }
            if (checkBox_usb.Checked)
            {
                File.WriteAllText(tempfolder + @"\usbpowersaving.bat", Resources.usbpowersaving);
                hcmd($"regedit.exe /s {tempfolder}/usbpowersaving.cmd");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("usbpowersaving", 1);
                RegistryKey key;
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\usbhub\\hubg");
                key?.SetValue("DisableOnSoftRemove", 0x00000001, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\xusb22\\Parameters");
                key?.SetValue("IoQueueWorkItem", 0x0000000a, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\USBXHCI\\Parameters");
                key?.SetValue("IoQueueWorkItem", 0x0000000a, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\usbhub\\Parameters");
                key?.SetValue("IoQueueWorkItem", 0x0000000a, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Enum\\USB");
                key?.SetValue("AllowIdleIrpInD3", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("EnhancedPowerManagementEnabled", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\USBXHCI\\Parameters\\Wdf");
                key?.SetValue("NoExtraBufferRoom", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\usbflags");
                key?.SetValue("fid_D1Latency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("fid_D2Latency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("fid_D3Latency", 0x00000001, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\usbhub\\Performance");
                key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\EnhancedStorageDevices");
                key?.SetValue("TCGSecurityActivationDisabled", 0x00000001, RegistryValueKind.DWord);
                key.Close();
                checkBox_usb.Enabled = false;
                checkBox_usb.Checked = false;
                back_main_7.Visible = true;
            }
            if (checkBox_mtiititit.Checked)
            {
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true)?.SetValue("FeatureSettings", 1);
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true)?.SetValue("FeatureSettingsOverride", 3);
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true)?.SetValue("FeatureSettingsOverrideMask", 3);
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true)?.SetValue("EnableCfg", 0);
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", true)?.SetValue("ProtectionMode", 0);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", true)?.SetValue("DisableExceptionChainValidation", 1);
                Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", true)?.SetValue("Enabled", 0);
                //supercmd("ren %SYSTEMROOT%\\System32\\mcupdate_GenuineIntel.dll mcupdate_GenuineIntel.old");
                //supercmd("ren %SYSTEMROOT%\\System32\\mcupdate_AuthenticAMD.dll mcupdate_AuthenticAMD.old");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("Mitigations", 1);
                checkBox_mtiititit.Enabled = false;
                checkBox_mtiititit.Checked = false;
                back_main_9.Visible = true;

            }
            if (checkBox_mmagent.Checked)
            {
                File.WriteAllText(tempfolder + @"\prefetcher hdd.reg", Resources.prefetcher_hdd);
                File.WriteAllText(tempfolder + @"\prefetcher ssd.reg", Resources.prefetcher_ssd);
                //

                using (WebClient wc = new WebClient())
                    if (!File.Exists($"{tempfolder}\\DefenderKiller.zip"))
                    {
                        wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/smartctl.exe", $"{tempfolder}\\smartctl.exe");
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
                    hcmd($"regedit.exe /s {tempfolder}/prefetcher ssd.reg");
                }
                else
                {
                    hcmd($"regedit.exe /s {tempfolder}/prefetcher hdd.reg");
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
                    powershell($"enable-mmagent -ApplicationPreLaunch");
                    powershell($"enable-mmagent -MC");
                    powershell($"disable-mmagent -PC");
                    powershell($"set-mmagent -moaf {mmMemory}");
                }
                else
                {
                    powershell($"enable-mmagent -ApplicationPreLaunch");
                    powershell($"disable-mmagent -MC");
                    powershell($"disable-mmagent -PC");
                    powershell($"set-mmagent -moaf {mmMemory}");
                }

                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("mmagent", 1);
                checkBox_mmagent.Enabled = false;
                checkBox_mmagent.Checked = false;
                back_main_10.Visible = true;
            }
            if (checkBox_page.Checked)
            {
                try
                {
                    // Открываем раздел реестра
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true))
                    {
                        if (key != null)
                        {
                            // Новое значение для PagingFiles
                            string[] newValue = { "C:\\pagefile.sys 16 32768" }; // Указываем диск и размеры в МБ

                            // Устанавливаем новое значение
                            key.SetValue("PagingFiles", newValue, RegistryValueKind.MultiString);

                            writelog("[pagefile check] Параметры файла подкачки успешно изменены. Перезагрузите систему для применения изменений.");
                            checkBox_page.Enabled = false;
                            checkBox_page.Checked = false;
                            back_main_11.Visible = true;
                        }
                        else
                        {
                            writelog("[pagefile check] Не удалось открыть ключ реестра.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    writelog("[pagefile check] Произошла ошибка: " + ex.Message);
                }
                ShowNotification("Файл подкачки", "Размер файла подкачки будет изменён после перезагрузки ПК", ToolTipIcon.Info);
            }
            if (checkBox_mousefix.Checked)
            {
                File.WriteAllText(tempfolder + @"\mousefix.reg", Resources.mousefix);
                hcmd($"regedit.exe /s {tempfolder}\\mousefix.reg");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("mousefix", 1);
                checkBox_mousefix.Enabled = false;
                checkBox_mousefix.Checked = false;
                back_main_5.Visible = true;
            }

            //вторая
            if (checkBox_audio.Checked)
            {
                supercmd($@"regedit /s {tempfolder}\Audio_Lantency.reg");
                RegistryKey key;
                key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Audio");
                key?.SetValue("DisableSpatialOnComboEndpoints", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DisableProtectedAudioDG", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DisableProtectedAudio", 0x00000001, RegistryValueKind.DWord);
                key.Close();
                checkBox_audio.Checked = false;
                checkBox_audio.Enabled = false;
                back_main_13.Visible = true;
            }
            if (checkBox_dwninput.Checked)
            {
                RegistryKey key;
                key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows");
                key?.SetValue("DesktopHeapLogging", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("DwmInputUsesIoCompletionPort", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("EnableDwmInputProcessing", 0x00000000, RegistryValueKind.DWord);
                key.Close();
                checkBox_dwninput.Checked = false;
                checkBox_dwninput.Enabled = false;
                back_main_14.Visible = true;
            }
            if (checkBox_tolerate.Checked)
            {
                RegistryKey key;
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Power");
                key?.SetValue("ExitLatency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DisableVsyncLatencyUpdate", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DisableSensorWatchdog", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("ExitLatencyCheckEnabled", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("Latency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("LatencyToleranceDefault", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("LatencyToleranceFSVP", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("LatencyToleranceIdleResiliency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("LatencyTolerancePerfOverride", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("LatencyToleranceScreenOffIR", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("LatencyToleranceVSyncEnabled", 0x000000001, RegistryValueKind.DWord);
                key?.SetValue("RtlCapabilityCheckLatency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("MfBufferingThreshold", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("CsEnabled", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("QosManagesIdleProcessors", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("SleepReliabilityDetailedDiagnostics", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("EventProcessorEnabled", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\\Power");
                key?.SetValue("RmGpsPsEnablePerCpuCoreDpc", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("PowerSavingTweaks", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("DisableWriteCombining", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("EnableRuntimePowerManagement", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("PrimaryPushBufferSize", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("FlTransitionLatency", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("D3PCLatency", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("RMDeepLlEntryLatencyUsec", 0x00000000, RegistryValueKind.DWord);
                key?.SetValue("PciLatencyTimerControl", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("Node3DLowLatency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("LOWLATENCY", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("RmDisableRegistryCaching", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("RMDisablePostL2Compression", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultMemoryRefreshLatencyToleranceNoContext", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultMemoryRefreshLatencyToleranceMonitorOff", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultMemoryRefreshLatencyToleranceActivelyUsed", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultD3TransitionLatencyIdleShortTime", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultD3TransitionLatencyIdleNoContext", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultD3TransitionLatencyIdleMonitorOff", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceTimerPeriod", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceOther", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceNoContextMonitorOff", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceNoContext", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceMemory", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceIdle1MonitorOff", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceIdle1", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceIdle0MonitorOff", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultLatencyToleranceIdle0", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultD3TransitionLatencyIdleVeryLongTime", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultD3TransitionLatencyIdleLongTime", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultD3TransitionLatencyActivelyUsed", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("Latency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DefaultD3TransitionLatencyActivelyUsed", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("TransitionLatency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("MonitorRefreshLatencyTolerance", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("MonitorLatencyTolerance", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("MiracastPerfTrackGraphicsLatency", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("MaxIAverageGraphicsLatencyInOneBucket", 0x00000001, RegistryValueKind.DWord);
                key.Close();
                checkBox_tolerate.Checked = false;
                checkBox_tolerate.Enabled = false;
                back_main_16.Visible = true;
            }
            if (checkBox_pro_11.Checked)
            {
                Win32Priority Win32Priority = new Win32Priority();
                Win32Priority.ShowDialog();
                checkBox_pro_11.Checked = false;
                checkBox_pro_11.Enabled = false;
                back_pro_11.Visible = true;
            } //Win32Priority
            #endregion
            #region gpu
            if (checkBox_ansel.Checked)
            {
                string[] ansel = Directory.GetFiles(@"C:\Windows\System32\DriverStore\", "NvCameraEnable.exe", SearchOption.AllDirectories);
                if (ansel.Length > 0)
                {
                    string anselPath = ansel[0];
                    hcmd($"{anselPath} off");
                }
                else
                {
                    MessageBox.Show("Файл NvCameraEnable.exe не найден");
                }

                checkBox_ansel.Checked = false;
                checkBox_ansel.Enabled = false;
                back_gpu_4.Visible = true;
            }
            if (checkBox_hdcp.Checked)
            {
                checkBox_hdcp.Checked = false;
                checkBox_hdcp.Enabled = false;
                Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true).SetValue("RMHdcpKeyglobZero", 1);
                back_gpu_2.Visible = true;

            }
            if (checkBox_dopNVIDIA_tweaks.Checked)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\NVIDIA Corporation\Global\NVTweak\Devices\509901423-0\Color", true)?.SetValue("NvCplUseColorCorrection", "0");
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", true)?.SetValue("PlatformSupportMiracast", "0");
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak", true)?.SetValue("DisplayPowerSaving", "0");
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true)?.SetValue("EnableTiledDisplay", "0");
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS", true)?.SetValue("EnableRID61684", "1");
                checkBox_dopNVIDIA_tweaks.Checked = false;
                checkBox_dopNVIDIA_tweaks.Enabled = false;
                back_gpu_3.Visible = true;
            }
            if (checkBox_directplay.Checked)
            {
                hcmd("dism /online /Enable-Feature /FeatureName:DirectPlay /All");
                checkBox_directplay.Checked = false;
                checkBox_directplay.Enabled = false;
                back_gpu_1.Visible = true;
            }
            #endregion
            #region ui
            if (checkBoxUI_Buttons_1.Checked)
            {
                Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop\\WindowMetrics", true).SetValue("CaptionHeight", "-270");
                Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop\\WindowMetrics", true).SetValue("CaptionWidth", "-270");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("buttons", 1);
                checkBoxUI_Buttons_1.Enabled = false;
                checkBoxUI_Buttons_1.Checked = false;
                back_ui_1.Visible = true;
            }
            if (checkBoxUI_Buttons_2.Checked)
            {
                hcmd($"regedit.exe /s {path_ui}/things.reg");
                hcmd(@"rd /s /q ""%userprofile%\3D Objects\""");
                if (!win10)
                {
                    hcmd(@"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""UseCompactMode"" /t REG_DWORD /d ""00000001"" /f");
                }
                else
                {
                    hcmd($"regedit.exe /s {path_ui}/Taskmgr_win10_new.reg");
                }

                hcmd("taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("litle_explorer_things", 1);
                checkBoxUI_Buttons_2.Enabled = false;
                checkBoxUI_Buttons_2.Checked = false;
                back_ui_2.Visible = true;
            }
            if (checkBoxUI_Buttons_4.Checked)
            {
                Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true).SetValue("JPEGImportQuality", 100);
                checkBoxUI_Buttons_4.Enabled = false;
                checkBoxUI_Buttons_4.Checked = false;
                back_ui_4.Visible = true;
            }
            if (checkBox_bluefolders.Checked)
            {
                if (win10)
                {
                    File.WriteAllText(tempfolder + @"\blueiconsw10.bat", Resources.blueiconsw10);
                    using (WebClient wc = new WebClient())
                        if (!File.Exists($"{tempfolder}\\imageres.zip"))
                        {
                            wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/imageres.zip", $"{tempfolder}\\imageres.zip");
                            ZipFile.ExtractToDirectory($"{tempfolder}\\imageres.zip", tempfolder);
                        }
                    await hcmdAsync($"{tempfolder}\\blueiconsw10.bat");
                    checkBox_bluefolders.Checked = false;
                    checkBox_bluefolders.Enabled = false;
                    back_ui_5.Visible = true;
                    //Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("bluefolders", 1);
                }
                else
                {
                    File.WriteAllText(tempfolder + @"\blueicons.bat", Resources.blueicons);
                    using (WebClient wc = new WebClient())
                        if (!File.Exists($"{tempfolder}\\BlueIcon_Minimal.zip"))
                        {
                            wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/BlueIcon_Minimal.zip", $"{tempfolder}\\BlueIcon_Minimal.zip");
                            ZipFile.ExtractToDirectory($"{tempfolder}\\BlueIcon_Minimal.zip", tempfolder + "\\BlueIcon Minimal");
                        }
                    await hcmdAsync($"{tempfolder}\\blueicons.bat");
                    checkBox_bluefolders.Checked = false;
                    checkBox_bluefolders.Enabled = false;
                    back_ui_5.Visible = true;
                    //Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("bluefolders", 1);
                }
            }
            if (checkBox_contex.Checked)
            {
                if (!win10)
                {
                    Registry.CurrentUser.CreateSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", true).SetValue("", " ");
                    hcmd(@"taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
                    checkBox_contex.Checked = false;
                    checkBox_contex.Enabled = false;
                    back_ui_6.Visible = true;
                    await Task.Delay(3000);
                }
            }
            if (checkBox_shapka.Checked)
            {
                //hcmd($"{tempfolder}\\shapka.bat");
                hcmd("taskkill /f /im explorer.exe >nul 2>&1");
                supercmd("reg add \"HKCR\\CLSID\\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}\\InProcServer32\" /v \"\" /t REG_SZ /d \"C:\\Windows\\System32\\FixByVlado\" /f");
                await Task.Delay(1000);
                hcmd("explorer.exe");
                checkBox_shapka.Checked = false;
                checkBox_shapka.Enabled = false;
                back_ui_7.Visible = true;
            }
            if (checkBox_wotboincontex.Checked)
            {
                if (!File.Exists(@"C:\Windows\gear.ico"))
                {
                    File.WriteAllBytes(tempfolder + @"\gear.zip", Resources.gear);
                    ZipFile.ExtractToDirectory(tempfolder + @"\gear.zip", "C:\\Windows");
                }
                RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\WOTBO", true);
                key.SetValue("", "WOTBO");
                key.SetValue("icon", @"C:\Windows\gear.ico");//вставить ico
                key.CreateSubKey("command");
                key.Close();
                if (!File.Exists(@"C:\Windows\wotbo.exe"))
                {
                    File.Copy(exepath, @"C:\Windows\wotbo.exe");
                }
                Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\WOTBO\command", true).SetValue("", contexpath);
                checkBox_wotboincontex.Checked = false;
                checkBox_wotboincontex.Enabled = false;
                back_ui_8.Visible = true;
            }
            if (checkBox_explorer.Checked)
            {
                RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\explorer", true);
                key.SetValue("", "Restart explorer.exe");
                if (win10)
                    key.SetValue("icon", "imageres.dll,264");
                else
                    key.SetValue("icon", "imageres.dll,265");

                key.CreateSubKey("command");
                key.Close();
                Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\Shell\explorer\command", true).SetValue("", @"c:\Windows\system32\explorer.bat");
                checkBox_explorer.Checked = false;
                checkBox_explorer.Enabled = false;
                back_ui_9.Visible = true;
            }
            if (checkBox_ffmpeg.Checked)
            {
                ShowNotification("FFMPEG", "Ожидайте скачивания FFMPEG. Программа не зависла!", ToolTipIcon.Info);
                using (WebClient wc = new WebClient())
                    if (!File.Exists($"{tempfolder}\\ffmpeg.zip"))
                    {
                        await wc.DownloadFileTaskAsync(new Uri("https://github.com/GyanD/codexffmpeg/releases/download/7.1/ffmpeg-7.1-essentials_build.zip"), $"{tempfolder}\\ffmpeg.zip");
                        await wc.DownloadFileTaskAsync(new Uri("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/ffmpeg.reg"), $"{tempfolder}\\ffmpeg.reg");
                        ZipFile.ExtractToDirectory($"{tempfolder}\\ffmpeg.zip", tempfolder);
                    }
                if (File.Exists(@"C:\Windows\ffmpeg.exe"))
                {
                    hcmd("taskkill /f /im ffmpeg.exe");
                    File.Delete(@"C:\Windows\ffmpeg.exe");
                }
                File.Copy($@"{tempfolder}\ffmpeg-7.1-essentials_build\bin\ffmpeg.exe", @"C:\Windows\ffmpeg.exe");
                hcmd($@"regedit.exe /s {tempfolder}/ffmpeg.reg");

                checkBox_ffmpeg.Enabled = false;
                checkBox_ffmpeg.Checked = false;
                back_ui_10.Visible = true;
            }
            if (checkBox_mica.Checked)
            {
                using (WebClient wc = new WebClient())
                    if (!File.Exists($"{tempfolder}\\Release_x64.zip"))
                    {
                        wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/Release_x64.zip", $"{tempfolder}\\Release_x64.zip");
                        ZipFile.ExtractToDirectory($"{tempfolder}\\Release_x64.zip", tempfolder);
                    }
                File.Copy($@"{tempfolder}\Release\ExplorerBlurMica.dll", @"C:\Windows\ExplorerBlurMica.dll");
                File.Copy($@"{tempfolder}\Release\config.ini", @"C:\Windows\config.ini");
                hcmd($"regsvr32 /s C:\\Windows\\ExplorerBlurMica.dll & taskkill /f /im explorer.exe & explorer.exe & timeout /t 1 & explorer.exe");
                checkBox_mica.Enabled = false;
                checkBox_mica.Checked = false;
                back_ui_11.Visible = true;
            }
            if (checkBox_animation.Checked)
            {
                animation = false;
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("animations", 1);
                back_animations.Visible = true;
                checkBox_animation.Checked = false;
                checkBox_animation.Enabled = false;
            }
            #endregion
            #region dop
            if (checkBox_onedrive.Checked)
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
                    writelog($"путь к Onedrive найден - {OneDriveSetupPath}, переходим к удалению");

                    // Вызов команды для деинсталляции OneDrive
                    await hcmdAsync($"{OneDriveSetupPath} /uninstall");
                }
                else
                {
                    // Логирование ошибки, если файл не найден
                    writelog("Файл OneDriveSetup.exe не найден");
                }
                #endregion
                checkBox_onedrive.Checked = false;
                checkBox_onedrive.Enabled = false;
            }
            if (checkBox_compactos.Checked)
            {
                scmd(@"compact /CompactOS:always");
                checkBox_compactos.Checked = false;
            }
            if (checkBox_WinSxS.Checked)
            {
                scmd(@"Dism.exe /online /Cleanup-Image /StartComponentCleanup /ResetBase");
                checkBox_WinSxS.Checked = false;
            }
            if (checkBox_temp.Checked)
            {
                string tempPath = Path.GetTempPath();

                // Удаление всех файлов, кроме wotbo.log
                foreach (string file in Directory.GetFiles(tempPath))
                {
                    try
                    {
                        // Получаем имя файла без пути
                        string fileName = Path.GetFileName(file);

                        // Проверяем, не является ли файл wotbo.log
                        if (fileName.Equals("wotbo.log", StringComparison.OrdinalIgnoreCase))
                        {
                            writelog($"Файл {file} пропущен.");
                            continue;
                        }

                        File.Delete(file);
                        writelog($"Файл {file} успешно удален.");
                    }
                    catch (Exception ex)
                    {
                        writelog($"Ошибка при удалении файла {file}: {ex.Message}");
                    }
                }

                // Удаление всех папок и их содержимого
                foreach (string dir in Directory.GetDirectories(tempPath))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        writelog($"Папка {dir} успешно удалена.");
                    }
                    catch (Exception ex)
                    {
                        writelog($"Ошибка при удалении папки {dir}: {ex.Message}");
                    }
                }
                checkBox_temp.Checked = false;
            }
            if (checkBox_updclean.Checked)
            {
                //hcmd($"{tempfolder}\\cleanupdate.bat");
                hcmd(@"net stop wuauserv >nul");
                hcmd(@"del /q /f /s %windir%\SoftwareDistribution\Download\*.* >nul");
                hcmd(@"rd /q /s %windir%\SoftwareDistribution\Download\ >nul");
                hcmd(@"del /q /f /s %windir%\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache\ >nul");
                hcmd(@"rd /q /s %windir%\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache\ >nul");
                checkBox_updclean.Checked = false;
            }
            if (checkBox_picture_cache.Checked)
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true).SetValue("Max Cached Icons", 4096);
                checkBox_picture_cache.Checked = false;
                checkBox_picture_cache.Enabled = false;
                back_dop_1.Visible = true;
            }
            if (checkBox_mobile_traffic.Checked)
            {
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true).SetValue("DefaultTTL", 0x00000041);
                Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", true).SetValue("DefaultTTL", 0x00000041);
                checkBox_mobile_traffic.Checked = false;
                checkBox_mobile_traffic.Enabled = false;
                back_dop_2.Visible = true;
            }
            if (checkBox_nastroyka.Checked)
            {
                Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement", true)?.SetValue("ScoobeSystemSettingEnabled", 0);
                checkBox_nastroyka.Checked = false;
                checkBox_nastroyka.Enabled = false;
                back_dop_3.Visible = true;
            }
            if (checkBox_zalipanie.Checked)
            {
                Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys", true)?.SetValue("Flags", "506");
                checkBox_zalipanie.Checked = false;
                checkBox_zalipanie.Enabled = false;
                back_dop_4.Visible = true;
            }
            if (checkBox_dwm.Checked)
            {
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true)
                    .CreateSubKey("dwm.exe");
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\dwm.exe", true)
                    .CreateSubKey("PerfOptions");
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\dwm.exe\PerfOptions", true)
                    .SetValue("CpuPriorityClass", "1", RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\dwm.exe\PerfOptions", true)
    .SetValue("IoPriority", "0", RegistryValueKind.DWord);
                checkBox_dwm.Checked = false;
                checkBox_dwm.Enabled = false;
                back_dop_6.Visible = true;
            }
            if (checkBox_edge.Checked)
            {
                RegistryKey edge;
                edge = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Edge");
                edge?.SetValue("EdgeEnhanceImagesEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("EdgeWorkspacesEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("TyposquattingCheckerEnabled", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("RemoveDesktopShortcutDefault", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("AlternateErrorPagesEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SpotlightExperiencesAndRecommendationsEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("NewTabPageSearchBox", "redirect");
                edge?.SetValue("DefaultSearchProviderEnabled", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("BasicAuthOverHttpEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("AllowCrossOriginAuthPrompt", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("DisableAuthNegotiateCnameLookup", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("NativeMessagingUserLevelHosts", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("PasswordMonitorAllowed", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("StartupBoostEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("UseSystemPrintDialog", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("SleepingTabsEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("RestoreOnStartup", 0x00000004, RegistryValueKind.DWord);
                edge?.SetValue("HomepageLocation", "www.google.com");
                edge?.SetValue("NewTabPageLocation", "www.google.com");
                edge?.SetValue("NewTabPagePrerenderEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("NewTabPageHideDefaultTopSites", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("AdsSettingForIntrusiveAdsSites", 0x00000002, RegistryValueKind.DWord);
                edge?.SetValue("DownloadRestrictions", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("TabFreezingEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("InternetExplorerIntegrationTestingAllowed", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("InternetExplorerIntegrationLocalFileAllowed", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("PersonalizationReportingEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("BrowserNetworkTimeQueriesEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("LocalProvidersEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("AudioSandboxEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("WebWidgetIsEnabledOnStartup", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("UserFeedbackAllowed", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("FamilySafetySettingsEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ClickOnceEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("DirectInvokeEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SSLErrorOverrideAllowed", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("BingAdsSuppression", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("TrackingPrevention", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("BrowserSignin", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("InternetExplorerIntegrationEnhancedHangDetection", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("InternetExplorerIntegrationLevel", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ConfigureOnlineTextToSpeech", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("VerticalTabsAllowed", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("ConfigureFriendlyURLFormat", 0x00000004, RegistryValueKind.DWord);
                edge?.SetValue("ExperimentationAndConfigurationServiceControl", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("IntensiveWakeUpThrottlingEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("DnsOverHttpsMode", "off");
                edge?.SetValue("DNSInterceptionChecksEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("TargetBlankImpliesNoOpener", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ComponentUpdatesEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("EnableDomainActionsDownload", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("NetworkPredictionOptions", 0x00000002, RegistryValueKind.DWord);
                edge?.SetValue("EnableOnlineRevocationChecks", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("RendererCodeIntegrityEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ResolveNavigationErrorsUseWebService", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("BackgroundTemplateListUpdatesEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SearchSuggestEnabled", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("CommandLineFlagSecurityWarningsEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SitePerProcess", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SpellcheckEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("EdgeCollectionsEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("WebWidgetAllowed", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("MetricsReportingEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ForceEphemeralProfiles", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ForceGoogleSafeSearch", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("GoToIntranetSiteForSingleWordEntryInAddressBar", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("HideFirstRunExperience", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("HideInternetExplorerRedirectUXForIncompatibleSitesEnabled", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("RelaunchNotification", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("RedirectSitesFromInternetExplorerPreventBHOInstall", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("PromotionalTabsEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("DiagnosticData", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SendSiteInfoToImproveServices", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("TotalMemoryLimitMb", 0x00008192, RegistryValueKind.DWord);
                edge?.SetValue("WPADQuickCheckEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("EdgeShoppingAssistantEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("InternetExplorerIntegrationLocalFileShowContextMenu", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("AddressBarMicrosoftSearchInBingProviderEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ShowOfficeShortcutInFavoritesBar", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ShowMicrosoftRewards", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("BuiltInDnsClientEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ClearCachedImagesAndFilesOnExit", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("ConfigureDoNotTrack", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("ApplicationGuardFavoritesSyncEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ApplicationGuardTrafficIdentificationEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("ApplicationGuardUploadBlockingEnabled", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("EnableMediaRouter", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("ShowCastIconInToolbar", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("DefaultCookiesSetting", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("DefaultFileSystemReadGuardSetting", 0x00000003, RegistryValueKind.DWord);
                edge?.SetValue("DefaultFileSystemWriteGuardSetting", 0x00000003, RegistryValueKind.DWord);
                edge?.SetValue("DefaultGeolocationSetting", 0x00000002, RegistryValueKind.DWord);
                edge?.SetValue("DefaultImagesSetting", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("DefaultInsecureContentSetting", 0x00000002, RegistryValueKind.DWord);
                edge?.SetValue("DefaultJavaScriptJitSetting", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("DefaultJavaScriptSetting", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("DefaultNotificationsSetting", 0x00000003, RegistryValueKind.DWord);
                edge?.SetValue("DefaultPluginsSetting", 0x00000003, RegistryValueKind.DWord);
                edge?.SetValue("DefaultPopupsSetting", 0x00000002, RegistryValueKind.DWord);
                edge?.SetValue("DefaultWebBluetoothGuardSetting", 0x00000003, RegistryValueKind.DWord);
                edge?.SetValue("DefaultWebHidGuardSetting", 0x00000002, RegistryValueKind.DWord);
                edge?.SetValue("DefaultWebUsbGuardSetting", 0x00000002, RegistryValueKind.DWord);
                edge?.SetValue("LegacySameSiteCookieBehaviorEnabled", 0x00000002, RegistryValueKind.DWord);
                edge?.SetValue("PreventSmartScreenPromptOverrideForFiles", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("PreventSmartScreenPromptOverride", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("SmartScreenForTrustedDownloadsEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("NewSmartScreenLibraryEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SmartScreenDnsRequestsEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SmartScreenPuaEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SmartScreenEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("NewTabPageContentEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("NewTabPageQuickLinksEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("NewTabPageAllowedBackgroundTypes", 0x00000003, RegistryValueKind.DWord);
                edge?.SetValue("TyposquattingCheckerEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("BackgroundModeEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("EfficiencyMode", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("SuppressUnsupportedOSWarning", 0x00000001, RegistryValueKind.DWord);
                edge?.SetValue("HubsSidebarEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("SiteSafetyServicesEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("EdgeFollowEnabled", 0x00000000, RegistryValueKind.DWord);
                edge?.SetValue("LocalBrowserDataShareEnabled", 0x00000000, RegistryValueKind.DWord);
                edge = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Edge\\RestoreOnStartupURLs");
                edge?.SetValue("1", "www.google.com");
                back_dop_edge.Visible = true;
                checkBox_edge.Checked = false;
                checkBox_edge.Enabled = false;
            }
            if (checkBox_move_temp.Checked)
            {
                Registry.CurrentUser.OpenSubKey("Environment", true).SetValue("TEMP", "%systemdrive%\\Temp", RegistryValueKind.ExpandString);
                Registry.CurrentUser.OpenSubKey("Environment", true).SetValue("TMP", "%systemdrive%\\Temp", RegistryValueKind.ExpandString);
                if (!Directory.Exists($@"{Environment.ExpandEnvironmentVariables("%systemdrive%")}\Temp"))
                {
                    Directory.CreateDirectory($@"{Environment.ExpandEnvironmentVariables("%systemdrive%")}\Temp");
                }

                checkBox_move_temp.Checked = false;
                checkBox_move_temp.Enabled = false;
                back_dop_movetemp.Visible = true;
            }
            #endregion
            #region pro
            if (checkBox_pro_1.Checked)
            {
                {  //HomeDelete
                    hcmd(@"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v ""HubMode"" /t REG_DWORD /d ""00000001"" /f >nul 2>&1");
                    supercmd(@"cmd.exe /c reg add ""HKCR\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}"" /v ""System.IsPinnedToNameSpaceTree"" /t REG_DWORD /d ""00000000"" /f");
                    //GaleryDelete
                    hcmd("reg add \"HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}\" /v \"System.IsPinnedToNameSpaceTree\" /t REG_DWORD /d \"00000000\" /f >nul 2>&1");
                    supercmd(@"cmd.exe /c reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}"" /v ""System.IsPinnedToNameSpaceTree"" /t REG_DWORD /d ""00000001"" /f >nul 2>&1");
                    //restart explorer
                    hcmd("taskkill /f /im explorer.exe && timeout /t 1 && explorer.exe");
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("pro_1", 1);
                    checkBox_pro_1.Checked = false;
                    checkBox_pro_1.Enabled = false;
                }
            } //disable main & gallery folder in explorer.exe
            if (checkBox_pro_3.Checked)
            {
                hcmd($"regedit /s {tempfolder}\\updates.reg");
                checkBox_pro_3.Checked = false;
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;
            } //winupdates

            if (checkBox_CSRSS.Checked)
            {
                //hcmd($"{tempfolder}\\csrss.bat");
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true)
    .CreateSubKey("csrss.exe");
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe", true)
                    .CreateSubKey("PerfOptions");
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions", true)
                    .SetValue("CpuPriorityClass", "3", RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\csrss.exe\PerfOptions", true)
    .SetValue("IoPriority", "3", RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true)
.SetValue("NoLazyMode", "1", RegistryValueKind.DWord);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true)
.SetValue("AlwaysOn", "1", RegistryValueKind.DWord);

                checkBox_CSRSS.Checked = false;
                checkBox_CSRSS.Enabled = false;
                back_dop_7.Visible = true;
                //Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("pro_8", 1);
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
                DialogResult result = MessageBox.Show(
                    "Bufferbloat - явление, когда чрезмерная буферизация вызывает увеличение времени прохождения пакетов (Ping) и разброса задержки пакетов (Packetloss)\n\n" +
                    "Рекомендуется использовать только если у вас нестабильное соединение!\n(Например мобильный интернет и т.д)\n" +
                    "!Твик может незначительно снизить скорость интернета!\n" +
                    "В случае если у вас стало хуже, или же вам не нужен этот твик, просто откатите его.\n" +
                    "Применить?",
    "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    hcmd("netsh int tcp set global autotuninglevel=disabled");
                    checkBox_pro_12.Checked = false;
                    checkBox_pro_12.Enabled = false;
                    back_pro_12.Visible = true;
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("buffer", 1);
                }
                else
                {
                    checkBox_pro_12.Checked = false;
                }
            } //Bufferbloat
            if (checkBox_pro_13.Checked)
            {
                using (WebClient wc = new WebClient())
                    if (!Directory.Exists($"{tempfolder}\\unlocker.zip"))
                    {
                        wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/unlocker.zip", $"{tempfolder}\\unlocker.zip");
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
                Registry.ClassesRoot.CreateSubKey(@"*\shell\Уничтожить", true).SetValue("Icon", @"C:\Windows\unlocker\Icons\Unlocker.ico");
                Registry.ClassesRoot.CreateSubKey(@"*\shell\Уничтожить\command", true).SetValue("", "\"C:\\Windows\\unlocker\\Unlocker.exe\" /contextMenu \"%1\"");
                Registry.ClassesRoot.CreateSubKey(@"Directory\shell\Уничтожить", true).SetValue("", @"Уничтожить");
                Registry.ClassesRoot.CreateSubKey(@"Directory\shell\Уничтожить", true).SetValue("Icon", @"C:\Windows\unlocker\Icons\Unlocker.ico");
                Registry.ClassesRoot.CreateSubKey(@"Directory\shell\Уничтожить\command", true).SetValue("", "\"C:\\Windows\\unlocker\\Unlocker.exe\" /contextMenu \"%1\"");
                checkBox_pro_13.Checked = false;
                checkBox_pro_13.Enabled = false;
                back_pro_13.Visible = true;
            } //unlocker
            if (checkBox_pro_14.Checked)
            {
                using (WebClient wc = new WebClient())
                    if (!File.Exists(@"C:\Windows\AlwaysOnTop.exe"))
                    {
                        wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/AlwaysOnTop.exe", @"C:\Windows\AlwaysOnTop.exe");
                    }
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).SetValue("AlwaysOnTop", @"C:\Windows\AlwaysOnTop.exe");
                Process.Start(@"C:\Windows\AlwaysOnTop.exe");
                checkBox_pro_14.Checked = false;
                checkBox_pro_14.Enabled = false;
                back_pro_14.Visible = true;
            }//alwaysontop
            #endregion
        }
        #region курсоры
        void label_cursors_Click_1(object sender, EventArgs e)
        {
            using (WebClient wc = new WebClient())
                if (!File.Exists($"{tempfolder}\\cursors.zip"))
                {
                    wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/cursors.zip", $"{tempfolder}\\cursors.zip");
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
            powershell("irm https://get.activated.win | iex");
        }
        #endregion
        #region msi mode
        void label_msimode_Click(object sender, EventArgs e)
        {
            if (Internet.OK())
            {
                try
                {
                    using (WebClient wcAA = new WebClient())
                        if (!File.Exists($"{tempfolder}\\MSI_util_v3.exe"))
                        {
                            wcAA.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/MSI_util_v3.exe", $"{tempfolder}\\MSI_util_v3.exe");
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

            using (WebClient wc = new WebClient())
                if (!File.Exists($"{tempfolder}\\DefenderKiller.zip"))
                {
                    wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/DefenderKiller.zip", $"{tempfolder}\\DefenderKiller.zip");
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
            hcmd($"regedit.exe /s {path_regpack}/exploitscfg.reg");
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
                using (WebClient wcAA = new WebClient())
                    if (!File.Exists($"{tempfolder}\\NVidiaProfileInspectorDmW.zip"))
                    {
                        wcAA.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/NVidiaProfileInspectorDmW.zip",
                            $"{tempfolder}\\NVidiaProfileInspectorDmW.zip");
                    }
                ZipFile.ExtractToDirectory(tempfolder + @"\NVidiaProfileInspectorDmW.zip", tempfolder);
                hcmd($"{path_NVidiaProfileInspectorDmW}\\nvidiaProfileInspector.exe \"{tempfolder}\\Base_Profile.nip\"");
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
            string[] possiblePaths = {
            @"C:\Windows\SysWOW64",
            @"C:\Windows\System32"
        };

            bool directplay = false;

            foreach (var path in possiblePaths)
            {
                try
                {
                    if (File.Exists(Path.Combine(path, "dpnet.dll.mui")))
                    {
                        directplay = true;
                        break;
                    }
                    else
                    {
                        string[] subDirectories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
                        foreach (var subDir in subDirectories)
                        {
                            try
                            {
                                if (File.Exists(Path.Combine(subDir, "dpnet.dll.mui")))
                                {
                                    directplay = true;
                                    checkBox_directplay.Enabled = false;
                                    back_gpu_1.Visible = true;
                                    break;
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // Пропускаем директории, доступ к которым запрещен
                                continue;
                            }
                        }
                    }
                    if (directplay) break;
                }
                catch (UnauthorizedAccessException)
                {
                    // Пропускаем директории, к которым нет доступа
                    continue;
                }
            }

            writelog($"DirectPlay установлен: {directplay}");
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
        void label_download_2_Click(object sender, EventArgs e)
        {
            OpenUrl("https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe");
        }
        void label_download_3_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.msi.com/Landing/afterburner/graphics-cards");
        }
        void label_download_4_Click(object sender, EventArgs e)
        {
            OpenUrl("https://www.7-zip.org/a/7z2409-x64.exe");
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
        void label_download_8_Click(object sender, EventArgs e)
        {
            OpenUrl("https://github.com/ShareX/ShareX/releases/download/v17.0.0/ShareX-17.0.0-setup.exe");
        }
        void label_download_9_Click(object sender, EventArgs e)
        {
            OpenUrl("https://telegram.org/dl/desktop/win64_portable");
        }
        void label_download_10_Click(object sender, EventArgs e)
        {
            OpenUrl("https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64");
        }
        void label_download_11_Click(object sender, EventArgs e)
        {
            OpenUrl("https://cdn-fastly.obsproject.com/downloads/OBS-Studio-31.0.0-Windows-Installer.exe");
        }
        void label_download_13_Click(object sender, EventArgs e)
        {
            OpenUrl("https://files2.codecguide.com/K-Lite_Codec_Pack_1875_Mega.exe");
        }
        void label_download_14_Click(object sender, EventArgs e)
        {
            OpenUrl("https://deac-fra.dl.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-5.0.3/qbittorrent_5.0.3_x64_setup.exe?viasf=1");
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
        void label_download_18_Click(object sender, EventArgs e)
        {
            OpenUrl("https://diskanalyzer.com/files/wiztree_4_23_portable.zip");
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
        void label_download_22_Click(object sender, EventArgs e)
        {
            OpenUrl("https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.28-windows-x64-installer");
        }
        void label_download_23_Click(object sender, EventArgs e)
        {
            OpenUrl("https://deac-riga.dl.sourceforge.net/project/crystaldiskinfo/9.2.3/CrystalDiskInfo9_2_3.exe");
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
            #region new
            if (win10)
            {
                hcmd($"regedit.exe /s {path_regpack}/w10/w10_AIO_RESTORE.reg");
            }
            else
            {
                supercmd($"regedit.exe /s {path_regpack}\\restore\\win11widgets_restore.bat");
                supercmd($"regedit.exe /s {path_regpack}/w11/w11_AIO_RESTORE.reg");
            }
            supercmd($"regedit.exe /s {path_regpack}/foldernetworkX_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}/REG_AIO_RESTORE.reg");
            RegistryKey key;
            key = Registry.CurrentUser.CreateSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\VideoSettings");
            key?.SetValue("EnableAutoEnhanceDuringPlayback", 0x00000001, RegistryValueKind.DWord);
            key.Close();
            #endregion
            checkBox_reg.Enabled = true;
            back_main_1.Visible = false;
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("regpack");
        } //regpack
        void back_main_2_Click(object sender, EventArgs e)
        {
            hcmd("Dism.exe /Online /Set-ReservedStorageState /State:Enabled");
            //Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("ReservedStorageState");
            checkBox_dism.Enabled = true;
            checkBox_dism.Checked = false;
            back_main_2.Visible = false;
        } //dism
        void back_main_3_Click(object sender, EventArgs e)
        {
            hcmd("powercfg -h on");
            //Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("hibernate");
            checkBox_gibernate.Enabled = true;
            checkBox_gibernate.Checked = false;
            back_main_3.Visible = false;
        } //powercfg /h off
        void back_main_4_Click(object sender, EventArgs e)
        {
            hcmd("powercfg -restoredefaultschemes");
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("imported_powerscheme");
            checkBox_scheme.Enabled = true;
            checkBox_scheme.Checked = false;
            back_main_4.Visible = false;
        }//scheme
        void back_main_5_Click(object sender, EventArgs e)
        {
            File.WriteAllText(tempfolder + @"\unmousefix.reg", Resources.unmousefix);
            hcmd($"regedit.exe /s {tempfolder}\\unmousefix.reg");
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("mousefix");
            checkBox_mousefix.Enabled = true;
            checkBox_mousefix.Checked = false;
            back_main_5.Visible = false;
        } //mouse
        void back_main_6_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm", true).DeleteValue("OverlayTestMode");
            checkBox_mpo.Enabled = true;
            checkBox_mpo.Checked = false;
            back_main_6.Visible = false;
        }//mpo
        void back_main_7_Click(object sender, EventArgs e)
        {
            File.WriteAllText(tempfolder + @"\unusbpowersaving.bat", Resources.unusbpowersaving);
            hcmd($"regedit.exe /s {tempfolder}/un_usbpowersaving.cmd");
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Services\\usbhub\\hubg");
            key?.DeleteValue("DisableOnSoftRemove");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Services\\xusb22\\Parameters");
            key?.DeleteValue("IoQueueWorkItem");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Services\\USBXHCI\\Parameters");
            key?.DeleteValue("IoQueueWorkItem");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Services\\usbhub\\Parameters");
            key?.DeleteValue("IoQueueWorkItem");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Enum\\USB");
            key?.DeleteValue("AllowIdleIrpInD3");
            key?.DeleteValue("EnhancedPowerManagementEnabled");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Services\\USBXHCI\\Parameters\\Wdf");
            key?.DeleteValue("NoExtraBufferRoom");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\usbflags");
            key?.DeleteValue("fid_D1Latency");
            key?.DeleteValue("fid_D2Latency");
            key?.DeleteValue("fid_D3Latency");
            key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\EnhancedStorageDevices");
            key?.SetValue("TCGSecurityActivationDisabled", 0x00000000, RegistryValueKind.DWord);
            key.Close();
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("usbpowersaving");
            checkBox_usb.Enabled = true;
            checkBox_usb.Checked = false;
            back_main_7.Visible = false;
        }//usbpowersaving
        void back_main_9_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true)?.DeleteValue("FeatureSettings");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true)?.DeleteValue("FeatureSettingsOverride");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true)?.DeleteValue("FeatureSettingsOverrideMask");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", true)?.DeleteValue("EnableCfg");

            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", true)?.DeleteValue("ProtectionMode");
            Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\kernel", true)?.DeleteValue("DisableExceptionChainValidation");

            if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity") != null)
            {
                Registry.LocalMachine.DeleteSubKeyTree(@"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity");
            }

            supercmd("ren %SYSTEMROOT%\\System32\\mcupdate_GenuineIntel.old mcupdate_GenuineIntel.dll");
            supercmd("ren %SYSTEMROOT%\\System32\\mcupdate_AuthenticAMD.old mcupdate_AuthenticAMD.dll");

            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("Mitigations");
            checkBox_mtiititit.Enabled = true;
            checkBox_mtiititit.Checked = false;
            back_main_9.Visible = false;
        }//mititigiig
        void back_main_10_Click(object sender, EventArgs e)
        {
            checkBox_mmagent.Enabled = true;
            checkBox_mmagent.Checked = false;
            back_main_10.Visible = false;
        } //mmagent не буду делать
        void back_main_11_Click(object sender, EventArgs e)
        {
            try
            {
                // Открываем раздел реестра
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management", writable: true))
                {
                    if (key != null)
                    {
                        // Восстанавливаем значение PagingFiles по умолчанию
                        string[] defaultValue = { "C:\\pagefile.sys 0 0" }; // Автоматический размер

                        // Устанавливаем новое значение
                        key.SetValue("PagingFiles", defaultValue, RegistryValueKind.MultiString);

                        writelog("[pagefile check] Стандартные настройки файла подкачки восстановлены. Перезагрузите систему для применения изменений.");
                    }
                    else
                    {
                        writelog("[pagefile check] Не удалось открыть ключ реестра.");
                    }
                }
            }
            catch (Exception ex)
            {
                writelog("[pagefile check] Произошла ошибка: " + ex.Message);
            }
            checkBox_page.Enabled = true;
            checkBox_page.Checked = false;
            back_main_11.Visible = false;
        } //page file
        void back_main_12_Click(object sender, EventArgs e)
        {
            #region new
            if (win10)
            {
                supercmd($"regedit.exe /s {path_regpack}/w10/win10defenderX_restore.reg");
            }
            else
            {
                supercmd($"regedit.exe /s {path_regpack}/w11/win11defenderX_restore.reg");
                #region webthreatdefusersvc_XXXXX
                // Путь к основной ветке реестра
                string baseKeyPath = @"SYSTEM\CurrentControlSet\Services";

                // Открываем основную ветку реестра
                using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(baseKeyPath))
                {
                    if (baseKey != null)
                    {
                        // Ищем подветку с нужным префиксом
                        foreach (string subKeyName in baseKey.GetSubKeyNames())
                        {
                            if (subKeyName.StartsWith("webthreatdefusersvc_"))
                            {
                                // Открываем найденную ветку
                                using (RegistryKey targetKey = baseKey.OpenSubKey(subKeyName, true))
                                {
                                    if (targetKey != null)
                                    {
                                        // Устанавливаем параметры в ветке
                                        targetKey.DeleteValue("LaunchProtected");
                                        targetKey.SetValue("Start", "00000002", RegistryValueKind.DWord);
                                        targetKey.DeleteValue("Start_old");
                                        writelog($"Параметр установлен в ветке {subKeyName}");
                                    }
                                }
                                break; // Предполагаем, что нужная ветка одна, и можно прекратить поиск
                            }
                        }
                    }
                }
                #endregion
            }
            supercmd($"regedit.exe /s {path_regpack}/antimalwareserviceexecutable/AIO_ON.reg");
            #endregion
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true)?.DeleteValue("defenderdisabled");
            back_main_12.Visible = false;
            //MessageBox.Show("Для восстановления работы защитника выполните перезагрузку!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ShowNotification("Защитник Windows", "Работа защитника будет восстановлена после перезагруки ПК", ToolTipIcon.Warning);
            checkBox_disabledefender.Enabled = true;
            checkBox_disabledefender.Checked = false;

        } //restoreDEF
        void back_main_13_Click(object sender, EventArgs e) //Audio_Lantency_delete.reg
        {
            supercmd($@"regedit /s {tempfolder}\Audio_Lantency_delete.reg");
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio");
            key?.SetValue("DisableSpatialOnComboEndpoints", 0x00000000, RegistryValueKind.DWord);
            key?.DeleteValue("DisableProtectedAudioDG");
            key?.DeleteValue("DisableProtectedAudio");
            key.Close();
            checkBox_audio.Checked = false;
            checkBox_audio.Enabled = true;
            back_main_13.Visible = false;
        }

        void back_main_14_Click(object sender, EventArgs e)
        {
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Windows");
            key?.SetValue("DesktopHeapLogging", 0x00000001, RegistryValueKind.DWord);
            key?.SetValue("DwmInputUsesIoCompletionPort", 0x00000001, RegistryValueKind.DWord);
            key?.SetValue("EnableDwmInputProcessing", 0x00000007, RegistryValueKind.DWord);
            key.Close();
            checkBox_dwninput.Checked = false;
            checkBox_dwninput.Enabled = true;
            back_main_14.Visible = false;
        }

        void back_main_16_Click(object sender, EventArgs e)
        {
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Power");
            key?.DeleteValue("ExitLatency");
            key?.DeleteValue("DisableVsyncLatencyUpdate");
            key?.DeleteValue("DisableSensorWatchdog");
            key?.DeleteValue("ExitLatencyCheckEnabled");
            key?.DeleteValue("Latency");
            key?.DeleteValue("LatencyToleranceDefault");
            key?.DeleteValue("LatencyToleranceFSVP");
            key?.DeleteValue("LatencyToleranceIdleResiliency");
            key?.DeleteValue("LatencyTolerancePerfOverride");
            key?.DeleteValue("LatencyToleranceScreenOffIR");
            key?.DeleteValue("RtlCapabilityCheckLatency");
            key?.DeleteValue("CsEnabled");
            key?.DeleteValue("QosManagesIdleProcessors");
            key?.DeleteValue("SleepReliabilityDetailedDiagnostics");
            key?.SetValue("EventProcessorEnabled", 0x00000001, RegistryValueKind.DWord);
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\GraphicsDrivers\\Power");
            key?.DeleteValue("RmGpsPsEnablePerCpuCoreDpc");
            key?.DeleteValue("PowerSavingTweaks");
            key?.DeleteValue("DisableWriteCombining");
            key?.DeleteValue("EnableRuntimePowerManagement");
            key?.DeleteValue("PrimaryPushBufferSize");
            key?.DeleteValue("FlTransitionLatency");
            key?.DeleteValue("D3PCLatency");
            key?.DeleteValue("RMDeepLlEntryLatencyUsec");
            key?.DeleteValue("PciLatencyTimerControl");
            key?.DeleteValue("Node3DLowLatency");
            key?.DeleteValue("LOWLATENCY");
            key?.DeleteValue("RmDisableRegistryCaching");
            key?.DeleteValue("RMDisablePostL2Compression");
            key?.DeleteValue("DefaultMemoryRefreshLatencyToleranceNoContext");
            key?.DeleteValue("DefaultMemoryRefreshLatencyToleranceMonitorOff");
            key?.DeleteValue("DefaultMemoryRefreshLatencyToleranceActivelyUsed");
            key?.DeleteValue("DefaultD3TransitionLatencyIdleShortTime");
            key?.DeleteValue("DefaultD3TransitionLatencyIdleNoContext");
            key?.DeleteValue("DefaultD3TransitionLatencyIdleMonitorOff");
            key?.DeleteValue("DefaultLatencyToleranceTimerPeriod");
            key?.DeleteValue("DefaultLatencyToleranceOther");
            key?.DeleteValue("DefaultLatencyToleranceNoContextMonitorOff");
            key?.DeleteValue("DefaultLatencyToleranceNoContext");
            key?.DeleteValue("DefaultLatencyToleranceMemory");
            key?.DeleteValue("DefaultLatencyToleranceIdle1MonitorOff");
            key?.DeleteValue("DefaultLatencyToleranceIdle1");
            key?.DeleteValue("DefaultLatencyToleranceIdle0MonitorOff");
            key?.DeleteValue("DefaultLatencyToleranceIdle0");
            key?.DeleteValue("DefaultD3TransitionLatencyIdleVeryLongTime");
            key?.DeleteValue("DefaultD3TransitionLatencyIdleLongTime");
            key?.DeleteValue("DefaultD3TransitionLatencyActivelyUsed");
            key?.DeleteValue("Latency");
            key?.DeleteValue("TransitionLatency");
            key?.DeleteValue("MonitorRefreshLatencyTolerance");
            key?.DeleteValue("MonitorLatencyTolerance");
            key?.DeleteValue("MiracastPerfTrackGraphicsLatency");
            key?.DeleteValue("MaxIAverageGraphicsLatencyInOneBucket");
            key.Close();
            checkBox_tolerate.Checked = false;
            checkBox_tolerate.Enabled = true;
            back_main_16.Visible = false;
        }



        void back_ui_1_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop\\WindowMetrics", true).SetValue("CaptionHeight", "-330");
            Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop\\WindowMetrics", true).SetValue("CaptionWidth", "-330");
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("buttons");
            checkBoxUI_Buttons_1.Enabled = true;
            checkBoxUI_Buttons_1.Checked = false;
            back_ui_1.Visible = false;
        } //buttons
        void back_ui_2_Click(object sender, EventArgs e)
        {
            hcmd($"regedit.exe /s {path_ui}/un_things.reg");
            if (!win10)
            {
                hcmd(@"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""UseCompactMode"" /t REG_DWORD /d ""00000000"" /f");
            }
            else
            {
                hcmd($"regedit.exe /s {path_ui}/Taskmgr_win10_default.reg");
            }

            hcmd("taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
            checkBoxUI_Buttons_2.Enabled = true;
            checkBoxUI_Buttons_2.Checked = false;
            back_ui_2.Visible = false;
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("litle_explorer_things");
        }//заглушка

        void back_ui_4_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true).DeleteValue("JPEGImportQuality");
            checkBoxUI_Buttons_4.Enabled = true;
            checkBoxUI_Buttons_4.Checked = false;
            back_ui_4.Visible = false;
        } //jpegwallappers
        void back_ui_5_Click(object sender, EventArgs e)
        {
            if (win10)
            {
                File.WriteAllText(tempfolder + @"\deficonsw10.bat", Resources.deficonsw10);
                hcmd($"{tempfolder}\\deficonsw10.bat");
            }
            else
            {
                File.WriteAllText(tempfolder + @"\defblueicons.bat", Resources.defblueicons);
                hcmd($"{tempfolder}\\defblueicons.bat");
            }
            checkBox_bluefolders.Checked = false;
            checkBox_bluefolders.Enabled = true;
            //Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true)?.DeleteValue("bluefolders");
            back_ui_5.Visible = false;
        }//bluefolders
        void back_ui_6_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}");
            hcmd(@"taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
            checkBox_contex.Checked = false;
            checkBox_contex.Enabled = true;
            back_ui_6.Visible = false;
        }//contex
        void back_ui_7_Click(object sender, EventArgs e)
        {
            hcmd("taskkill /f /im explorer.exe >nul 2>&1");
            supercmd("reg add \"HKCR\\CLSID\\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}\\InProcServer32\" /v \"\" /t REG_SZ /d \"C:\\Windows\\System32\\Windows.UI.FileExplorer.dll\" /f");
            //await Task.Delay(1000);
            Process.Start("explorer");
            checkBox_shapka.Checked = false;
            checkBox_shapka.Enabled = true;
            back_ui_7.Visible = false;
        }//shapka
        void back_ui_8_Click(object sender, EventArgs e)
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
        void back_ui_9_Click(object sender, EventArgs e)
        {
            Registry.ClassesRoot.DeleteSubKeyTree(@"DesktopBackground\Shell\explorer");
            back_ui_9.Visible = false;
            checkBox_explorer.Enabled = true;
        }
        void back_ui_10_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\Classes\SystemFileAssociations");
            Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\SystemFileAssociations");
            hcmd("taskkill /f /im ffmpeg.exe");
            File.Delete(@"C:\Windows\ffmpeg.exe");
            back_ui_10.Visible = false;
            checkBox_ffmpeg.Enabled = true;
        } //ffmpeg
        async void back_ui_11_Click(object sender, EventArgs e)
        {
            hcmd($"regsvr32 /s /u C:\\Windows\\ExplorerBlurMica.dll & taskkill /f /im explorer.exe & explorer.exe & timeout /t 1 & explorer.exe");
            await Task.Delay(1000);
            File.Delete(@"C:\Windows\ExplorerBlurMica.dll");
            File.Delete(@"C:\Windows\config.ini");
            checkBox_mica.Enabled = true;
            checkBox_mica.Checked = false;
            back_ui_11.Visible = false;
        }
        void back_animations_Click(object sender, EventArgs e)
        {
            animation = true;
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("animations");
            back_animations.Visible = false;
            checkBox_animation.Enabled = true;
        }
        void back_gpu_1_Click(object sender, EventArgs e)
        {
            hcmd("Dism /online /Disable-Feature /FeatureName:DirectPlay");
            back_gpu_1.Visible = false;
            checkBox_directplay.Checked = false;
            checkBox_directplay.Enabled = true;
        }//directplay
        void back_gpu_2_Click(object sender, EventArgs e)
        {
            checkBox_hdcp.Checked = false;
            checkBox_hdcp.Enabled = true;
            Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true)?.DeleteValue("RMHdcpKeyglobZero");
            back_gpu_2.Visible = false;
        }//hdcp
        void back_gpu_3_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.CreateSubKey(@"Software\NVIDIA Corporation\Global\NVTweak\Devices\509901423-0\Color", true)?.DeleteValue("NvCplUseColorCorrection");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", true)?.SetValue("PlatformSupportMiracast", "1");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak", true)?.DeleteValue("DisplayPowerSaving");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true)?.SetValue("EnableTiledDisplay", "0");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS", true)?.DeleteValue("EnableRID61684");
            checkBox_dopNVIDIA_tweaks.Checked = false;
            checkBox_dopNVIDIA_tweaks.Enabled = true;
            back_gpu_3.Visible = false;
        }//settings nvidia
        void back_gpu_4_Click(object sender, EventArgs e)
        {
            string[] ansel = Directory.GetFiles(@"C:\Windows\System32\DriverStore\", "NvCameraEnable.exe", SearchOption.AllDirectories);
            if (ansel.Length > 0)
            {
                string anselPath = ansel[0];
                hcmd($"{anselPath} on");
            }
            else
            {
                MessageBox.Show("Файл NvCameraEnable.exe не найден");
            }
            checkBox_ansel.Checked = false;
            checkBox_ansel.Enabled = true;
            back_gpu_4.Visible = false;
        }//ansel

        void back_pro_1_Click(object sender, EventArgs e)
        {

        }
        void back_pro_3_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", true).DeleteSubKeyTree("AU");
            checkBox_pro_3.Checked = false;
            checkBox_pro_3.Enabled = true;
            back_pro_3.Visible = false;
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
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true).DeleteSubKeyTree("csrss.exe");
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true)
.DeleteValue("NoLazyMode");
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", true)
.DeleteValue("AlwaysOn");
            checkBox_CSRSS.Checked = false;
            checkBox_CSRSS.Enabled = true;
            back_dop_7.Visible = false;
        }
        void back_pro_9_Click(object sender, EventArgs e)
        {

        }
        void back_pro_11_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\PriorityControl", true).SetValue("Win32PrioritySeparation", 0x00000002);
            checkBox_pro_11.Enabled = true;
            checkBox_pro_11.Checked = false;
            back_pro_11.Visible = false;
        }

        void back_pro_12_Click(object sender, EventArgs e)
        {
            hcmd("netsh int tcp set global autotuninglevel=normal");
            checkBox_pro_12.Checked = false;
            checkBox_pro_12.Enabled = true;
            back_pro_12.Visible = false;
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("buffer");
        }
        void back_pro_13_Click(object sender, EventArgs e)
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
        async void back_pro_14_Click(object sender, EventArgs e)
        {
            hcmd("taskkill /f /im AlwaysOnTop.exe");
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).DeleteValue("AlwaysOnTop");
            checkBox_pro_14.Checked = false;
            checkBox_pro_14.Enabled = true;
            back_pro_14.Visible = false;
            await Task.Delay(1000);
            if (File.Exists(@"C:\Windows\AlwaysOnTop.exe"))
            {
                File.Delete(@"C:\Windows\AlwaysOnTop.exe");
            }
        }


        void back_dop_1_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true).DeleteValue("Max Cached Icons");
            checkBox_picture_cache.Enabled = true;
            back_dop_1.Visible = false;
        }

        void back_dop_2_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true).DeleteValue("DefaultTTL");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", true).DeleteValue("DefaultTTL");
            checkBox_mobile_traffic.Enabled = true;
            back_dop_2.Visible = false;
        }
        void back_dop_edge_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.DeleteSubKeyTree($"SOFTWARE\\Policies\\Microsoft\\Edge");
            back_dop_edge.Visible = false;
            checkBox_edge.Checked = false;
            checkBox_edge.Enabled = true;
        }
        void back_dop_3_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement", true).DeleteValue("ScoobeSystemSettingEnabled");
            checkBox_nastroyka.Enabled = true;
            back_dop_3.Visible = false;
        }

        void back_dop_4_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys", true).SetValue("Flags", "510");
            checkBox_zalipanie.Enabled = true;
            back_dop_4.Visible = false;
        }
        void back_dop_6_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true).DeleteSubKeyTree("dwm.exe");
            checkBox_dwm.Checked = false;
            checkBox_dwm.Enabled = true;
            back_dop_6.Visible = false;
        }
        void back_dop_movetemp_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.OpenSubKey("Environment", true).SetValue("TEMP", "%USERPROFILE%\\AppData\\Local\\Temp", RegistryValueKind.ExpandString);
            Registry.CurrentUser.OpenSubKey("Environment", true).SetValue("TMP", "%USERPROFILE%\\AppData\\Local\\Temp", RegistryValueKind.ExpandString);
            checkBox_move_temp.Checked = false;
            checkBox_move_temp.Enabled = true;
            back_dop_movetemp.Visible = false;
        }

        #endregion
    }
}