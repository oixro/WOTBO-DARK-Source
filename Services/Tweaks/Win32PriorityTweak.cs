using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Win32PrioritySeparation. Значение выбирается в диалоге
    /// <c>Win32Priority</c>; твик отвечает за обнаружение, резервную копию и откат.
    /// </summary>
    public sealed class Win32PriorityTweak : TweakBase
    {
        public override string Id { get { return "pro.win32priority"; } }
        public override string Category { get { return "Pro"; } }

        public override TweakState Detect(TweakContext context)
        {
            object value = context.Registry.GetValue(
                RegistryHive.LocalMachine,
                RegistryPaths.PriorityControl,
                "Win32PrioritySeparation");

            if (value == null)
            {
                return TweakState.NotApplied;
            }

            return (int)value == 0x00000002 ? TweakState.NotApplied : TweakState.Applied;
        }

        public override void Apply(TweakContext context)
        {
            // Значение записывает диалог Win32Priority после выбора пользователя.
            context.BackupRegistryKey(RegistryHive.LocalMachine, RegistryPaths.PriorityControl);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.WriteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.PriorityControl,
                "Win32PrioritySeparation",
                0x00000002,
                RegistryValueKind.DWord);
        }
    }
}
