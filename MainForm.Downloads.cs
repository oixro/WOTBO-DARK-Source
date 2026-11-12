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
    }
}
