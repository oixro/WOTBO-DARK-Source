using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Приоритет мультимедиа (MMCSS) и отключение сетевого троттлинга.</summary>
    public sealed class MmcssTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "SystemResponsiveness", 0),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF)),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "GPU Priority", 0x0C),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games", "Priority", 0x06),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing", "GPU Priority", 0x0C),
            new TweakValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\DisplayPostProcessing", "Priority", 0x08),
        };

        public override string Id
        {
            get { return "main.mmcss"; }
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
