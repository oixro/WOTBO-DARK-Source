using System;
using System.Diagnostics;
using System.IO;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;
using WOTBO.Services.State;

namespace WOTBO.Services.Windows
{
    /// <summary>Одноразовая уборка legacy-артефактов при старте.</summary>
    public interface ILegacyCleanupService
    {
        void Cleanup();
    }

    public sealed class LegacyCleanupService : ILegacyCleanupService
    {
        private const string Category = "System";
        private const string BackgroundMonitoring =
            @"C:\Windows\System32\drivers\BackgroundMonitoringServices.exe";

        private readonly IProcessService _processes;
        private readonly TweakStateStore _store;
        private readonly IAppLogger _log;

        public LegacyCleanupService(IProcessService processes, TweakStateStore store, IAppLogger log)
        {
            _processes = processes ?? throw new ArgumentNullException(nameof(processes));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Cleanup()
        {
            RemoveBackgroundMonitoring();
            ResetBcdedit();
        }

        private void RemoveBackgroundMonitoring()
        {
            if (!File.Exists(BackgroundMonitoring)) return;

            Process[] processes = Process.GetProcessesByName("BackgroundMonitoringServices");
            if (processes.Length > 0)
            {
                _log.Info(Category, $"BackgroundMonitoringServices был найден - {processes.Length}");
                _processes.StartCaptured("taskkill /f /im BackgroundMonitoringServices.exe");
            }

            File.Delete(BackgroundMonitoring);
        }

        private void ResetBcdedit()
        {
            if (!_store.IsApplied("main.bcdedit")) return;

            _log.Warn("Tweak:main.bcdedit", "bcdedit был применён, откатываем");
            _processes.StartCaptured("bcdedit /deletevalue tscsyncpolicy");
            _processes.StartCaptured("bcdedit /deletevalue bootux");
            _processes.StartCaptured("bcdedit /set bootmenupolicy standard");
            _processes.StartCaptured("bcdedit /set hypervisorlaunchtype Auto");
            _processes.StartCaptured("bcdedit /deletevalue tpmbootentropy");
            _processes.StartCaptured("bcdedit /deletevalue quietboot");
            _processes.StartCaptured("bcdedit /set nx optin");
            _processes.StartCaptured("bcdedit /set allowedinmemorysettings 0x17000077");
            _processes.StartCaptured("bcdedit /set isolatedcontext Yes");
            _processes.StartCaptured("bcdedit /deletevalue vsmlaunchtype");
            _processes.StartCaptured("bcdedit /deletevalue vm");
            _processes.StartCaptured("reg delete \"HKLM\\Software\\Policies\\Microsoft\\FVE\" /v \"DisableExternalDMAUnderLock\" /f");
            _processes.StartCaptured("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"EnableVirtualizationBasedSecurity\" /f");
            _processes.StartCaptured("reg delete \"HKLM\\Software\\Policies\\Microsoft\\Windows\\DeviceGuard\" /v \"HVCIMATRequired\" /f");
            _processes.StartCaptured("bcdedit /deletevalue firstmegabytepolicy");
            _processes.StartCaptured("bcdedit /deletevalue avoidlowmemory");
            _processes.StartCaptured("bcdedit /deletevalue nolowmem");
            _processes.StartCaptured("bcdedit /deletevalue configaccesspolicy");
            _processes.StartCaptured("bcdedit /deletevalue x2apicpolicy");
            _processes.StartCaptured("bcdedit /deletevalue usephysicaldestination");
            _processes.StartCaptured("bcdedit /deletevalue usefirmwarepcisettings");
            _processes.StartCaptured("bcdedit /deletevalue uselegacyapicmode");
            _store.Remove("main.bcdedit");
        }
    }
}
