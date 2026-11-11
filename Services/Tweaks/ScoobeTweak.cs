using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключение подсказок/предложений Windows (SCOOBE).</summary>
    public sealed class ScoobeTweak : TweakBase
    {
        public override string Id
        {
            get { return "extra.scoobe"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.ValueExists(
                    RegistryHive.CurrentUser,
                    RegistryPaths.UserProfileEngagement,
                    RegistryPaths.ScoobeSystemSettingEnabled)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(
                RegistryHive.CurrentUser,
                RegistryPaths.UserProfileEngagement,
                RegistryPaths.ScoobeSystemSettingEnabled,
                0,
                RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(
                RegistryHive.CurrentUser,
                RegistryPaths.UserProfileEngagement,
                RegistryPaths.ScoobeSystemSettingEnabled);
        }
    }
}
