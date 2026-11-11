using System.IO;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Политики Windows Update из updates.reg.</summary>
    public sealed class WindowsUpdateTweak : TweakBase
    {
        public override string Id { get { return "pro.windowsupdate"; } }
        public override string Category { get { return "Pro"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.KeyExists(RegistryHive.LocalMachine, RegistryPaths.WindowsUpdatePolicy + @"\AU")
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string path = Path.Combine(context.Workspace.Root, "updates.reg");
            if (!File.Exists(path))
            {
                context.Log.Warn("Tweak:pro.windowsupdate", "файл не найден: " + path);
                return;
            }

            context.BackupRegFile(path);
            context.Processes.StartCaptured("regedit /s \"" + path + "\"");
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteKey(RegistryHive.LocalMachine, RegistryPaths.WindowsUpdatePolicy + @"\AU");
        }
    }
}
