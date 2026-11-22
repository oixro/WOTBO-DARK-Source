using System;
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
    }
}
