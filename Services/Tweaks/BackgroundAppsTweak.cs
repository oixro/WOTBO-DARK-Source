using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключает работу приложений в фоне.</summary>
    public sealed class BackgroundAppsTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2),
        };

        public override string Id
        {
            get { return "extra.backgroundapps"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        protected override TweakValue[] Values
        {
            get { return Items; }
        }
    }
}
