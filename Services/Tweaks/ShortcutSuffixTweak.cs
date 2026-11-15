using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Убирает приписку « - Ярлык» у ярлыков.</summary>
    public sealed class ShortcutSuffixTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "link", new byte[] { 0, 0, 0, 0 }),
        };

        public override string Id
        {
            get { return "ui.shortcutsuffix"; }
        }

        public override string Category
        {
            get { return "UI"; }
        }

        protected override TweakValue[] Values
        {
            get { return Items; }
        }
    }
}
