using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключает SleepStudy и фоновое сканирование простоя.</summary>
    public sealed class SleepStudyTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "SleepStudyDisabled", 1),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "IdleScanInterval", 0),
        };

        public override string Id
        {
            get { return "extra.sleepstudy"; }
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
