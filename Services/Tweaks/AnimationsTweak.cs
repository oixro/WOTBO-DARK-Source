using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Выключение анимации панелей WOTBO.</summary>
    public sealed class AnimationsTweak : TweakBase
    {
        private const string ValueName = "animationsEnabled";

        public override string Id { get { return "ui.animations"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            object value = context.Registry.GetValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, ValueName);
            if (value == null)
            {
                // Старые версии хранили флаг "animations"; после миграции
                // состояние остаётся только в TweakStateStore.
                return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
            }

            return (int)value == 0 ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.CurrentUser, RegistryPaths.AppRoot);
            context.Registry.WriteValue(
                RegistryHive.CurrentUser,
                RegistryPaths.AppRoot,
                ValueName,
                0,
                RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, ValueName);
        }
    }
}
