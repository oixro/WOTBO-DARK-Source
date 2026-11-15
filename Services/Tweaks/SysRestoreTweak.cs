using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключает точки восстановления системы.</summary>
    public sealed class SysRestoreTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", "RPSessionInterval", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", "DiskPercent", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", "LimitInternalGuidance", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore\cfg", "DiskPercent", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableConfig", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableSR", 1),
        };

        public override string Id
        {
            get { return "extra.sysrestore"; }
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
