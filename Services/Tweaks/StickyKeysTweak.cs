using System;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключение залипания клавиш (StickyKeys Flags = 506).</summary>
    public sealed class StickyKeysTweak : TweakBase
    {
        private const string Path = @"Control Panel\Accessibility\StickyKeys";
        private const string Value = "Flags";
        private const string AppliedValue = "506";
        private const string DefaultValue = "510";

        public override string Id
        {
            get { return "extra.stickykeys"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = Read(RegistryHive.CurrentUser, Path, Value, context);
            return string.Equals(Convert.ToString(current), AppliedValue, StringComparison.Ordinal)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.CurrentUser, Path, Value, AppliedValue, RegistryValueKind.String);
        }

        public override void Rollback(TweakContext context)
        {
            // Прежнее поведение: вернуть стандартное значение, а точное
            // прежнее состояние восстановит TweakScope из бэкапа.
            context.Registry.WriteValue(RegistryHive.CurrentUser, Path, Value, DefaultValue, RegistryValueKind.String);
        }
    }
}
