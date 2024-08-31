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
        public bool isEnglish = true;
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

        #endregion
        #region default crap
        public WOTBO()
        {
            InitializeComponent();
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            async void Exit() { for (Opacity = 1; Opacity > .0; Opacity -= .2) await Task.Delay(7); Close(); }
            ButtonClose.Click += (s, a) => Exit();

            FormPaint(Color.FromArgb(44, 57, 67), Color.FromArgb(35, 44, 55));

            new List<Control> { LabelHead, PanelHead, logo }.ForEach(x =>
            {
                x.MouseDown += (s, a) =>
                {
                    x.Capture = false; Capture = false; Message m = Message.Create(Handle, 0xA1, new IntPtr(2), IntPtr.Zero); base.WndProc(ref m);
                };
            });
            KeyPreview = true;
            //KeyDown += (s, e) => { if (e.KeyValue == (char)Keys.F12) Process.Start(logfile); };
            KeyDown += (s, e) =>
            {
                if (e.KeyValue == (char)Keys.F12) Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"/command Get-Content -Path \"{logfile}\" -Wait -Encoding UTF8",
                });
            };
        }
        #endregion
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
            button1.Text = "Apply";
            button_new.Text = "Apply";
            toolTip1.ToolTipTitle = "Brief description of the function:";
            label_site.Text = "8. Site";

            //main
            checkBox_disabledefender.Text = "Disable Windows Defender";
            toolTip1.SetToolTip(checkBox_disabledefender, "Initial Tooltip Text");
            checkBox_reg.Text = "Apply basic registry settings";
            toolTip1.SetToolTip(checkBox_reg, "The most important registry settings are applied");
            checkBox_gibernate.Text = "Disable hibernation mode";
            toolTip1.SetToolTip(checkBox_gibernate, "Hibernation is a power-saving state of the computer, primarily intended for laptops.\r\n(Takes up ~2gb)");
            checkBox_scheme.Text = "Import power scheme";
            toolTip1.SetToolTip(checkBox_scheme, "Adopts optimal power supply circuitry.\r\nImproves FPS stability.");
            toolTip1.SetToolTip(checkBox_mousefix, "Disables acceleration, mouse acceleration.\r\nMakes mouse movements more predictable.");
            checkBox_mpo.Text = "Disable Multi-Plane Overlay (MPO)";
            toolTip1.SetToolTip(checkBox_mpo, "Disabling Multi-Plane Overlay (MPO) can fix flicker issues in some desktop applications.\r\n" +
                "Screen flicker may occur when playing videos using hardware acceleration in Chrome.\r\n" +
                "Black screens can occur when switching from a game (or app like Whatsapp) to a browser with looped video.\r\n" +
                "Some desktop applications may flicker or stutter when resizing the window on some PC configurations.");
            checkBox_usb.Text = "Disable USB Power saving";
            toolTip1.SetToolTip(checkBox_usb, "Disables power saving mode on USB ports.");
            toolTip1.SetToolTip(checkBox_bcdedit, "Configures Windows boot loader settings");
            checkBox_mtiititit.Text = "Disable Meltdown и Spectre";
            toolTip1.SetToolTip(checkBox_mtiititit, "Disables advanced defenses against memory-based attacks, that is, attacks, \r\n" +
                "where malware manipulates memory to gain control of the system.\r\n(Takes 3-5% FPS)");
            checkBox_mmagent.Text = "Configure MM-Agent";
            toolTip1.SetToolTip(checkBox_mmagent, "Configures the memory management agent (MMAgent)\r\ndepending on the amount of RAM installed\r\nto achieve stable FPS");
            checkBox_page.Text = "Configure the swap file";
            toolTip1.SetToolTip(checkBox_page, "Applies optimal settings for the swap file\r\n(Ranges from 16 mb to 32 gb)");
            checkBox_dism.Text = "Disable reserved storage";
            toolTip1.SetToolTip(checkBox_dism, "Disable reserved space for Windows updates\r\n(Takes up ~7GB)");
            checkBox_audio.Text = "Lower Audio latency";
            toolTip1.SetToolTip(checkBox_audio, "This tweak reduces audio latency.\r\nIt positively affects all delays in the system.");
            checkBox_dwninput.Text = "Disable DWM Input";
            toolTip1.SetToolTip(checkBox_dwninput, "Makes adjustments in Desktop Window Manager (DWM)\r\nPositively affects mouse movements, making them more responsive.");
            checkBox_audioDG.Text = "Disable audio protection against copy";
            toolTip1.SetToolTip(checkBox_audioDG, "Disables a feature built into Windows\r\nthat prevents you from \"pirating\" audio files.");
            checkBox_tolerate.Text = "Stop Tolerating high DPC ISP Latencies";
            toolTip1.SetToolTip(checkBox_tolerate, "These registry settings are designed to improve the system's handling of DPC, ISR \r\n" +
                "by reducing tolerance to high latency." +
                "\r\nThis results in improved responsiveness of the mouse and the system as a whole.");
            checkBox_videoprocess.Text = "Turn off video enhance";
            toolTip1.SetToolTip(checkBox_videoprocess, "Disables automatic video processing in Windows.\r\n" +
                "Useful for HDR and non-HDR monitors.\r\nAlso has a positive effect on system responsiveness.");
            checkBox_usbport.Text = "USB Port Optimization";
            toolTip1.SetToolTip(checkBox_usbport, "Disables the power saving state of USB ports.\r\nPositive effect on the behaviour of all devices connected via USB.");
            checkBox_usbpollrate.Text = "Increase USB Polling Rate";
            toolTip1.SetToolTip(checkBox_usbpollrate, "Increases driver polling speed of USB ports.\r\nAlso has a positive effect on system and mouse responsiveness.");



            //ui
            checkBoxUI_Buttons_1.Text = "Reducing the Close, Collapse buttons size";
            toolTip1.SetToolTip(checkBoxUI_Buttons_1, "");
            checkBoxUI_Buttons_2.Text = "Minor explorer customizations";
            toolTip1.SetToolTip(checkBoxUI_Buttons_2, "Makes Explorer more pleasant to use\r\n(Removes unnecessary folders, items, etc.)");
            checkBoxUI_Buttons_3.Text = "Customize context menu";
            toolTip1.SetToolTip(checkBoxUI_Buttons_3, "Adds useful items for the context menu");
            checkBoxUI_Buttons_4.Text = "Disable desktop wallpaper compression";
            toolTip1.SetToolTip(checkBoxUI_Buttons_4, "");
            checkBox_bluefolders.Text = "Install blue folders";
            toolTip1.SetToolTip(checkBox_bluefolders, "Sets the blue folders in Explorer to replace the yellow ones.");
            checkBox_contex.Text = "Bring back the old context menu";
            toolTip1.SetToolTip(checkBox_contex, "Returns a human context menu");
            checkBox_shapka.Text = "Bring back the old explorer hat";
            toolTip1.SetToolTip(checkBox_shapka, "Returns the human menu header");
            checkBox_wotboincontex.Text = "Add WOTBO to the context menu on the desktop";
            toolTip1.SetToolTip(checkBox_wotboincontex, "");
            checkBox_explorer.Text = "Add restart explorer.exe to the context menu on the desktop";
            toolTip1.SetToolTip(checkBox_explorer, "");
            checkBox_ffmpeg.Text = "Add ffmpeg to context menu";
            toolTip1.SetToolTip(checkBox_ffmpeg, "Adds useful items to the context menu for videos\r\n(Works for avi, flac, mov,mkv,mp4,wav,weba)");
            checkBox_mica.Text = "Make the explorer translucent";
            toolTip1.SetToolTip(checkBox_mica, "");
            label_cursors.Text = "Install new cursors";

            //gpu
            label_nvcleaninstall.Text = "Download NVCleanInstall";
            toolTip1.SetToolTip(label_nvcleaninstall, "The best program to install driver on graphics card.\r\n(NVIDIA only)");
            label_ddu.Text = "Download DDU";
            toolTip1.SetToolTip(label_ddu, "Best program to uninstall video card driver");
            checkBox_directplay.Text = "Enable DirectPlay";
            toolTip1.SetToolTip(checkBox_directplay, "");
            checkBox_hdcp.Text = "Disable HDCP";
            toolTip1.SetToolTip(checkBox_hdcp, "Disables media content protection technology, \r\ndesigned to prevent illegal video copying.");
            checkBox_dopNVIDIA_tweaks.Text = "NVIDIA Minor Customizations";
            toolTip1.SetToolTip(checkBox_dopNVIDIA_tweaks, "");
            checkBox_ansel.Text = "Disable Ansel";
            toolTip1.SetToolTip(checkBox_ansel, "Disables NVIDIA Ansel, a “productivity” tool, \r\nthat allows you to create “professional-grade” in-game photos.");

            //dop
            label_activate.Text = "Activate Windows";
            toolTip1.SetToolTip(label_activate, "Opens the Windows activator");
            label_delete_defender.Text = "Remove the defender completely";
            toolTip1.SetToolTip(label_delete_defender, "Utility that allows you to remove the defender completely");
            checkBox_edgedelete.Text = "Remove Edge browser";
            toolTip1.SetToolTip(checkBox_edgedelete, "A utility that allows you to remove the built-in Edge browser");
            checkBox_onedrive.Text = "Delete OneDrive";
            toolTip1.SetToolTip(checkBox_onedrive, "");
            checkBox_WinSxS.Text = "Cleaning the WinSxS storage";
            toolTip1.SetToolTip(checkBox_WinSxS, "Clears the folder where all remaining Windows updates are stored\r\n(May free up several GB, depending on the number of installed updates)");
            checkBox_compactos.Text = "Compress OS files";
            toolTip1.SetToolTip(checkBox_compactos, "Compresses Windows system files to save space");
            checkBox_temp.Text = "Clear temporary files";
            toolTip1.SetToolTip(checkBox_temp, "Clears temporary files that are no longer used by programs");
            checkBox_updclean.Text = "Clear the Windows update cache";
            toolTip1.SetToolTip(checkBox_updclean, "Clears downloaded Windows updates");
            checkBox_picture_cache.Text = "Increase the image cache";
            toolTip1.SetToolTip(checkBox_picture_cache, "Increases the size of the image preview cache. \r\nThus, it (cache) will not be overwritten too often (raping NDD/SSD).");
            checkBox_mobile_traffic.Text = "Bypassing mobile traffic tracking";
            toolTip1.SetToolTip(checkBox_mobile_traffic, "Allows you to hide the consumption of your mobile traffic on your PC to your mobile operator.\r\nmobile traffic on your PC. Thus, you will stop paying for going over the limit.");
            checkBox_nastroyka.Text = "Remove the Windows Setup window";
            toolTip1.SetToolTip(checkBox_nastroyka, "Disables the annoying prompt at Windows startup to finish customization");
            checkBox_zalipanie.Text = "Disable key sticking";
            toolTip1.SetToolTip(checkBox_zalipanie, "Disables notification when Shift is pressed five times and that very Shift is stuck.");
            checkBox_dwm.Text = "Decrease the priority of dwm.exe";
            toolTip1.SetToolTip(checkBox_dwm, "dwm.exe is a system process responsible for window display effects in the system: \r\n" +
                "transparency effects, shadows, animation, etc.\r\nDecreasing the priority of the process allows you to reduce the \"jelly\" of the mouse.");
            checkBox_CSRSS.Text = "Increase the priority of CSRSS.exe";
            toolTip1.SetToolTip(checkBox_CSRSS, "Sets a high priority to a Windows component,\r\n which allows you to control most of the graphics instruction sets in Windows");
            checkBox_edge.Text = "Reduce Microsoft Edge's ambitions";
            checkBox_move_temp.Text = "Move TEMP to the root of the disk";
            //pro
            checkBox_pro_1.Text = "Remove Home and Gallery (W11)";
            toolTip1.SetToolTip(checkBox_pro_1, "Removes useless two folders in explorer from explorer");
            checkBox_pro_3.Text = "Disable Windows Auto Update";
            toolTip1.SetToolTip(checkBox_pro_3, "Disables automatic downloading and installation of updates,\r\nOnce applied, updates will only be installed at the user's request.");
            checkBox_pro_4.Text = "Configure Nvidia Profile Inspector";
            toolTip1.SetToolTip(checkBox_pro_4, "Applies my personal Nvidia Profile Inspector settings.");
            label_msimode.Text = "Configure MSI Mode";
            toolTip1.SetToolTip(label_msimode, "Allows you to customize device interrupts\r\n(Increases FPS, reduces latency)");
            //checkBox_pro_7.Text = "";
            toolTip1.SetToolTip(checkBox_pro_7, "Automatically allocates devices (usb, video card) to different processor cores");
            //checkBox_pro_8.Text = "";
            toolTip1.SetToolTip(checkBox_CSRSS, "Gives high priority to a Windows component that allows you to control most of the graphics instruction sets in Windows");
            checkBox_pro_9.Text = "Optimize Windows memory settings";
            toolTip1.SetToolTip(checkBox_pro_9, "Configures the system file management utility in Windows");
            checkBox_pro_10.Text = "Advanced Windows Cleanup";
            toolTip1.SetToolTip(checkBox_pro_10, "Opens a system cleanup window hidden from the normal user.");
            //checkBox_pro_11.Text = "";
            toolTip1.SetToolTip(checkBox_pro_11, "Parameter that allows you to customize the amount of time allocated to background and active processes.");
            //checkBox_pro_12.Text = "";
            toolTip1.SetToolTip(checkBox_pro_12, "Bufferbloat is the phenomenon where excessive buffering\r\ncauses an increase in packet transit time (Ping)\r\nand packet delay spread (Packetloss)");
            checkBox_pro_13.Text = "Forced deletion of files";
            toolTip1.SetToolTip(checkBox_pro_13, "Adds to the context menu (RMB) the ability to:\r\ndelete files that are used by other processes\r\ndelete any folders, even system folders\r\n");
            checkBox_pro_14.Text = "Add window pinning";
            toolTip1.SetToolTip(checkBox_pro_14, "Pinning a window by pressing Ctrl+Space");
        }
        void writelog(string line)
        {
            using (StreamWriter logs = new StreamWriter(logfile, true))
            {
                // Получение текущего времени
                DateTime now = DateTime.Now;

                // Форматирование времени с миллисекундами
                string formattedTime = now.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                logs.WriteLine($"{formattedTime} - {line}");
            }
        }
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
            Label Lbl = sender as Label;
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
            #region logs
            if (File.Exists(logfile))
            {
                if ((File.ReadAllLines(logfile).Length) > 3000)
                {
                    File.Delete(logfile);
                }
            }

            writelog($"                                    ");
            writelog($"==============Launched==============");
            writelog($"                                    ");
            #endregion
            #region version
            label_ver.Text = $"{version}";
            writelog(version);
            #endregion
            #region позиция и размеры
            Size = new System.Drawing.Size(390, 305);
            CenterToScreen();
            panel1.Location = new System.Drawing.Point(105, 36);
            #endregion
            #region проверка запущенна ли, eula и язык
            writelog("");
            writelog("Язык");
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("eula") == null)
            {
                writelog("eula null");
                Form2 eula = new Form2();
                eula.ShowDialog();
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("Language") == null)
            {
                writelog("language null");
                if (uilanguage == "ru")
                {
                    writelog($"ui lanuage - {uilanguage}");
                    Registry.CurrentUser.CreateSubKey(@"Software\oixro\wotbo", true).SetValue("Language", uilanguage);
                    isEnglish = false;
                }
                else
                {
                    writelog($"ui lanuage - {uilanguage}");
                    Registry.CurrentUser.CreateSubKey(@"Software\oixro\wotbo", true).SetValue("Language", uilanguage);
                    isEnglish = true;
                    english();
                }
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("Language") != null)
            {
                writelog("language не null");
                if ((Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo").GetValue("Language").ToString()) == "en")
                {
                    writelog("language - en");
                    isEnglish = true;
                    english();
                }
                else
                {
                    writelog("language - ru");
                    isEnglish = false;
                }
            }
            writelog("");
            writelog("InstanceChecker");
            if (!InstanceChecker.TakeMemory())
            {
                writelog("InstanceExists, relaunch");
                //if (!isEnglish)
                //{
                //    MessageBox.Show("Другая копия программы уже запущена\nИметь две открытии копии программы не рекомендуется.", "WOTBO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //}
                //else
                //{
                //    MessageBox.Show("It is not recommended to have two copies of the program open.", "WOTBO", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //}
                hcmd($"taskkill /f /im {exename} && \"{exepath}\"");
                writelog("cmd на перезапуск вызван, ждём когда приебашит меня");
            }
            #endregion
            #region temp
            writelog("");
            writelog("tempfolder");
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
            writelog("");
            writelog("отключаем чеки");
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
            } //reserved
            if (!File.Exists(@"C:\hiberfil.sys"))
            {
                writelog("hiberfila нету");
                checkBox_gibernate.Enabled = false;
                back_main_3.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("imported_powerscheme")) == 1)
            {
                writelog("imported_powerscheme был применён");
                checkBox_scheme.Enabled = false;
                back_main_4.Visible = true;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("mousefix")) == 1)
            {
                writelog("mousefix был применён");
                checkBox_mousefix.Enabled = false;
                back_main_5.Visible = true;
            }
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
                writelog("bcdedit был применён");
                checkBox_bcdedit.Enabled = false;
                back_main_8.Visible = true;
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
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pagefile")) == 1)
            {
                writelog("pagefile был применён");
                checkBox_page.Enabled = false;
                back_main_11.Visible = true;
            }
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
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("contex")) == 1)
            {
                writelog("contex был применён");
                checkBoxUI_Buttons_3.Enabled = false;
                back_ui_3.Visible = true;
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
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pro_9")) == 1)
            {
                writelog("pro_9 был применён");
                checkBox_pro_9.Enabled = false;
            }
            if (Convert.ToInt32(Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo")?.GetValue("pro_7")) == 1)
            {
                writelog("pro_7 был применён");
                checkBox_pro_7.Enabled = false;

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
            if (!Directory.Exists($@"{Environment.GetEnvironmentVariable("userprofile")}\OneDrive"))
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
            if (Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio")?.GetValue("DisableProtectedAudio") != null)
            {
                writelog("DisableProtectedAudio был применён");
                checkBox_audioDG.Enabled = false;
                back_main_15.Visible = true;
            }
            if (Convert.ToString(Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power")?.GetValue("ExitLatency")) == "1")
            {
                writelog("Power\\ExitLatency был применён");
                checkBox_tolerate.Enabled = false;
                back_main_16.Visible = true;
            }
            if (Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\VideoSettings") != null)
            {
                writelog("VideoSettings был применён");
                checkBox_videoprocess.Enabled = false;
                back_main_17.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Services\usbhub\hubg")?.GetValue("DisableOnSoftRemove") != null)
            {
                writelog("Services\\usbhub\\hubg\\DisableOnSoftRemove был применён");
                checkBox_usbport.Enabled = false;
                back_main_18.Visible = true;
            }
            if (Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{36fc9e60-c465-11cf-8056-444553540000}\0000")?.GetValue("IdleEnable") != null)
            {
                writelog("IdleEnable был применён");
                checkBox_usbpollrate.Enabled = false;
                back_main_19.Visible = true;
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
            #region получаем инфу о видеодырке
            writelog("");
            writelog("gpu info");
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
                checkBox_pro_4.Enabled = false;
                checkBox_dopNVIDIA_tweaks.Visible = false;
                checkBox_hdcp.Visible = false;
            }//не nvidia
            #endregion
            #region проверка версии Windows
            writelog("");
            writelog("windows info");
            RegistryKey winver = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");
            string buildnumber = (string)winver.GetValue("CurrentBuild");
            string DisplayVersion = (string)winver.GetValue("DisplayVersion");
            if (Convert.ToUInt16(buildnumber) >= 22000) // проверка на Windows 11
            {
                win10 = false;
                writelog($"Windows is win 10? {win10}");
                label_winver.Text = "Windows 11 " + DisplayVersion + " Build:" + buildnumber;
                if (Convert.ToUInt16(buildnumber) >= 26100)
                {
                    writelog($"Windows 11 build : {buildnumber}");
                }
            }
            else if (Convert.ToUInt16(buildnumber) <= 19042) // проверка на Windows 10 <20H2 
            {
                writelog($"Windows 10 <20H2 ");
                win10 = true;
                MessageBox.Show("Рекомендуется использовать Windows 10 20H2 и выше! \nЭффективность настройки не гарантирована",
                    "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                label_winver.Text = "(Old) Windows 10 " + DisplayVersion + " Build:" + buildnumber;
            }
            else if (Convert.ToUInt16(buildnumber) >= 19042) // проверка на Windows 10
            {
                writelog($"Windows 10 {buildnumber} ");
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
            writelog("");
            writelog("проверка инет + обнов");
            if (!Internet.OK())
            {
                writelog($"инета нету");
                checkBox_bluefolders.Enabled = false;
                label_nvcleaninstall.Enabled = false;
                checkBox_edgedelete.Enabled = false;
                label_activate.Enabled = false;
                label_ddu.Enabled = false;
                label_progs.Enabled = false;
                label_delete_defender.Enabled = false;
                checkBox_mmagent.Enabled = false;
                checkBox_pro_13.Enabled = false;
                checkBox_ffmpeg.Enabled = false;
                checkBox_mica.Enabled = false;
                label_cursors.Enabled = false;
                checkBox_pro_4.Enabled = false;
                if (!isEnglish)
                {
                    MessageBox.Show("Нет доступа в интернет!\nПроврека на обновления, и некоторые функции недоступны.", "Windows optimization tool by oixro (WOTBO)",
        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("No internet access!\nChecking for updates and some features are not available.", "Windows optimization tool by oixro (WOTBO)",
MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

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
                                    if (!isEnglish)
                                    {
                                        MessageBox.Show($"Текущая версия - {curver}\nДоступна новая - {verjson}\nБудет выполнено обновление\n\n" +
                                        $"Список изменений:\n {File.ReadAllText("lastchange.json")}", "", MessageBoxButtons.OK);
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Current version - {curver}\nA new one is available - {verjson}\nAn update will be performed\n\n" +
                                        $"Changelog:\n {File.ReadAllText("lastchange.json")}", "", MessageBoxButtons.OK);
                                    }
                                    writelog($"скачиваем новую");
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
                                writelog($"версия dev");
                                label_ver.Text = $"d{version}";
                            }

                        }
                    }
                }
                catch { } //release
            }
            #endregion
            #region checkforwin10
            writelog("");
            writelog("checkforwin10");
            if (win10)
            {
                checkBox_contex.Enabled = false;
                checkBox_pro_1.Enabled = false;
                checkBox_shapka.Enabled = false;
                writelog($"чеки под win 11 для win10 отключены");
            }
            #endregion
            #region планый запуск
            writelog("");
            for (Opacity = 0; Opacity < 1; Opacity += .2) await Task.Delay(10);
            writelog($"плавно открылись");
            #endregion
            #region распаковка в temp
            writelog("");
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
            writelog($"Audio_Lantency.reg распакован");
            File.WriteAllText(tempfolder + @"\updates.reg", Resources.updates);
            writelog($"updates.reg распакован");
            #endregion
            #region bitlocker
            try
            {
                writelog("");
                writelog("try bitlocker");
                hcmd("manage-bde -off C: & manage-bde -off D: & manage-bde -off E: & manage-bde -off F: & manage-bde -off G:");
            }
            catch { }
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
            writelog("closed");
            if (Directory.Exists(@"C:\Windows\oixro"))
            {
                try { Directory.Delete(@"C:\Windows\oixro", true); } catch { }
            }
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
        #region все твики
        async void button1_Click(object sender, EventArgs e)
        {
            #region main
            if (checkBox_disabledefender.Checked)
            {
                if (!isEnglish)
                {
                    MessageBox.Show("Защитник будет отключен!" +
                        "\nЗащита от эксполитов не будет работать." +
                        "\nОна нужна для работы некоторых античитов." +
                        "\nНо восстановить защитник можно через вкладку дополнительно.\n" +
                        "", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("The defender will be disabled!" +
    "\nThe expolite defense won't work." +
    "\nIt's needed for some anti-chips to work." +
    "\nBut you can restore the defender through the advanced tab.\n" +
    "", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

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
                back_main_12.Visible = true;
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("defenderdisabled", 1);
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
                if (!isEnglish)
                {
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
                else
                {
                    DialogResult result = MessageBox.Show("Mousefix will work correctly only when the Windows scale is 100%. \nAre you sure you have the correct scale set?",
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
            }

            //вторая
            if (checkBox_audio.Checked)
            {
                supercmd($@"regedit /s {tempfolder}\Audio_Lantency.reg");
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
            if (checkBox_audioDG.Checked)
            {
                RegistryKey key;
                key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Audio");
                key?.SetValue("DisableSpatialOnComboEndpoints", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DisableProtectedAudioDG", 0x00000001, RegistryValueKind.DWord);
                key?.SetValue("DisableProtectedAudio", 0x00000001, RegistryValueKind.DWord);
                key.Close();
                checkBox_audioDG.Checked = false;
                checkBox_audioDG.Enabled = false;
                back_main_15.Visible = true;
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
            if (checkBox_videoprocess.Checked)
            {
                RegistryKey key;
                key = Registry.CurrentUser.CreateSubKey($"Software\\Microsoft\\Windows\\CurrentVersion\\VideoSettings");
                key?.SetValue("EnableAutoEnhanceDuringPlayback", 0x00000000, RegistryValueKind.DWord);
                key.Close();
                checkBox_videoprocess.Checked = false;
                checkBox_videoprocess.Enabled = false;
                back_main_17.Visible = true;
            }
            if (checkBox_usbport.Checked)
            {
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
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Services\\usbhub\\hubg");
                key?.SetValue("DisableOnSoftRemove", 0x00000001, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SOFTWARE\\Policies\\Microsoft\\Windows\\EnhancedStorageDevices");
                key?.SetValue("TCGSecurityActivationDisabled", 0x00000001, RegistryValueKind.DWord);
                key.Close();
                checkBox_usbport.Checked = false;
                checkBox_usbport.Enabled = false;
                back_main_18.Visible = true;
            }
            if (checkBox_usbpollrate.Checked)
            {
                RegistryKey key;
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}");
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0000");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0001");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0002");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0003");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0004");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0045");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0005");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0006");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0007");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0008");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0009");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0010");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0011");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0012");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0013");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0014");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0015");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0016");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0017");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0018");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0019");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0020");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0021");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0022");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0023");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0024");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0025");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0026");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0027");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0028");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0029");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\CurrentControlSet\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0030");
                key?.SetValue("IdleEnable", 0x00000000, RegistryValueKind.DWord);
                key.Close();
                checkBox_usbpollrate.Checked = false;
                checkBox_usbpollrate.Enabled = false;
                back_main_19.Visible = true;
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
                    hcmd($"{tempfolder}\\blueicons.bat");
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

                using (WebClient wc = new WebClient())
                    if (!File.Exists($"{tempfolder}\\ffmpeg.zip"))
                    {
                        await wc.DownloadFileTaskAsync(new Uri("https://github.com/GyanD/codexffmpeg/releases/download/7.0.1/ffmpeg-7.0.1-essentials_build.zip"), $"{tempfolder}\\ffmpeg.zip");
                        await wc.DownloadFileTaskAsync(new Uri("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/ffmpeg.reg"), $"{tempfolder}\\ffmpeg.reg");
                        ZipFile.ExtractToDirectory($"{tempfolder}\\ffmpeg.zip", tempfolder);
                    }
                if (File.Exists(@"C:\Windows\ffmpeg.exe"))
                {
                    hcmd("taskkill /f /im ffmpeg.exe");
                    File.Delete(@"C:\Windows\ffmpeg.exe");
                }
                File.Copy($@"{tempfolder}\ffmpeg-7.0.1-essentials_build\bin\ffmpeg.exe", @"C:\Windows\ffmpeg.exe");
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
            #endregion
            #region dop
            if (checkBox_edgedelete.Checked)
            {
                if (!isEnglish)
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
                else
                {
                    DialogResult result = MessageBox.Show("Edge will be deleted, keep WebView? ", "", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
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

            }
            if (checkBox_onedrive.Checked)
            {
                void ExecutePowerShellCommand(string command)
                {
                    ProcessStartInfo processInfo = new ProcessStartInfo("powershell.exe", "-Command " + command);
                    processInfo.CreateNoWindow = true;
                    processInfo.UseShellExecute = false;
                    processInfo.RedirectStandardError = true;
                    processInfo.RedirectStandardOutput = true;

                    Process process = Process.Start(processInfo);
                    process.WaitForExit();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine("OUTPUT: " + output);
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine("ERROR: " + error);
                    }
                }
                try
                {
                    // Удаление UWP приложения OneDrive
                    ExecutePowerShellCommand("Get-AppxPackage *OneDrive* | Remove-AppxPackage");
                    writelog("UWP версия OneDrive успешно удалена.");
                    hcmd($"{Environment.ExpandEnvironmentVariables(@"%SystemRoot%\SysWOW64\OneDriveSetup.exe")} /uninstall");
                    writelog("не UWP onedrive удалён");
                }
                catch (Exception ex)
                {
                    writelog("Произошла ошибка: " + ex.Message);
                }
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

                // Удаление всех файлов
                foreach (string file in Directory.GetFiles(tempPath))
                {
                    try
                    {
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
            if (checkBox_pro_4.Checked)
            {
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
                checkBox_pro_4.Checked = false;
            } //inspector
            if (checkBox_pro_7.Checked)
            {
                hcmd($"{tempfolder}\\affinity.bat");
                checkBox_pro_7.Checked = false;
                checkBox_pro_7.Enabled = false;
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("pro_7", 1);
            } //affinity
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
            #endregion
        }
        void label_cursors_Click_1(object sender, EventArgs e)
        {
            using (WebClient wc = new WebClient())
                if (!File.Exists($"{tempfolder}\\cursors.zip"))
                {
                    wc.DownloadFile("https://raw.githubusercontent.com/oixro/WOTBO/main/resources/cursors.zip", $"{tempfolder}\\cursors.zip");
                    ZipFile.ExtractToDirectory($"{tempfolder}\\cursors.zip", tempfolder);
                }
            if (!isEnglish)
            {
                if (MessageBox.Show($"Установить светлый или тёмный курсор?\nДа - светлый\nНет - тёмный", "WOTBO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    InstallCursor($@"{tempfolder}\cursors\light\small\base\Install.inf");
                }
                else
                {
                    InstallCursor($@"{tempfolder}\cursors\dark\small\base\Install.inf");
                }
            }
            else
            {
                if (MessageBox.Show($"Set a light or dark cursor?\nYes, light. \nNo, dark.", "WOTBO", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    InstallCursor($@"{tempfolder}\cursors\light\small\base\Install.inf");
                }
                else
                {
                    InstallCursor($@"{tempfolder}\cursors\dark\small\base\Install.inf");
                }
            }
        }
        void label_activate_Click(object sender, EventArgs e)
        {
            powershell("irm https://get.activated.win | iex");
        }
        void label_msimode_Click(object sender, EventArgs e)
        {
            if (Internet.OK())
            {
                try
                {
                    File.WriteAllText(tempfolder + @"\affinity.bat", Resources.affinity);
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
        void label_delete_defender_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("DefenderKiller полностью удалит защитник.\nВосстановить его не получится!\nЗапустить DefenderKiller?", "",
    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
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
                checkBox_disabledefender.Visible = false;
                back_main_12.Visible = false;
            }
            else
            {
                //label_delete_defender.Enabled = false;
            }
        }
        void back_dop_movetemp_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.OpenSubKey("Environment", true).SetValue("TEMP", "%USERPROFILE%\\AppData\\Local\\Temp", RegistryValueKind.ExpandString);
            Registry.CurrentUser.OpenSubKey("Environment", true).SetValue("TMP", "%USERPROFILE%\\AppData\\Local\\Temp", RegistryValueKind.ExpandString);
            checkBox_move_temp.Checked = false;
            checkBox_move_temp.Enabled = true;
            back_dop_movetemp.Visible = false;
        }
        void label_site_Click(object sender, EventArgs e)
        {
            Process.Start("https://wotbo.pro/");
        }
        #endregion
        #region перемещение по пунктам
        void label1_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                    continue;
                if (pnl == PanelHead)
                    continue;
                pnl.Visible = false;
            }
            panel1.Visible = true;
            panel1.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = new Point(panel1.Location.X * 2, panel1.Location.Y + panel1.Height);

        } //main        
        void button_main_1_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                    continue;
                if (pnl == PanelHead)
                    continue;
                pnl.Visible = false;
            }
            panel1.Visible = true;
            panel1.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = new Point(panel1.Location.X * 2, panel1.Location.Y + panel1.Height);
        }

        void button_main_2_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                    continue;
                if (pnl == PanelHead)
                    continue;
                pnl.Visible = false;
            }
            panel_main_2.Visible = true;
            panel_main_2.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = new Point(panel1.Location.X * 2, panel1.Location.Y + panel1.Height);
        }
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
                    continue;
                }
                if (pnl == PanelHead)
                {
                    continue;
                }
                pnl.Visible = false;
            }
            panel2.Visible = true;
            panel2.Location = panel1.Location;

        } //gpu        
        void label3_Click(object sender, EventArgs e) //dop
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                    continue;
                if (pnl == PanelHead)
                    continue;
                pnl.Visible = false;
            }
            panel_dop.Visible = true;
            panel_dop.Location = panel1.Location;
            button_dop_new_1.Visible = true;
            button_dop_new_2.Visible = true;
            panel_dop_navigate.Visible = true;
            panel_dop_navigate.Location = new Point(panel1.Location.X * 2, panel1.Location.Y + panel1.Height);
        }
        void button_dop_new_1_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                    continue;
                if (pnl == PanelHead)
                    continue;
                pnl.Visible = false;
            }
            panel_dop.Visible = true;
            panel_dop.Location = panel1.Location;
            panel_dop_navigate.Visible = true;
        }
        void button_dop_new_2_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                    continue;
                if (pnl == PanelHead)
                    continue;
                pnl.Visible = false;
            }
            panel_dop_2.Visible = true;
            panel_dop_2.Location = panel1.Location;
            panel_dop_navigate.Visible = true;
        }
        void label6_Click(object sender, EventArgs e) // проги
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue;
                }
                if (pnl == PanelHead)
                {
                    continue;
                }
                pnl.Visible = false;
            }
            panel_sorry.Visible = true;
            panel_sorry.Location = panel1.Location;
        }
        void label8_Click(object sender, EventArgs e) //pc info
        {
            Process Win32_Processor_GET_Name = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = "/c wmic PATH Win32_Processor GET Name",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });
            label7.Text = "CPU: " + ((Win32_Processor_GET_Name.StandardOutput.ReadToEnd()).Replace("Name", "")).Trim();

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
                    continue;
                }
                if (pnl == PanelHead)
                {
                    continue;
                }
                pnl.Visible = false;
            }
            panel_5.Visible = true;
            panel_5.Location = panel1.Location;
            writelog(label7.Text);
        }
        void label_interface_Click(object sender, EventArgs e)
        {

            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue;
                }
                if (pnl == PanelHead)
                {
                    continue;
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
        void button_ui_1_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue;
                }
                if (pnl == PanelHead)
                {
                    continue;
                }
                pnl.Visible = false;
            }
            panel_ui_1.Visible = true;
            panel_ui_1.Location = panel1.Location;
            panel_ui_navigate.Visible = true;
        }
        void button_ui_2_Click(object sender, EventArgs e)
        {
            foreach (Panel pnl in Controls.OfType<Panel>())
            {
                if (pnl == PanelMain)
                {
                    continue;
                }
                if (pnl == PanelHead)
                {
                    continue;
                }
                pnl.Visible = false;
            }
            panel_ui_2.Visible = true;
            panel_ui_2.Location = panel1.Location;
            panel_ui_navigate.Visible = true;
        }
        #endregion
        #region download progs
        void label_download_1_Click(object sender, EventArgs e)
        {
            Process.Start("https://win10tweaker.ru/");
        }
        void label_download_2_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.6.9/npp.8.6.9.Installer.x64.exe");
        }
        void label_download_3_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.msi.com/Landing/afterburner/graphics-cards");
        }
        void label_download_4_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.7-zip.org/a/7z2408-x64.exe");
        }
        void label_download_5_Click(object sender, EventArgs e)
        {
            Process.Start("https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe");
        }
        void label_download_6_Click(object sender, EventArgs e)
        {
            Process.Start("https://dl.comss.org/download/Visual-C-Runtimes-All-in-One-May-2024.zip");
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
            Process.Start("https://cdn-fastly.obsproject.com/downloads/OBS-Studio-30.2.3-Windows-Installer.exe");
        }
        void label_download_13_Click(object sender, EventArgs e)
        {
            Process.Start("https://files2.codecguide.com/K-Lite_Codec_Pack_1850_Mega.exe");
        }
        void label_download_14_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.fosshub.com/qBittorrent.html");
        }
        void label_download_15_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.hwinfo.com/files/hwi_810.zip");
        }
        void label_download_16_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.google.com/chrome/");
        }
        void label_download_17_Click(object sender, EventArgs e)
        {
            Process.Start("https://static.centbrowser.com/win_stable/5.1.1130.129/centbrowser_5.1.1130.129_x64.exe");
        }
        void label_download_18_Click(object sender, EventArgs e)
        {
            Process.Start("https://diskanalyzer.com/files/wiztree_4_20_portable.zip");
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
            #region old
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
            #endregion
            #region new
            if (win10)
            {
                hcmd($"regedit.exe /s {path_regpack}/w10/w10_AIO_RESTORE.reg");
            }
            else
            {
                supercmd($"regedit.exe /s {path_regpack}/w11/w11_AIO_RESTORE.reg");
            }
            supercmd($"regedit.exe /s {path_regpack}/foldernetworkX_restore.reg");
            hcmd($"regedit.exe /s {path_regpack}/REG_AIO_RESTORE.reg");
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
            #region old
            //File.WriteAllText(tempfolder + @"\restoreWINDEF10.reg", Resources.restoreWINDEF10);
            //File.WriteAllText(tempfolder + @"\restoreWINDEF11.reg", Resources.restoreWINDEF11);
            //if (win10)
            //{
            //    supercmd($"regedit.exe /s {tempfolder}\\restoreWINDEF10.reg");
            //}
            //else
            //{
            //    supercmd($"regedit.exe /s {tempfolder}\\restoreWINDEF11.reg");
            //}
            //supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_10_11\\AIO_ON.reg");
            //supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\webthreatdefsvc\\webthreatdefsvc_ON.reg");
            //supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\webthreatdefusersvc\\webthreatdefusersvc_ON.reg");
            //supercmd($"regedit.exe /s {tempfolder}\\services_off\\antimalwareserviceexecutable\\Win_11\\!webthreatdefusersvc_XXX\\win11defsubsvcX_restore.bat");
            #endregion
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
            checkBox_disabledefender.Enabled = false;
            checkBox_disabledefender.Checked = false;
            back_main_12.Visible = true;
            Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).DeleteValue("defenderdisabled");
            checkBox_disabledefender.Enabled = true;
            back_main_12.Visible = false;
            MessageBox.Show("Для восстановления работы защитника выполните перезагрузку!", "", MessageBoxButtons.OK, MessageBoxIcon.Information);

        } //restoreDEF
        void back_main_13_Click(object sender, EventArgs e) //Audio_Lantency_delete.reg
        {
            supercmd($@"regedit /s {tempfolder}\Audio_Lantency_delete.reg");
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

        void back_main_15_Click(object sender, EventArgs e)
        {
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Audio");
            key?.SetValue("DisableSpatialOnComboEndpoints", 0x00000000, RegistryValueKind.DWord);
            key?.DeleteValue("DisableProtectedAudioDG");
            key?.DeleteValue("DisableProtectedAudio");
            key.Close();
            checkBox_audioDG.Checked = false;
            checkBox_audioDG.Enabled = true;
            back_main_15.Visible = false;
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

        void back_main_17_Click(object sender, EventArgs e)
        {
            Registry.CurrentUser.DeleteSubKeyTree($"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\VideoSettings");
            checkBox_videoprocess.Checked = false;
            checkBox_videoprocess.Enabled = true;
            back_main_17.Visible = false;
        }

        void back_main_18_Click(object sender, EventArgs e)
        {
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
            checkBox_usbport.Checked = false;
            checkBox_usbport.Enabled = true;
            back_main_18.Visible = false;
        }

        void back_main_19_Click(object sender, EventArgs e)
        {
            RegistryKey key;
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0000");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0001");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0002");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0003");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0004");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0045");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0005");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0006");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0007");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0008");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0009");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0010");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0011");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0012");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0013");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0014");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0015");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0016");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0017");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0018");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0019");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0020");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0021");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0022");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0023");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0024");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0025");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0026");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0027");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0028");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0029");
            key?.DeleteValue("IdleEnable");
            key = Registry.LocalMachine.CreateSubKey($"SYSTEM\\ControlSet001\\Control\\Class\\{{36fc9e60-c465-11cf-8056-444553540000}}\\0030");
            key?.DeleteValue("IdleEnable");
            key.Close();
            checkBox_usbpollrate.Checked = false;
            checkBox_usbpollrate.Enabled = true;
            back_main_19.Visible = false;
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
        #region language
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



        #endregion


    }
}