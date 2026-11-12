using System;
using System.IO;
using System.Linq;
using WOTBO.Properties;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Настройка mmagent и prefetcher под объём RAM и тип диска.
    /// </summary>
    public sealed class MmAgentTweak : TweakBase
    {
        public override string Id { get { return "main.mmagent"; } }
        public override string Category { get { return "Main"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string root = context.Workspace.Root;
            string hdd = Path.Combine(root, "prefetcher hdd.reg");
            string ssd = Path.Combine(root, "prefetcher ssd.reg");
            File.WriteAllText(hdd, Resources.prefetcher_hdd);
            File.WriteAllText(ssd, Resources.prefetcher_ssd);

            if (!File.Exists(context.Workspace.Smartctl))
            {
                context.Network.DownloadFile(
                    "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/smartctl.exe",
                    context.Workspace.Smartctl);
            }

            int ramGB = ReadRamGb(context);
            int mmMemory = ramGB * 32;
            if (mmMemory <= 128)
            {
                mmMemory = 128;
            }
            else if (mmMemory >= 1024)
            {
                mmMemory = 1024;
            }

            string systemDisk = Environment.GetEnvironmentVariable("windir").Replace(@"\Windows", string.Empty);
            string diskInfo = context.Processes
                .ReadOutputAsync(context.Workspace.Smartctl, "-i " + systemDisk)
                .GetAwaiter()
                .GetResult()
                .ToLowerInvariant();

            bool isSsd = diskInfo.Contains("solid state device")
                || diskInfo.Contains("ssd")
                || diskInfo.Contains("nvme");

            string prefetcher = isSsd ? ssd : hdd;
            context.BackupRegFile(prefetcher);
            context.Processes.StartCaptured("regedit.exe /s \"" + prefetcher + "\"");

            bool win10 = context.WindowsVersion.Detect().IsWindows10;
            context.Processes.StartPowerShell("enable-mmagent -ApplicationPreLaunch");
            if (win10)
            {
                context.Processes.StartPowerShell("disable-mmagent -MC");
            }
            else
            {
                context.Processes.StartPowerShell("enable-mmagent -MC");
            }

            context.Processes.StartPowerShell("disable-mmagent -PC");
            context.Processes.StartPowerShell("set-mmagent -moaf " + mmMemory);
        }

        public override void Rollback(TweakContext context)
        {
            // Prefetcher и mmagent не имеют автоматического отката.
        }

        private static int ReadRamGb(TweakContext context)
        {
            string output = context.Processes
                .ReadOutputAsync(
                    "powershell.exe",
                    "/command (Get-WmiObject Win32_PhysicalMemory).capacity | Measure-Object -Sum | Foreach {[int]($_.Sum/1GB)}")
                .GetAwaiter()
                .GetResult();

            foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Reverse())
            {
                int value;
                if (int.TryParse(line.Trim(), out value))
                {
                    return value;
                }
            }

            context.Log.Warn("Tweak:main.mmagent", "не удалось определить объём RAM, используется 4 ГБ");
            return 4;
        }
    }
}
