using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>NVIDIA tweaks: Miracast, DisplayPowerSaving, tiled display и т.д.</summary>
    public sealed class NvidiaTweaksTweak : TweakBase
    {
        public override string Id
        {
            get { return "gpu.nvidia"; }
        }

        public override string Category
        {
            get { return "GPU"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.ValueExists(
                    RegistryHive.LocalMachine,
                    RegistryPaths.NvidiaTweaksPath,
                    "DisplayPowerSaving")
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.NvidiaColor, "NvCplUseColorCorrection", "0", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.GraphicsDrivers, "PlatformSupportMiracast", "0", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.NvidiaTweaksPath, "DisplayPowerSaving", "0", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.HdcpPath, "EnableTiledDisplay", "0", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.NvidiaFts, "EnableRID61684", "1", RegistryValueKind.String);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.CurrentUser, RegistryPaths.NvidiaColor, "NvCplUseColorCorrection");
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.GraphicsDrivers, "PlatformSupportMiracast", "1", RegistryValueKind.String);
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.NvidiaTweaksPath, "DisplayPowerSaving");
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.HdcpPath, "EnableTiledDisplay", "0", RegistryValueKind.String);
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.NvidiaFts, "EnableRID61684");
        }
    }
}
