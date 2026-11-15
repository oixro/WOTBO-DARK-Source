using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>NVIDIA: отключает телеметрию и подробные логи драйвера.</summary>
    public sealed class NvidiaTelemetryTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMNvTelemetryCollection", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMNvTelemetryTest", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMNvLog", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMIntrDetailedLogs", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "MaxPerfWithPerfMon", 0),
        };

        public override string Id
        {
            get { return "gpu.nvtelemetry"; }
        }

        public override string Category
        {
            get { return "GPU"; }
        }

        protected override TweakValue[] Values
        {
            get { return Items; }
        }
    }
}
