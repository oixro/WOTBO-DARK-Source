using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключает задержку IPI последнего владельца часов.</summary>
    public sealed class IpiLatencyTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power", "IpiLastClockOwnerDisable", 1),
        };

        public override string Id
        {
            get { return "main.ipilatency"; }
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
