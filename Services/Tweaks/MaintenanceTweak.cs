using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключает автоматическое обслуживание Windows.</summary>
    public sealed class MaintenanceTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\Maintenance", "MaintenanceDisabled", 1),
        };

        public override string Id
        {
            get { return "extra.maintenance"; }
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
