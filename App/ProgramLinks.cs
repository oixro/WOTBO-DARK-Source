using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO.App.Logging;
using WOTBO.Services.Network;

namespace WOTBO.App
{
    /// <summary>Ссылки кнопок вкладки «Скачать программы».</summary>
    public static class ProgramLinks
    {
        private const string Category = "Programs";

        private sealed class Link
        {
            public string Control;
            public string Url;
            public string LatestKey;
            public string Fallback;
        }

        private static readonly Link[] Links =
        {
            Direct("label_download_1", "https://win10tweaker.ru/"),
            Latest("label_download_2", "notepadpp", "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.7.5/npp.8.7.5.Installer.x64.exe"),
            Direct("label_download_3", "https://www.msi.com/Landing/afterburner/graphics-cards"),
            Latest("label_download_4", "7zip", "https://www.7-zip.org/a/7z2409-x64.exe"),
            Direct("label_download_5", "https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E18C4C/dxwebsetup.exe"),
            Direct("label_download_6", "https://dl.comss.org/download/Visual-C-Runtimes-All-in-One-Nov-2024.zip"),
            Direct("label_download_7", "https://cdn.akamai.steamstatic.com/client/installer/SteamSetup.exe"),
            Latest("label_download_8", "sharex", "https://github.com/ShareX/ShareX/releases/download/v17.0.0/ShareX-17.0.0-setup.exe"),
            Direct("label_download_9", "https://telegram.org/dl/desktop/win64_portable"),
            Direct("label_download_10", "https://discord.com/api/downloads/distributions/app/installers/latest?channel=stable&platform=win&arch=x64"),
            Latest("label_download_11", "obs", "https://cdn-fastly.obsproject.com/downloads/OBS-Studio-31.0.0-Windows-Installer.exe"),
            Latest("label_download_13", "klite", "https://files2.codecguide.com/K-Lite_Codec_Pack_1875_Mega.exe"),
            Latest("label_download_14", "qbittorrent", "https://deac-fra.dl.sourceforge.net/project/qbittorrent/qbittorrent-win32/qbittorrent-5.0.3/qbittorrent_5.0.3_x64_setup.exe?viasf=1"),
            Direct("label_download_15", "https://www.hwinfo.com/files/hwi64_860.exe"),
            Direct("label_download_16", "https://www.google.com/chrome/"),
            Direct("label_download_17", "https://static.centbrowser.com/win_stable/5.1.1130.129/centbrowser_5.1.1130.129_x64.exe"),
            Latest("label_download_18", "wiztree", "https://diskanalyzer.com/files/wiztree_4_23_portable.zip"),
            Direct("label_download_19", "https://launcher-public-service-prod06.ol.epicgames.com/launcher/api/installer/download/EpicGamesLauncherInstaller.msi"),
            Direct("label_download_20", "https://anydesk.com/ru/downloads/thank-you?dv=win_exe"),
            Direct("label_download_21", "https://www.faststonesoft.net/DN/FSViewerSetup85.exe"),
            Latest("label_download_22", "dotnet6", "https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-6.0.28-windows-x64-installer"),
            Latest("label_download_23", "crystaldiskinfo", "https://deac-riga.dl.sourceforge.net/project/crystaldiskinfo/9.2.3/CrystalDiskInfo9_2_3.exe"),
            Direct("label_download_24", "https://www.amd.com/en/support/download/drivers.html"),
            Direct("label_ddu", "https://www.wagnardsoft.com/display-driver-uninstaller-DDU-"),
            Direct("label_nvcleaninstall", "https://www.techpowerup.com/download/techpowerup-nvcleanstall/"),
        };

        public static void Attach(Form form, Action<string> openUrl)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (openUrl == null) throw new ArgumentNullException(nameof(openUrl));

            foreach (Link link in Links)
            {
                Control[] controls = form.Controls.Find(link.Control, true);
                if (controls.Length == 0) continue;

                Link captured = link;
                controls[0].Click += async (sender, args) => await OpenAsync(captured, openUrl);
            }
        }

        private static async Task OpenAsync(Link link, Action<string> openUrl)
        {
            string url = link.Url;
            if (!string.IsNullOrEmpty(link.LatestKey))
            {
                try
                {
                    string latest = await AppServices.LatestVersions.GetProgramUrlAsync(link.LatestKey);
                    if (!string.IsNullOrEmpty(latest))
                    {
                        url = latest;
                    }
                }
                catch (Exception exception)
                {
                    Log.Warn(Category, "не удалось получить последнюю версию " + link.LatestKey + ": " + exception.Message);
                }

                if (string.IsNullOrEmpty(url))
                {
                    url = link.Fallback;
                }
            }

            openUrl(url);
        }

        private static Link Direct(string control, string url)
        {
            return new Link { Control = control, Url = url };
        }

        private static Link Latest(string control, string key, string fallback)
        {
            return new Link { Control = control, LatestKey = key, Fallback = fallback };
        }
    }
}
