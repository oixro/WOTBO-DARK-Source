using System;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключает виртуализацию безопасности (VBS/HVCI) и гипервизор.
    ///
    /// VBS резервирует 200–500 МБ и заметно бьёт по производительности,
    /// но это защита уровня ядра — твик предупреждает об этом.
    /// Полный эффект — после перезагрузки.
    /// </summary>
    public sealed class VbsTweak : TweakBase
    {
        private const string DeviceGuard = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
        private const string Hvci = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";

        public override string Id
        {
            get { return "main.vbs"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object vbs = context.Registry.GetValue(
                RegistryHive.LocalMachine, DeviceGuard, "EnableVirtualizationBasedSecurity");
            object hvci = context.Registry.GetValue(RegistryHive.LocalMachine, Hvci, "Enabled");

            bool off = vbs != null && Convert.ToInt32(vbs) == 0
                       && hvci != null && Convert.ToInt32(hvci) == 0;

            return off ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.LocalMachine, DeviceGuard);
            context.BackupRegistryKey(RegistryHive.LocalMachine, Hvci);

            context.Registry.WriteValue(
                RegistryHive.LocalMachine, DeviceGuard, "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
            context.Registry.WriteValue(
                RegistryHive.LocalMachine, Hvci, "Enabled", 0, RegistryValueKind.DWord);

            context.Processes.StartCaptured("bcdedit /set hypervisorlaunchtype off");
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.StartCaptured("bcdedit /set hypervisorlaunchtype auto");
        }
    }
}
