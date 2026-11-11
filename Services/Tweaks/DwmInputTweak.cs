using System;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Обработка ввода DWM через IO completion port.</summary>
    public sealed class DwmInputTweak : TweakBase
    {
        public override string Id
        {
            get { return "main.dwminput"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = Read(
                RegistryHive.LocalMachine,
                RegistryPaths.WindowsKey,
                "DwmInputUsesIoCompletionPort",
                context);

            return string.Equals(Convert.ToString(current), "0", StringComparison.Ordinal)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.WindowsKey, "DesktopHeapLogging", 0, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.WindowsKey, "DwmInputUsesIoCompletionPort", 0, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.WindowsKey, "EnableDwmInputProcessing", 0, RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.WindowsKey, "DesktopHeapLogging", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.WindowsKey, "DwmInputUsesIoCompletionPort", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.WindowsKey, "EnableDwmInputProcessing", 7, RegistryValueKind.DWord);
        }
    }
}
