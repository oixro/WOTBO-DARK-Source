using System;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Spectre/Meltdown mitigations: Memory Management и DeviceGuard.</summary>
    public sealed class MitigationsTweak : TweakBase
    {
        public override string Id
        {
            get { return "main.mitigations"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = Read(
                RegistryHive.LocalMachine,
                RegistryPaths.MemoryManagement,
                "FeatureSettings",
                context);

            return current != null && Convert.ToInt32(current) == 1
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, "FeatureSettings", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, "FeatureSettingsOverride", 3, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, "FeatureSettingsOverrideMask", 3, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, "EnableCfg", 0, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Kernel, "ProtectionMode", 0, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Kernel, "DisableExceptionChainValidation", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.DeviceGuardHvci, "Enabled", 0, RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, "FeatureSettings");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, "FeatureSettingsOverride");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, "FeatureSettingsOverrideMask");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, "EnableCfg");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.Kernel, "ProtectionMode");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.Kernel, "DisableExceptionChainValidation");
            context.Registry.DeleteKey(RegistryHive.LocalMachine, RegistryPaths.DeviceGuardHvci);
        }
    }
}
