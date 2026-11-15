using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>NVIDIA: PowerMizer на максимум, динамические p-state отключены.</summary>
    public sealed class NvidiaPowerTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "DisableDynamicPstate", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "PowerMizerEnable", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "PowerMizerLevel", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "PowerMizerLevelAC", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "PowerMizerHardLevel", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "PowerMizerHardLevelAC", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "PowerMizerDefault", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "PowerMizerDefaultAC", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "DisableAsyncPstates", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RmPerfCfOverride", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RmPerfCfPolicyOverrides", 3),
        };

        public override string Id
        {
            get { return "gpu.nvpower"; }
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
