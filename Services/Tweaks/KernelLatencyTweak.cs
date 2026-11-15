using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Непроверенные значения планировщика ядра: DPC, таймеры, кэши.</summary>
    public sealed class KernelLatencyTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", "TimerCoalescing", new byte[64]),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "DpcTimeout", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "DpcWatchdogPeriod", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "DpcWatchdogProfileOffset", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "DpcWatchdogProfileSingleDpcThreshold", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "IdealDpcRate", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "InterruptSteeringDisabled", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "MaxDynamicTickDuration", 1000),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "MaximumDpcQueueDepth", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "MinDynamicTickDuration", 1000),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "MinimumDpcRate", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "SplitLargeCaches", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "StackSubSystemStackSize", 0x10000),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "ThreadDpcEnable", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "UseNewEaBuffering", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "UseNormalStack", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel", "XMMIZeroingEnable", 0),
        };

        public override string Id
        {
            get { return "extra.kernellatency"; }
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
