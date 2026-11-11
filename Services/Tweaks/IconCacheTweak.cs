using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Увеличение кэша иконок (Max Cached Icons = 4096).</summary>
    public sealed class IconCacheTweak : TweakBase
    {
        private const string Path = WOTBO.Services.Registry.RegistryPaths.Explorer;
        private const string Value = WOTBO.Services.Registry.RegistryPaths.MaxCachedIcons;

        public override string Id
        {
            get { return "extra.iconcache"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.ValueExists(RegistryHive.LocalMachine, Path, Value)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.LocalMachine, Path, Value, 4096, RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.LocalMachine, Path, Value);
        }
    }
}
