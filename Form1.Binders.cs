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
namespace Project
{
    public partial class WOTBO
    {
        #region TweakUiBinder
        private TweakUiBinder Bind(
            string id,
            CheckBox checkBox,
            PictureBox rollback,
            string detectMessage = null,
            Action onDetected = null,
            Action beforeApply = null,
            Action inApply = null,
            Action afterApply = null,
            Action inRollback = null,
            Action afterRollback = null,
            Func<bool> confirmApply = null,
            bool disableOnApply = true,
            bool showRollbackOnApply = true,
            bool showRollbackOnDetect = true,
            bool uncheckOnDetect = false)
        {
            var binder = new TweakUiBinder(
                id,
                checkBox,
                rollback,
                detectMessage,
                onDetected,
                beforeApply,
                inApply,
                afterApply,
                inRollback,
                afterRollback,
                confirmApply,
                disableOnApply,
                showRollbackOnApply,
                showRollbackOnDetect,
                uncheckOnDetect);

            tweakBinders[id] = binder;
            if (rollback != null)
            {
                rollback.Click += (sender, args) => binder.Rollback();
            }

            return binder;
        }

        private TweakUiBinder Binder(string id)
        {
            return tweakBinders[id];
        }

        private void SetupTweakBinders()
        {
            Bind("main.defender", checkBox_disabledefender, back_main_12,
                "defenderdisabled был применён",
                onDetected: () =>
                {
                    label_delete_defender.Enabled = true;
                    label_kalorant.Visible = true;
                },
                beforeApply: () => MessageBox.Show(
                    "Защитник будет отключен!\nЗащита от эксполитов не будет работать.\n" +
                    "Она нужна для работы некоторых античитов.\n" +
                    "Но восстановить защитник можно через вкладку дополнительно.\n",
                    "", MessageBoxButtons.OK, MessageBoxIcon.Warning),
                afterApply: () =>
                {
                    label_kalorant.Visible = true;
                    ShowNotification("Защитник", "Рекомендуется перезагрузить компьютер для полноценного отключения защитника!", ToolTipIcon.Warning);
                },
                afterRollback: () => ShowNotification("Защитник Windows", "Работа защитника будет восстановлена после перезагруки ПК", ToolTipIcon.Warning));

            Bind("main.regpack", checkBox_reg, back_main_1, "regpack был применён",
                afterApply: () => ShowNotification("Настройки Реестра", "Рекомендуется перезагрузить ПК после применения данного твика!", ToolTipIcon.Warning));
            Bind("main.reservedstorage", checkBox_dism, back_main_2);
            Bind("main.hibernate", checkBox_gibernate, back_main_3, "hiberfil'a нету");
            Bind("main.powerscheme", checkBox_scheme, back_main_4, "imported_powerscheme был применён");
            Bind("main.mpo", checkBox_mpo, back_main_6, "MPO был применён");
            Bind("main.usb", checkBox_usb, back_main_7, "usbpowersaving был применён");
            Bind("main.mitigations", checkBox_mtiititit, back_main_9, "spectre and meltdown был применён");
            Bind("main.mmagent", checkBox_mmagent, back_main_10, "mmagent был применён");
            Bind("main.pagefile", checkBox_page, back_main_11);
            Bind("main.mousefix", checkBox_mousefix, back_main_5);
            Bind("main.audio", checkBox_audio, back_main_13, "AllowHeadlessExecution BackgroundAudioPolicy был применён");
            Bind("main.dwminput", checkBox_dwninput, back_main_14, "DwmInputUsesIoCompletionPort был применён");
            Bind("main.powerlatency", checkBox_tolerate, back_main_16, "Power\\ExitLatency был применён");
            Bind("pro.win32priority", checkBox_pro_11, back_pro_11, "Win32PrioritySeparation был применён",
                inApply: () => new Win32Priority().ShowDialog());

            Bind("gpu.hdcp", checkBox_hdcp, back_gpu_2);
            Bind("gpu.nvidia", checkBox_dopNVIDIA_tweaks, back_gpu_3);
            Bind("gpu.directplay", checkBox_directplay, back_gpu_1);

            Bind("ui.buttons", checkBoxUI_Buttons_1, back_ui_1, "buttons был применён");
            Bind("ui.explorerdetails", checkBoxUI_Buttons_2, back_ui_2, "litle_explorer_things был применён");
            Bind("ui.jpegquality", checkBoxUI_Buttons_4, back_ui_4, "JPEGImportQuality был применён");
            Bind("ui.bluefolders", checkBox_bluefolders, back_ui_5, "синие папки были применены");
            Bind("ui.contextmenu", checkBox_contex, back_ui_6, "contex11 был применён");
            Bind("ui.win11header", checkBox_shapka, back_ui_7, "shapka11 был применён");
            Bind("ui.wotbocontext", checkBox_wotboincontex, back_ui_8, "wotbo in contex был применён");
            Bind("ui.explorerrestart", checkBox_explorer, back_ui_9, "restart explorer был применён");
            Bind("ui.ffmpeg", checkBox_ffmpeg, back_ui_10, "FFmpeg был применён",
                beforeApply: () => ShowNotification("FFMPEG", "Ожидайте скачивания FFMPEG. Программа не зависла!", ToolTipIcon.Info));
            Bind("ui.mica", checkBox_mica, back_ui_11, "ExplorerBlurMica был применён");
            Bind("ui.animations", checkBox_animation, back_animations, "анимации выключены",
                onDetected: () => animation = false,
                afterApply: () => animation = false,
                afterRollback: () => animation = true);

            Bind("extra.onedrive", checkBox_onedrive, null, "OneDrive был удалён",
                showRollbackOnApply: false, showRollbackOnDetect: false);
            Bind("extra.compactos", checkBox_compactos, null,
                disableOnApply: false, showRollbackOnApply: false);
            Bind("extra.winsxs", checkBox_WinSxS, null,
                disableOnApply: false, showRollbackOnApply: false);
            Bind("extra.tempclean", checkBox_temp, null,
                disableOnApply: false, showRollbackOnApply: false);
            Bind("extra.updateclean", checkBox_updclean, null,
                disableOnApply: false, showRollbackOnApply: false);
            Bind("extra.iconcache", checkBox_picture_cache, back_dop_1, "Max Cached Icons был применён");
            Bind("extra.mobiletraffic", checkBox_mobile_traffic, back_dop_2, "DefaultTTL был применён");
            Bind("extra.scoobe", checkBox_nastroyka, back_dop_3, "ScoobeSystemSettingEnabled был применён");
            Bind("extra.stickykeys", checkBox_zalipanie, back_dop_4, "StickyKeys был применён");
            Bind("extra.dwmpriority", checkBox_dwm, back_dop_6, "dwm.exe priority был применён");
            Bind("extra.edge", checkBox_edge, back_dop_edge, "Edge был применён",
                uncheckOnDetect: true);
            Bind("extra.movetemp", checkBox_move_temp, back_dop_movetemp);

            Bind("pro.homegallery", checkBox_pro_1, back_pro_1, "pro_1 был применён",
                showRollbackOnApply: false, showRollbackOnDetect: false);
            Bind("pro.windowsupdate", checkBox_pro_3, back_pro_3);
            Bind("pro.csrss", checkBox_CSRSS, back_dop_7, "csrss.exe priority был применён");
            Bind("pro.bufferbloat", checkBox_pro_12, back_pro_12, "bufferbloat был применён",
                confirmApply: () => MessageBox.Show(
                    "Bufferbloat - явление, когда чрезмерная буферизация вызывает увеличение времени прохождения пакетов (Ping) и разброса задержки пакетов (Packetloss)\n\n" +
                    "Рекомендуется использовать только если у вас нестабильное соединение!\n(Например мобильный интернет и т.д)\n" +
                    "!Твик может незначительно снизить скорость интернета!\n" +
                    "В случае если у вас стало хуже, или же вам не нужен этот твик, просто откатите его.\n" +
                    "Применить?",
                    "Windows optimization tool by oixro (WOTBO)", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes);
            Bind("pro.unlocker", checkBox_pro_13, back_pro_13, "unclockewr был применён");
            Bind("pro.alwaysontop", checkBox_pro_14, back_pro_14, "AlwaysOnTop был применён",
                inRollback: () => AppServices.Processes.StartCaptured("taskkill /f /im AlwaysOnTop.exe"));
        }
        #endregion
    }
}
