using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// USB power saving. Registry-часть; .bat/.cmd запускает Form1,
    /// потому что там нужен текущий tempfolder.
    /// </summary>
    public sealed class UsbPowerTweak : TweakBase
    {
        public override string Id
        {
            get { return "main.usb"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.ValueExists(
                    RegistryHive.LocalMachine,
                    RegistryPaths.UsbHubHubg,
                    "DisableOnSoftRemove")
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.UsbHubHubg, "DisableOnSoftRemove", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Xusb22Parameters, "IoQueueWorkItem", 0x0a, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.UsbXhciParameters, "IoQueueWorkItem", 0x0a, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.UsbHubParameters, "IoQueueWorkItem", 0x0a, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.EnumUsb, "AllowIdleIrpInD3", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.EnumUsb, "EnhancedPowerManagementEnabled", 0, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.UsbXhciWdf, "NoExtraBufferRoom", 0, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.UsbFlags, "fid_D1Latency", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.UsbFlags, "fid_D2Latency", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.UsbFlags, "fid_D3Latency", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.EnhancedStorageDevices, "TCGSecurityActivationDisabled", 1, RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.UsbHubHubg, "DisableOnSoftRemove");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.Xusb22Parameters, "IoQueueWorkItem");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.UsbXhciParameters, "IoQueueWorkItem");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.UsbHubParameters, "IoQueueWorkItem");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.EnumUsb, "AllowIdleIrpInD3");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.EnumUsb, "EnhancedPowerManagementEnabled");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.UsbXhciWdf, "NoExtraBufferRoom");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.UsbFlags, "fid_D1Latency");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.UsbFlags, "fid_D2Latency");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.UsbFlags, "fid_D3Latency");
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.EnhancedStorageDevices, "TCGSecurityActivationDisabled", 0, RegistryValueKind.DWord);
        }
    }
}
