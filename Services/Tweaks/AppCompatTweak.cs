using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключает помощник совместимости и сбор телеметрии приложений.</summary>
    public sealed class AppCompatTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "AITEnable", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "DisableEngine", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "DisableInventory", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "DisablePCA", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "DisableUAR", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppCompat", "SbEnable", 0),
        };

        public override string Id
        {
            get { return "extra.appcompat"; }
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
