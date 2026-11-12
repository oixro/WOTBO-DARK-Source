using System;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Registry-часть аудио-твика. Сам .reg-пакет запускает MainForm,
    /// потому что он идёт через su.exe.
    /// </summary>
    public sealed class AudioTweak : TweakBase
    {
        public override string Id
        {
            get { return "main.audio"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = Read(
                RegistryHive.LocalMachine,
                RegistryPaths.BackgroundAudioPolicy,
                RegistryPaths.AllowHeadlessExecution,
                context);

            return string.Equals(Convert.ToString(current), "1", StringComparison.Ordinal)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.RunSuper(
                "regedit /s \"" + context.Workspace.Root + "\\Audio_Lantency.reg\"",
                context.Workspace.SuExecutable);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Audio, "DisableSpatialOnComboEndpoints", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Audio, "DisableProtectedAudioDG", 1, RegistryValueKind.DWord);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Audio, "DisableProtectedAudio", 1, RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.RunSuper(
                "regedit /s \"" + context.Workspace.Root + "\\Audio_Lantency_delete.reg\"",
                context.Workspace.SuExecutable);
            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.Audio, "DisableSpatialOnComboEndpoints", 0, RegistryValueKind.DWord);
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.Audio, "DisableProtectedAudioDG");
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.Audio, "DisableProtectedAudio");
        }
    }
}
