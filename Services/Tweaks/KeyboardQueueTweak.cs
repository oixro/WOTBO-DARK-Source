using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Уменьшает кольцевой буфер ввода клавиатуры и мыши (20 вместо 100).</summary>
    public sealed class KeyboardQueueTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\kbdclass\Parameters", "KeyboardDataQueueSize", 0x14),
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\mouclass\Parameters", "MouseDataQueueSize", 0x14),
        };

        public override string Id
        {
            get { return "main.kbqueue"; }
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
