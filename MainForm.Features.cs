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
                    // Скачивание в фоне с прогрессом и отменой.
                    using (var download = new DownloadForm(
                        "Режим MSI — MSI_util_v3.exe",
                        "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/MSI_util_v3.exe",
                        exe))
                    {
                        download.Run();
                        if (download.Canceled) return;
                    }
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
        #region site
        void label_site_Click(object sender, EventArgs e)
        {
            OpenUrl("https://wotbo.pro/");
        }
        #endregion
        #region inspector
        void label_inspector_Click(object sender, EventArgs e)
        {
            //inspector
            if (!Directory.Exists(path_NVidiaProfileInspectorDmW))
            {
                File.WriteAllBytes(tempfolder + @"\Base_Profile.nip", Resources.Base_Profile);

                string nviZip = $"{tempfolder}\\NVidiaProfileInspectorDmW.zip";

                if (!File.Exists(nviZip))
                {
                    using (var download = new DownloadForm(
                        "NVIDIA Profile Inspector",
                        "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/NVidiaProfileInspectorDmW.zip",
                        nviZip))
                    {
                        download.Run();
                        if (download.Canceled) return;
                    }
                }

                using (var busy = new BusyForm("NVIDIA Profile Inspector", "Распаковываем…"))
                {
                    busy.Run(delegate { ZipFile.ExtractToDirectory(nviZip, tempfolder); });
                }

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
