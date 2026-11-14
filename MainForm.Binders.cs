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
using WOTBO.Services.Defender;
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
            Func<bool> confirmRollback = null,
            Func<Action, bool> wrap = null,
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
                confirmRollback,
                wrap,
                disableOnApply,
                showRollbackOnApply,
                showRollbackOnDetect,
                uncheckOnDetect);

            tweakBinders[id] = binder;
            TweakHoverInfo.Register(checkBox.Name, id);
            if (rollback != null)
            {
                TweakHoverInfo.Register(rollback.Name, id);
                rollback.Click += (sender, args) => binder.Rollback();
            }

            return binder;
        }

        private TweakUiBinder Binder(string id)
        {
            return tweakBinders[id];
        }

        private static bool DefenderProgress(Action action)
        {
            using (var form = new DefenderProgressForm("Отключение Windows Defender"))
            {
                form.Run(action);
                form.ShowDialog();
            }

            return true;
        }

        private bool ConfirmDefenderApply()
        {
            DefenderProtectionState protection = DefenderStatus.GetProtection();

            if (protection.Available && (protection.TamperProtection || protection.RealTimeProtection))
            {
                string what = protection.TamperProtection
                    ? "защиту от подделки (Tamper Protection)"
                    : "защиту в реальном времени (Real-time protection)";

                MessageBox.Show(
                    "Перед отключением Windows Defender нужно вручную выключить:\n" +
                    "• защиту от подделки (Tamper Protection);\n" +
                    "• защиту в реальном времени (Real-time protection).\n\n" +
                    "Сейчас включена " + what + ".\n\n" +
                    "Откройте «Безопасность Windows» → «Защита от вирусов и угроз» →\n" +
                    "«Управление параметрами», выключите обе защиты и повторите.",
                    "Сначала отключите защиты",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                try
                {
                    Process.Start(new ProcessStartInfo("windowsdefender://threatsettings/")
                    {
                        UseShellExecute = true,
                    });
                }
                catch
                {
                }

                return false;
            }

            DialogResult answer = MessageBox.Show(
                "Windows Defender будет отключён полностью:\n" +
                "• защита в реальном времени, облачная защита и проверка загрузки;\n" +
                "• службы и драйверы защитника, его задачи и политики;\n" +
                "• SmartScreen, Smart App Control и брандмауэр Windows;\n" +
                "• раздел «Безопасность Windows».\n\n" +
                "Что нужно знать:\n" +
                "• античиты (Vanguard, EAC, BattlEye) могут отказаться запускаться;\n" +
                "• Windows Update может пытаться вернуть компоненты защитника;\n" +
                "• файлы защитника переименовываются, а не удаляются, поэтому\n" +
                "  откат возможен стрелкой отката;\n" +
                "• на Windows 11 WOTBO перезагрузит ПК в безопасный режим\n" +
                "  (две перезагрузки, всё автоматически).\n\n" +
                "Продолжить?",
                "Отключение Windows Defender",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (answer != DialogResult.Yes)
            {
                return false;
            }

            if (DefenderStatus.IsBitLockerOn())
            {
                DialogResult bitlocker = MessageBox.Show(
                    "На системном диске включён BitLocker.\n" +
                    "Перезагрузка в безопасный режим может запросить ключ восстановления.\n\n" +
                    "Продолжить?",
                    "BitLocker",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (bitlocker != DialogResult.Yes)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ConfirmDefenderRollback()
        {
            return MessageBox.Show(
                "Восстановить Windows Defender?\n\n" +
                "Файлы защитника будут возвращены из *.wotbo_bak, службы, драйверы,\n" +
                "задачи, брандмауэр и SmartScreen — в прежнее состояние.\n\n" +
                "Продолжить?",
                "Восстановление Windows Defender",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        private void HandleDefenderApplied()
        {
            switch (DefenderTweak.LastOutcome)
            {
                case DefenderTweak.DefenderOutcome.Applied:
                    ShowNotification(
                        "Защитник",
                        "Windows Defender отключён. Перезагрузите ПК, чтобы выгрузить драйверы.",
                        ToolTipIcon.Info);
                    break;

                case DefenderTweak.DefenderOutcome.AppliedNeedsReboot:
                    ShowNotification(
                        "Защитник",
                        "Windows Defender отключён. Нужна перезагрузка для выгрузки драйверов.",
                        ToolTipIcon.Warning);
                    break;

                case DefenderTweak.DefenderOutcome.SafeModeApply:
                    OfferDefenderReboot(
                        "Нужна перезагрузка в безопасный режим.\n\n" +
                        "WOTBO применит изменения автоматически, вернёт обычный режим\n" +
                        "и после загрузки покажет результат. Понадобятся две перезагрузки.",
                        false);
                    break;

                default:
                    ShowDefenderFailure("Defender не отключён");
                    break;
            }
        }

        private void HandleDefenderRollback()
        {
            switch (DefenderTweak.LastOutcome)
            {
                case DefenderTweak.DefenderOutcome.Restored:
                    ShowNotification("Защитник", "Windows Defender восстановлен.", ToolTipIcon.Info);
                    break;

                case DefenderTweak.DefenderOutcome.SafeModeRestore:
                    OfferDefenderReboot(
                        "Откат нужно завершить в безопасном режиме.\n\n" +
                        "WOTBO вернёт файлы и службы автоматически и перезагрузит ПК обратно.\n" +
                        "Понадобятся две перезагрузки.",
                        true);
                    break;

                default:
                    ShowDefenderFailure("Откат Defender не завершён");
                    break;
            }
        }

        private void OfferDefenderReboot(string text, bool forRollback)
        {
            DialogResult answer = MessageBox.Show(
                text + "\n\nПерезагрузить сейчас?",
                "WOTBO",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (answer != DialogResult.Yes)
            {
                DefenderTweak.AbortPrepared(AppServices.TweakContext);

                if (forRollback)
                {
                    checkBox_disabledefender.Enabled = false;
                    back_main_12.Visible = true;
                }
                else
                {
                    checkBox_disabledefender.Enabled = true;
                    back_main_12.Visible = false;
                }

                ShowNotification("Защитник", "Изменения отменены.", ToolTipIcon.Info);
                return;
            }

            DefenderTweak.Reboot(AppServices.TweakContext);
            ShowNotification(
                "Защитник",
                "Перезагрузка…",
                ToolTipIcon.Warning);
        }

        private void ShowDefenderFailure(string headline)
        {
            string details = DefenderTweak.LastDetails;
            if (string.IsNullOrWhiteSpace(details))
            {
                details = DefenderEngine.ReadLastLog(10);
            }

            if (string.IsNullOrWhiteSpace(details))
            {
                details = "(подробностей нет)";
            }

            if (details.Length > 1200)
            {
                details = details.Substring(details.Length - 1200);
            }

            MessageBox.Show(
                headline + "\n\n" +
                details.Trim() + "\n\n" +
                "Лог: " + DefenderPaths.LogDirectory + "\n" +
                "Журнал: " + DefenderPaths.JournalFile,
                "WOTBO",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private readonly System.Collections.Generic.Dictionary<Control, bool> _defenderLockEnabled =
            new System.Collections.Generic.Dictionary<Control, bool>();

        private readonly System.Collections.Generic.Dictionary<Control, bool> _defenderLockVisible =
            new System.Collections.Generic.Dictionary<Control, bool>();

        /// <summary>
        /// Отключение Defender меняет службы и брандмауэр, поэтому вместе с ним
        /// нельзя применять другие твики: пока чекбокс отмечен, остальные
        /// блокируются, а стрелки отката скрываются.
        /// </summary>
        private void ApplyDefenderExclusiveMode(bool locked)
        {
            if (locked)
            {
                _defenderLockEnabled.Clear();
                _defenderLockVisible.Clear();

                foreach (CheckBox box in DescendantControls<CheckBox>(this))
                {
                    if (box == checkBox_disabledefender)
                    {
                        continue;
                    }

                    _defenderLockEnabled[box] = box.Enabled;
                    box.Enabled = false;
                }

                foreach (PictureBox back in DescendantControls<PictureBox>(this))
                {
                    if (back.Name == null || !back.Name.StartsWith("back_", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    _defenderLockVisible[back] = back.Visible;
                    back.Visible = false;
                }

                return;
            }

            foreach (var pair in _defenderLockEnabled)
            {
                pair.Key.Enabled = pair.Value;
            }

            foreach (var pair in _defenderLockVisible)
            {
                pair.Key.Visible = pair.Value;
            }

            _defenderLockEnabled.Clear();
            _defenderLockVisible.Clear();
        }

        private static System.Collections.Generic.IEnumerable<T> DescendantControls<T>(Control root)
            where T : Control
        {
            foreach (Control child in root.Controls)
            {
                T typed = child as T;
                if (typed != null)
                {
                    yield return typed;
                }

                foreach (T nested in DescendantControls<T>(child))
                {
                    yield return nested;
                }
            }
        }

        private void SetupTweakBinders()
        {
            checkBox_disabledefender.CheckedChanged +=
                (sender, args) => ApplyDefenderExclusiveMode(checkBox_disabledefender.Checked);

            Bind("main.defender", checkBox_disabledefender, back_main_12,
                "defenderdisabled был применён",
                onDetected: () => { },
                confirmApply: ConfirmDefenderApply,
                confirmRollback: ConfirmDefenderRollback,
                wrap: DefenderProgress,
                afterApply: HandleDefenderApplied,
                afterRollback: HandleDefenderRollback);

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
                inApply: () => new Win32PriorityForm().ShowDialog());

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
            Bind("pro.windowsupdate", checkBox_pro_3, back_pro_3, "WindowsUpdate\\AU был применён",
                uncheckOnDetect: true);
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

            TweakHoverInfo.Register(checkBox_ansel.Name, "gpu.ansel");
            TweakHoverInfo.Register(back_gpu_4.Name, "gpu.ansel");
        }
        #endregion
    }
}
