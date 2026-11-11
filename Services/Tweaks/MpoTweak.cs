using System;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>MPO / OverlayTestMode = 5.</summary>
    public sealed class MpoTweak : TweakBase
    {
        public override string Id
        {
            get { return "main.mpo"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = Read(RegistryHive.LocalMachine, RegistryPaths.Dwm, RegistryPaths.OverlayTestMode, context);
            return current != null && Convert.ToInt32(current) == 5
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.Dwm,
                RegistryPaths.OverlayTestMode,
                5,
                RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.Dwm,
                RegistryPaths.OverlayTestMode);
        }
    }
}
