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
    public partial class MainForm
    {
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

        async void MainForm_Load(object sender, EventArgs e)
        {
            Log.Info("App", $"==============WOTBO {version} ==============");
            Size = Dpi.ScaleSize(this, 390, 305);
            CenterToScreen();
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
                EulaForm eula = new EulaForm();
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
    }
}
