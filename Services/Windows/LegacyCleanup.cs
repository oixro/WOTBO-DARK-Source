using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using WOTBO.Services.Registry;
using WOTBO.Services.Tweaks;

namespace WOTBO.Services.Windows
{
    /// <summary>
    /// Одноразовая уборка твиков, которые убрали из WOTBO. Нужна, чтобы у
    /// пользователей, применявших их раньше, не оставался мусор в системе.
    /// </summary>
    public static class LegacyCleanup
    {
        private const string AlwaysOnTopId = "pro.alwaysontop";
        private const string NdisId = "extra.ndis";
        private const string NdisParameters = @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters";
        private const string AlwaysOnTopExe = @"C:\Windows\AlwaysOnTop.exe";
        private const uint MoveFileDelayUntilReboot = 0x00000004;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existing, string replacement, uint flags);

        public static void RemoveAlwaysOnTop(TweakContext context)
        {
            if (context == null)
            {
                return;
            }

            bool applied = context.Store.IsApplied(AlwaysOnTopId) || File.Exists(AlwaysOnTopExe);
            if (!applied)
            {
                return;
            }

            foreach (Process process in Process.GetProcessesByName("AlwaysOnTop"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            context.Registry.DeleteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.RunKey,
                "AlwaysOnTop");

            bool deleted = false;
            if (File.Exists(AlwaysOnTopExe))
            {
                try
                {
                    File.Delete(AlwaysOnTopExe);
                    deleted = !File.Exists(AlwaysOnTopExe);
                }
                catch (Exception exception)
                {
                    context.Log.Warn("Cleanup", "AlwaysOnTop.exe не удалён: " + exception.Message);
                }
            }
            else
            {
                deleted = true;
            }

            if (!deleted)
            {
                // Файл мог остаться запущенным: удалим при следующей перезагрузке.
                MoveFileEx(AlwaysOnTopExe, null, MoveFileDelayUntilReboot);
            }

            context.Store.Remove(AlwaysOnTopId);
            context.Log.Info("Cleanup", "твик pro.alwaysontop удалён из системы");
        }

        /// <summary>
        /// Твик «Отключить watchdog'и NDIS» убран. Если он был применён через
        /// WOTBO — восстанавливаем бэкап. Если значения остались без бэкапа —
        /// удаляем только те, что совпадают со значениями твика.
        /// </summary>
        public static void RemoveNdisWatchdogs(TweakContext context)
        {
            if (context == null)
            {
                return;
            }

            var values = new[]
            {
                new KeyValuePair<string, int>("DisableNDISWatchDog", 1),
                new KeyValuePair<string, int>("DisableNaps", 1),
                new KeyValuePair<string, int>("NoPauseOnSuspend", 1),
                new KeyValuePair<string, int>("DebugLoggingMode", 0),
                new KeyValuePair<string, int>("DisableWDIWatchdogForceBugcheck", 1),
                new KeyValuePair<string, int>("DisableReenumerateTimeoutBugcheck", 1),
                new KeyValuePair<string, int>("EnableNicAutoPowerSaverInSleepStudy", 0),
            };

            bool applied = context.Store.IsApplied(NdisId);
            if (applied)
            {
                context.Store.Restore(NdisId);
                context.Log.Info("Cleanup", "твик extra.ndis откачен по бэкапу");
                return;
            }

            bool removed = false;
            foreach (KeyValuePair<string, int> value in values)
            {
                object current = context.Registry.GetValue(
                    RegistryHive.LocalMachine, NdisParameters, value.Key);
                if (current == null)
                {
                    continue;
                }

                int number;
                if (!int.TryParse(Convert.ToString(current), out number) || number != value.Value)
                {
                    continue;
                }

                context.Registry.DeleteValue(
                    RegistryHive.LocalMachine, NdisParameters, value.Key);
                removed = true;
            }

            context.Store.Remove(NdisId);
            if (removed)
            {
                context.Log.Info("Cleanup", "удалены оставшиеся значения extra.ndis");
            }
        }
    }
}
