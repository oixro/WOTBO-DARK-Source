using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Объединяет службы в меньшее число процессов svchost
    /// (<c>SvcHostSplitThresholdInKB</c>).
    ///
    /// На системах с 8+ ГБ Windows разносит службы по отдельным процессам —
    /// это надёжнее, но каждый процесс держит свой heap. Объединение экономит
    /// 100–300 МБ. Требует перезагрузки.
    /// </summary>
    public sealed class SvcHostMergeTweak : ValueListTweak
    {
        private static readonly TweakValue[] Items =
        {
            new TweakValue(
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control",
                "SvcHostSplitThresholdInKB",
                unchecked((int)0xFFFFFFFF)),
        };

        public override string Id
        {
            get { return "extra.svchostmerge"; }
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
