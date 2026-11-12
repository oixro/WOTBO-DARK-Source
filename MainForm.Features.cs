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
            string exe = $"{tempfolder}\\MSI_util_v3.exe";
            if (!File.Exists(exe) && Internet.OK())
            {
                try
                {
                    AppServices.Network.DownloadFile(
                        "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/MSI_util_v3.exe",
                        exe);
                }
                catch (Exception exception)
                {
                    Log.Warn("MSI", "не удалось скачать MSI_util_v3.exe: " + exception.Message);
                }
            }

            if (File.Exists(exe))
            {
                MsiModeForm msimode = new MsiModeForm();
                Process.Start(exe);
                msimode.ShowDialog();
            }
            else
            {
                Log.Warn("MSI", "MSI_util_v3.exe не найден");
            }
        }
        #endregion
        #region defender killer
        void label_delete_defender_Click(object sender, EventArgs e)
        {
            string zip = $"{tempfolder}\\DefenderKiller.zip";
            string bat = $@"{path_dfkiller}\DefenderKiller.bat";

            if (!File.Exists(bat))
            {
                if (!File.Exists(zip))
                {
                    AppServices.Network.DownloadFile(
                        "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/DefenderKiller.zip",
                        zip);
                }

                if (Directory.Exists(path_dfkiller))
                {
                    Directory.Delete(path_dfkiller, true);
                }

                ZipFile.ExtractToDirectory(zip, path_dfkiller);
            }

            if (File.Exists(bat))
            {
                Process.Start(bat).WaitForExit();
            }
            else
            {
                Log.Warn("DefenderKiller", "DefenderKiller.bat не найден");
            }
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
    }
}
