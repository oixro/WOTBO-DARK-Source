using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Defender
{
    /// <summary>Проверка реального состояния Defender (а не флага в сторе).</summary>
    internal sealed class DefenderVerification
    {
        public Dictionary<string, string> Values { get; set; }

        public string WinDefend { get; set; }

        public string WdFilter { get; set; }

        public string Processes { get; set; }

        public string Antivirus { get; set; }

        public string Realtime { get; set; }

        public bool DefenderOff { get; set; }

        public bool DefenderOn { get; set; }

        public bool NeedsReboot { get; set; }

        /// <summary>Откат действительно вернул защиту, а не только службу.</summary>
        public bool Restored
        {
            get
            {
                return DefenderOn
                    && string.Equals(Antivirus, "1", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string Details { get; set; }
    }

    internal static class DefenderStatus
    {
        private const string LogCategory = "Defender";

        private static readonly string[] GuardedServices =
        {
            "WinDefend",
            "WdNisSvc",
            "WdFilter",
            "WdBoot",
            "WdNisDrv",
            "Sense",
            "SgrmBroker",
            "SgrmAgent",
            "MDCoreSvc",
            "MpDefenderCoreService",
            "MsSecFlt",
            "MsSecCore",
            "MsSecWfp",
            "SecurityHealthService",
            "MpsSvc",
        };

        /// <summary>Быстрая проверка для Detect: службы и драйверы выключены.</summary>
        public static bool IsFullyDisabled()
        {
            if (IsServiceRunning("WinDefend"))
            {
                return false;
            }

            int? start = ReadStart("WinDefend");
            if (start.HasValue && start.Value != 4)
            {
                return false;
            }

            int? filter = ReadStart("WdFilter");
            if (filter.HasValue && filter.Value != 4)
            {
                return false;
            }

            return start.HasValue || filter.HasValue || IsDefenderAbsent();
        }

        /// <summary>Служба WinDefend отсутствует или её файлы переименованы.</summary>
        public static bool IsDefenderAbsent()
        {
            if (!ReadStart("WinDefend").HasValue)
            {
                return true;
            }

            string platform = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                @"Microsoft\Windows Defender");
            string backup = platform + ".wotbo_bak";
            return !System.IO.Directory.Exists(platform) && System.IO.Directory.Exists(backup);
        }

        /// <summary>Признак текущей загрузки в безопасном режиме.</summary>
        public static bool IsSafeModeBoot()
        {
            try
            {
                using (RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\SafeBoot\Option"))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsTamperProtected()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    @"root\Microsoft\Windows\Defender",
                    "SELECT IsTamperProtected FROM MSFT_MpComputerStatus"))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        object value = item["IsTamperProtected"];
                        if (value == null)
                        {
                            return false;
                        }

                        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool IsBitLockerOn()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    @"root\CIMV2\Security\MicrosoftVolumeEncryption",
                    "SELECT DriveLetter, ProtectionStatus FROM Win32_EncryptableVolume"))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        object status = item["ProtectionStatus"];
                        if (status != null && Convert.ToInt32(status, CultureInfo.InvariantCulture) == 1)
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

        /// <summary>Текущее состояние WinDefend/WdFilter по данным SCM.</summary>
        public static string GetServiceState(string name)
        {
            try
            {
                using (var controller = new ServiceController(name))
                {
                    return controller.Status.ToString();
                }
            }
            catch
            {
                return "Absent";
            }
        }

        public static bool IsServiceRunning(string name)
        {
            return string.Equals(
                GetServiceState(name), "Running", StringComparison.OrdinalIgnoreCase);
        }

        public static int? ReadStart(string service)
        {
            try
            {
                using (RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\" + service))
                {
                    if (key == null)
                    {
                        return null;
                    }

                    object value = key.GetValue("Start");
                    if (value == null)
                    {
                        return null;
                    }

                    return Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Полная проверка через движок (<c>verify.ini</c>).</summary>
        public static DefenderVerification Verify(
            IProcessService processes, IAppLogger log, int timeoutMs)
        {
            DefenderRunResult run = DefenderEngine.Run(
                processes, log, DefenderEngine.PhaseVerify, false, timeoutMs);
            if (run == null && !System.IO.File.Exists(DefenderPaths.VerifyFile))
            {
                return null;
            }

            Dictionary<string, string> values = DefenderEngine.ReadVerify();
            var verification = new DefenderVerification { Values = values };

            string value;
            values.TryGetValue("windefend", out value);
            verification.WinDefend = value ?? "Absent";

            values.TryGetValue("wdfilter", out value);
            verification.WdFilter = value ?? "Absent";

            values.TryGetValue("processes", out value);
            verification.Processes = value ?? string.Empty;

            string processesText = verification.Processes ?? string.Empty;
            bool engineRunning = processesText.IndexOf("MsMpEng", StringComparison.OrdinalIgnoreCase) >= 0;

            verification.DefenderOn =
                string.Equals(verification.WinDefend, "Running", StringComparison.OrdinalIgnoreCase)
                || engineRunning;

            verification.DefenderOff = !verification.DefenderOn;

            verification.NeedsReboot = verification.DefenderOff
                && string.Equals(verification.WdFilter, "Running", StringComparison.OrdinalIgnoreCase);

            string antivirus;
            values.TryGetValue("antivirus", out antivirus);
            string realtime;
            values.TryGetValue("realtime", out realtime);

            verification.Antivirus = antivirus;
            verification.Realtime = realtime;

            verification.Details = string.Format(
                CultureInfo.InvariantCulture,
                "WinDefend={0}; WdFilter={1}; процессы={2}; AntivirusEnabled={3}; RealTime={4}",
                verification.WinDefend,
                verification.WdFilter,
                string.IsNullOrEmpty(processesText) ? "нет" : processesText,
                antivirus ?? "н/д",
                realtime ?? "н/д");

            return verification;
        }

        /// <summary>Список служб/драйверов, которые трогает твик.</summary>
        public static IEnumerable<string> TargetServices
        {
            get { return GuardedServices; }
        }
    }
}
