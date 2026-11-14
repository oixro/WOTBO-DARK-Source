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
        #region все твики
        async void button1_Click(object sender, EventArgs e)
        {
            // Defender меняет службы, драйверы и брандмауэр — вместе с другими
            // твиками его применять нельзя, будет конфликт.
            if (checkBox_disabledefender.Checked)
            {
                Binder("main.defender").Apply();
                return;
            }

            #region main
            Binder("main.defender").Apply();
            Binder("main.regpack").Apply();
            Binder("main.reservedstorage").Apply();
            Binder("main.hibernate").Apply();
            Binder("main.powerscheme").Apply();
            Binder("main.mpo").Apply();
            Binder("main.usb").Apply();
            Binder("main.mitigations").Apply();
            Binder("main.mmagent").Apply();
            if (checkBox_page.Checked)
            {
                try
                {
                    Binder("main.pagefile").Apply();
                    Log.Info("Tweak:main.pagefile", "[pagefile check] Параметры файла подкачки успешно изменены. Перезагрузите систему для применения изменений.");
                }
                catch (Exception ex)
                {
                    Log.Error("Tweak:main.pagefile", "[pagefile check] Произошла ошибка: " + ex.Message, ex);
                }
                ShowNotification("Файл подкачки", "Размер файла подкачки будет изменён после перезагрузки ПК", ToolTipIcon.Info);
            }
            Binder("main.mousefix").Apply();

            //вторая
            Binder("main.audio").Apply();
            Binder("main.dwminput").Apply();
            Binder("main.powerlatency").Apply();
            Binder("pro.win32priority").Apply(); //Win32Priority
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
            Binder("gpu.hdcp").Apply();
            Binder("gpu.nvidia").Apply();
            Binder("gpu.directplay").Apply();
            #endregion
            #region ui
            Binder("ui.buttons").Apply();
            Binder("ui.explorerdetails").Apply();
            Binder("ui.jpegquality").Apply();
            Binder("ui.bluefolders").Apply();
            if (checkBox_contex.Checked)
            {
                using (TweakScope.Apply("ui.contextmenu"))
                {
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
            Binder("ui.win11header").Apply();
            Binder("ui.wotbocontext").Apply();
            Binder("ui.explorerrestart").Apply();
            Binder("ui.ffmpeg").Apply();
            Binder("ui.mica").Apply();
            Binder("ui.animations").Apply();
            #endregion
            #region dop
            Binder("extra.onedrive").Apply();
            Binder("extra.compactos").Apply();
            Binder("extra.winsxs").Apply();
            Binder("extra.tempclean").Apply();
            Binder("extra.updateclean").Apply();
            Binder("extra.iconcache").Apply();
            Binder("extra.mobiletraffic").Apply();
            Binder("extra.scoobe").Apply();
            Binder("extra.stickykeys").Apply();
            Binder("extra.dwmpriority").Apply();
            Binder("extra.edge").Apply();
            Binder("extra.movetemp").Apply();
            #endregion
            #region pro
            Binder("pro.homegallery").Apply(); //disable main & gallery folder in explorer.exe
            Binder("pro.windowsupdate").Apply(); //winupdates

            Binder("pro.csrss").Apply(); //csrss
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
            Binder("pro.bufferbloat").Apply(); //Bufferbloat
            Binder("pro.unlocker").Apply(); //unlocker
            Binder("pro.alwaysontop").Apply();//alwaysontop
            #endregion

            if (checkBox_tweak_tooltips.Checked)
            {
                ApplyTweakTooltips();
            }
        }
        #endregion
    }
}
