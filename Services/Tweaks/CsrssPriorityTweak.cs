using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Приоритет csrss.exe + SystemProfile NoLazyMode/AlwaysOn.</summary>
    public sealed class CsrssPriorityTweak : TweakBase
    {
        private static readonly string Key = RegistryPaths.ImageFileExecutionOptions + @"\csrss.exe";

        public override string Id
        {
            get { return "pro.csrss"; }
        }

        public override string Category
        {
            get { return "Pro"; }
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
            context.Registry.WriteValue(RegistryHive.LocalMachine, perf, "CpuPriorityClass", "3", RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, perf, "IoPriority", "3", RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "NoLazyMode", "1", RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "AlwaysOn", "1", RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteKey(RegistryHive.LocalMachine, Key);
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "NoLazyMode");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.SystemProfile, "AlwaysOn");
        }
    }
}
