using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключает watchdog'и и отладочные замеры NDIS.</summary>
    public sealed class NdisWatchdogTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters", "DisableNDISWatchDog", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters", "DisableNaps", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters", "NoPauseOnSuspend", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters", "DebugLoggingMode", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters", "DisableWDIWatchdogForceBugcheck", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters", "DisableReenumerationTimeoutBugcheck", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NDIS\Parameters", "EnableNicAutoPowerSaverInSleepStudy", 0),
        };

        public override string Id
        {
            get { return "extra.ndis"; }
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
