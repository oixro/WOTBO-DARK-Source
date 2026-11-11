using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Уменьшение кнопок окна через WindowMetrics.</summary>
    public sealed class ButtonsTweak : TweakBase
    {
        public override string Id { get { return "ui.buttons"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.WindowMetrics, "CaptionHeight", "-270", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.WindowMetrics, "CaptionWidth", "-270", RegistryValueKind.String);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.WindowMetrics, "CaptionHeight", "-330", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.WindowMetrics, "CaptionWidth", "-330", RegistryValueKind.String);
        }
    }
}
