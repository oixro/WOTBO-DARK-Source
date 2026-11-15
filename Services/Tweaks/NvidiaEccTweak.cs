using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>NVIDIA: отключает ECC, коррекцию ошибок и фоновые скрабберы памяти.</summary>
    public sealed class NvidiaEccTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMAERRForceDisable", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMNoECCFuseCheck", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMDisableRCOnDBE", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RM1441072", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMAERRHandling", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMChkSuppl200405980Driv", 0x13deed31),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMNoECCFBScrub", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMDisableScrubOnFree", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMDisableAsyncMemScrub", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMDisableFastScrubber", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000", "RMUcodeEncryption", 0x15555),
        };

        public override string Id
        {
            get { return "gpu.nvecc"; }
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
