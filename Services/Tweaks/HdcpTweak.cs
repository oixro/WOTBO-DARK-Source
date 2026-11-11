using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>HDCP: RMHdcpKeyglobZero.</summary>
    public sealed class HdcpTweak : TweakBase
    {
        public override string Id
        {
            get { return "gpu.hdcp"; }
        }

        public override string Category
        {
            get { return "GPU"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.ValueExists(RegistryHive.LocalMachine, RegistryPaths.HdcpPath, "RMHdcpKeyglobZero")
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.HdcpPath, "RMHdcpKeyglobZero", 1, RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.HdcpPath, "RMHdcpKeyglobZero");
        }
    }
}
