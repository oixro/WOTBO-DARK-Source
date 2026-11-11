using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Приоритет dwm.exe через Image File Execution Options.</summary>
    public sealed class DwmPriorityTweak : TweakBase
    {
        private static readonly string Key = RegistryPaths.ImageFileExecutionOptions + @"\dwm.exe";

        public override string Id
        {
            get { return "extra.dwmpriority"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.KeyExists(RegistryHive.LocalMachine, Key)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string perf = Key + @"\PerfOptions";
            context.Registry.WriteValue(RegistryHive.LocalMachine, perf, "CpuPriorityClass", "1", RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, perf, "IoPriority", "0", RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteKey(RegistryHive.LocalMachine, Key);
        }
    }
}
