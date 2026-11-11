using System;
using System.IO;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Hardware;
using WOTBO.Services.Processes;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Hardware
{
    public interface IGpuService
    {
        string GetName();

        bool IsNvidia(string name);

        bool IsHdcpDisabled();

        bool IsNvidiaTweaksApplied();

        /// <summary><c>true</c> — Ansel выключен, <c>false</c> — включён, <c>null</c> — утилита не найдена.</summary>
        System.Threading.Tasks.Task<bool?> IsAnselDisabledAsync();

        /// <summary>Включить/выключить Ansel. <c>false</c>, если утилита не найдена.</summary>
        bool SetAnsel(bool enabled);
    }

    public sealed class GpuService : IGpuService
    {
        private const string Category = "GPU";
        private const string HdcpPath =
            @"System\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000";
        private const string NvidiaTweaksPath =
            @"SYSTEM\CurrentControlSet\Services\nvlddmkm\Global\NVTweak";

        private readonly IWmiSystemInfoService _wmi;
        private readonly IRegistryService _registry;
        private readonly IProcessService _process;
        private readonly IAppLogger _log;

        public GpuService(
            IWmiSystemInfoService wmi,
            IRegistryService registry,
            IProcessService process,
            IAppLogger log)
        {
            _wmi = wmi ?? throw new ArgumentNullException(nameof(wmi));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public string GetName()
        {
            return _wmi.GetString("Win32_VideoController", "Name");
        }

        public bool IsNvidia(string name)
        {
            return !string.IsNullOrEmpty(name)
                && name.IndexOf("NVIDIA", StringComparison.Ordinal) >= 0;
        }

        public bool IsHdcpDisabled()
        {
            return _registry.ValueExists(RegistryHive.LocalMachine, HdcpPath, "RMHdcpKeyglobZero");
        }

        public bool IsNvidiaTweaksApplied()
        {
            return _registry.ValueExists(RegistryHive.LocalMachine, NvidiaTweaksPath, "DisplayPowerSaving");
        }

        public async System.Threading.Tasks.Task<bool?> IsAnselDisabledAsync()
        {
            string tool = FindAnselTool();
            if (string.IsNullOrEmpty(tool))
            {
                return null;
            }

            string state = (await _process.ReadOutputAsync(tool, string.Empty)).Trim();
            return state == "0";
        }

        public bool SetAnsel(bool enabled)
        {
            string tool = FindAnselTool();
            if (string.IsNullOrEmpty(tool))
            {
                return false;
            }

            // Сохраняем прежнюю fire-and-forget семантику hcmd.
            _process.StartCaptured($"\"{tool}\" {(enabled ? "on" : "off")}");
            return true;
        }

        private string FindAnselTool()
        {
            try
            {
                string[] files = Directory.GetFiles(
                    @"C:\Windows\System32\DriverStore\",
                    "NvCameraEnable.exe",
                    SearchOption.AllDirectories);

                if (files.Length > 0)
                {
                    return files[0];
                }
            }
            catch (Exception ex)
            {
                _log.Debug(Category, "поиск NvCameraEnable.exe: " + ex.Message);
            }

            return null;
        }
    }
}
