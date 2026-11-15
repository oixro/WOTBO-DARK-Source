using System;
using System.IO;
using System.Linq;
using System.Management;
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
            // Быстрый путь: WMI-провайдер отвечает за миллисекунды и обычно
            // не врёт, когда твик уже применён.
            TweakState wmi = DetectViaWmi(context);
            if (wmi == TweakState.Applied)
            {
                return TweakState.Applied;
            }

            // Если WMI говорит «нет», проверяем через PowerShell: на части
            // сборок Win10 провайдер отдаёт неполные значения, из-за чего
            // применённый твик выглядел неприменённым.
            bool? applicationPreLaunch;
            bool? memoryCompression;
            bool? pageCombining;

            if (TryReadSettings(context, out applicationPreLaunch, out memoryCompression, out pageCombining))
            {
                context.Log.Debug(
                    "Tweak:main.mmagent",
                    "ApplicationPreLaunch=" + applicationPreLaunch
                    + ", MemoryCompression=" + memoryCompression
                    + ", PageCombining=" + pageCombining);

                return applicationPreLaunch == true && pageCombining == false
                    ? TweakState.Applied
                    : TweakState.NotApplied;
            }

            return wmi;
        }

        private static TweakState DetectViaWmi(TweakContext context)
        {
            try
            {
                var scope = new ManagementScope(@"root\Microsoft\Windows\PS_MMAgent");
                scope.Connect();
                using (var cls = new ManagementClass(scope, new ManagementPath("PS_MMAgent"), null))
                {
                    ManagementBaseObject inParams = cls.GetMethodParameters("Get");
                    using (ManagementBaseObject outParams = cls.InvokeMethod("Get", inParams, null))
                    {
                        var output = outParams["cmdletOutput"] as ManagementBaseObject;
                        if (output != null)
                        {
                            bool apl = Convert.ToBoolean(output["ApplicationPreLaunch"]);
                            bool pc = Convert.ToBoolean(output["PageCombining"]);

                            return apl && !pc ? TweakState.Applied : TweakState.NotApplied;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                context.Log.Debug("Tweak:main.mmagent", "detect: " + exception.Message);
            }

            return context.Store.IsApplied("main.mmagent") ? TweakState.Applied : TweakState.NotApplied;
        }

        /// <summary>
        /// Читает настройки mmagent через PowerShell в JSON: WMI-провайдер
        /// PS_MMAgent на части систем отвечает неполно, а JSON одинаков
        /// на всех языках (имена свойств не локализуются).
        /// </summary>
        private static bool TryReadSettings(
            TweakContext context,
            out bool? applicationPreLaunch,
            out bool? memoryCompression,
            out bool? pageCombining)
        {
            applicationPreLaunch = null;
            memoryCompression = null;
            pageCombining = null;

            try
            {
                string json = context.Processes
                    .ReadOutputAsync(
                        "powershell.exe",
                        "-NoProfile -Command \"Get-MMAgent | Select-Object ApplicationPreLaunch,MemoryCompression,PageCombining | ConvertTo-Json -Compress\"",
                        15000,
                        System.Text.Encoding.UTF8)
                    .GetAwaiter()
                    .GetResult();

                applicationPreLaunch = ReadBool(json, "ApplicationPreLaunch");
                memoryCompression = ReadBool(json, "MemoryCompression");
                pageCombining = ReadBool(json, "PageCombining");

                return applicationPreLaunch.HasValue;
            }
            catch (Exception exception)
            {
                context.Log.Debug("Tweak:main.mmagent", "Get-MMAgent: " + exception.Message);
                return false;
            }
        }

        private static bool? ReadBool(string json, string name)
        {
            if (string.IsNullOrEmpty(json)) return null;

            string marker = "\"" + name + "\":";
            int index = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            string tail = json.Substring(index + marker.Length).TrimStart();
            if (tail.StartsWith("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (tail.StartsWith("false", StringComparison.OrdinalIgnoreCase)) return false;

            return null;
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
                ResourceDownload.Ensure(
                    context,
                    "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/smartctl.exe",
                    context.Workspace.Smartctl,
                    "smartctl.exe");
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

            // Команды ждём, а не отправляем «в никуда»: иначе Detect сразу
            // после применения видел старые значения и твик выглядел неприменённым.
            RunPowerShell(context, "Enable-MMAgent -ApplicationPreLaunch");
            RunPowerShell(context, win10
                ? "Disable-MMAgent -MemoryCompression"
                : "Enable-MMAgent -MemoryCompression");
            RunPowerShell(context, "Disable-MMAgent -PageCombining");
            RunPowerShell(context, "Set-MMAgent -MaxOperationAPIFiles " + mmMemory);
        }

        private static void RunPowerShell(TweakContext context, string command)
        {
            string output = context.Processes
                .ReadOutputAsync(
                    "powershell.exe",
                    "-NoProfile -Command \"" + command + "\"",
                    20000,
                    System.Text.Encoding.UTF8)
                .GetAwaiter()
                .GetResult();

            if (!string.IsNullOrWhiteSpace(output))
            {
                context.Log.Debug("Tweak:main.mmagent", command + ": " + output.Trim());
            }
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
