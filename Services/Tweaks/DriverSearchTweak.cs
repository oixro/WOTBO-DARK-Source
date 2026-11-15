using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Windows Update больше не устанавливает драйверы автоматически.</summary>
    public sealed class DriverSearchTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\DriverSearching", "SearchOrderConfig", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "ExcludeWUDriversInQualityUpdate", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1),
        };

        public override string Id
        {
            get { return "extra.driversearch"; }
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
