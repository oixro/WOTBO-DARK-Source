using System;
using System.IO;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>AlwaysOnTop.exe и автозапуск.</summary>
    public sealed class AlwaysOnTopTweak : TweakBase
    {
        private const string Exe = @"C:\Windows\AlwaysOnTop.exe";

        public override string Id { get { return "pro.alwaysontop"; } }
        public override string Category { get { return "Pro"; } }

        public override TweakState Detect(TweakContext context)
        {
            return File.Exists(Exe) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupFile(Exe);

            if (!File.Exists(Exe))
            {
                context.Network.DownloadFile(
                    "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/AlwaysOnTop.exe",
                    Exe);
            }

            context.Registry.WriteValue(RegistryHive.LocalMachine, RegistryPaths.RunKey, "AlwaysOnTop", Exe, RegistryValueKind.String);
            context.Processes.StartCaptured(Exe);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.LocalMachine, RegistryPaths.RunKey, "AlwaysOnTop");
            if (File.Exists(Exe))
            {
                File.Delete(Exe);
            }
        }
    }
}
