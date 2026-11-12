using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Старое контекстное меню Windows 11.</summary>
    public sealed class ContextMenuTweak : TweakBase
    {
        private static readonly string InprocKey = RegistryPaths.ContextMenuClsid + @"\InprocServer32";

        public override string Id
        {
            get { return "ui.contextmenu"; }
        }

        public override string Category
        {
            get { return "UI"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.KeyExists(RegistryHive.CurrentUser, InprocKey)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.CurrentUser, RegistryPaths.ContextMenuClsid);
            context.Registry.WriteValue(RegistryHive.CurrentUser, InprocKey, string.Empty, " ", RegistryValueKind.String);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteKey(RegistryHive.CurrentUser, RegistryPaths.ContextMenuClsid);
            context.Processes.StartCaptured(@"taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
        }
    }
}
