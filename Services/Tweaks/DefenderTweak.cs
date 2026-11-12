using System;
using System.IO;
using System.Management;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключение Defender через defendnot (WSC fake AV).
    /// Требует интернет: команда загружается с dnot.sh.
    /// </summary>
    public sealed class DefenderTweak : TweakBase
    {
        private const string LogCategory = "Tweak:main.defender";
        private const string AvName = "WOTBO.PRO";
        private const string InstallCommand =
            "& ([ScriptBlock]::Create((irm https://dnot.sh/))) --name \"WOTBO.PRO\" --firewall --silent";
        private const string DisableCommand =
            "& ([ScriptBlock]::Create((irm https://dnot.sh/))) --disable";

        public override string Id { get { return "main.defender"; } }
        public override string Category { get { return "Main"; } }

        public override TweakState Detect(TweakContext context)
        {
            return IsDefenderDisabled() ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            if (!context.Network.IsAvailable())
            {
                context.Log.Warn(LogCategory, "нет интернета — defendnot не запущен");
                return;
            }

            string output = RunScript(context, InstallCommand, "defendnot-apply.ps1");
            context.Log.Info(LogCategory, "defendnot: " + output.Trim());

            if (!IsDefenderDisabled())
            {
                context.Log.Warn(LogCategory, "defendnot завершился, но Defender не подтверждён как отключённый");
            }
        }

        public override void Rollback(TweakContext context)
        {
            if (!context.Network.IsAvailable())
            {
                context.Log.Warn(LogCategory, "нет интернета — defendnot --disable не запущен");
                return;
            }

            string output = RunScript(context, DisableCommand, "defendnot-disable.ps1");
            context.Log.Info(LogCategory, "defendnot --disable: " + output.Trim());

            if (!IsDefenderDisabled())
            {
                context.Log.Info(LogCategory, "Defender снова активен");
            }
        }

        public static bool IsDefenderDisabled()
        {
            if (!IsFakeAvRegistered())
            {
                return false;
            }

            // Если WMI Defender недоступен, fake AV уже зарегистрирован — считаем отключённым.
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    @"root\Microsoft\Windows\Defender",
                    "SELECT AntivirusEnabled, RealTimeProtectionEnabled FROM MSFT_MpComputerStatus"))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        bool antivirus = Convert.ToBoolean(item["AntivirusEnabled"]);
                        bool realtime = Convert.ToBoolean(item["RealTimeProtectionEnabled"]);
                        return !antivirus && !realtime;
                    }
                }
            }
            catch
            {
                return true;
            }

            return true;
        }

        private static bool IsFakeAvRegistered()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    @"root\SecurityCenter2",
                    "SELECT displayName FROM AntiVirusProduct"))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        string name = Convert.ToString(item["displayName"]);
                        if (name.IndexOf(AvName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private static string RunScript(TweakContext context, string command, string fileName)
        {
            string script = Path.Combine(context.Workspace.Root, fileName);
            File.WriteAllText(script, command);

            return context.Processes.ReadOutputAsync(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"").GetAwaiter().GetResult();
        }
    }
}
