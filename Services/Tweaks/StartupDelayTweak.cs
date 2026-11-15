using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Убирает задержку автозапуска: приложения стартуют сразу после входа.</summary>
    public sealed class StartupDelayTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Serialize", "StartupDelayInMSec", 0),
            new TweakValue(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "WaitForIdleState", 0),
        };

        public override string Id
        {
            get { return "main.startupdelay"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        protected override TweakValue[] Values
        {
            get { return Items; }
        }
    }
}
