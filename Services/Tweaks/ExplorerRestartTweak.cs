using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Пункт «Перезапустить проводник» в контекстном меню.</summary>
    public sealed class ExplorerRestartTweak : TweakBase
    {
        public override string Id { get { return "ui.explorerrestart"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.KeyExists(RegistryHive.ClassesRoot, RegistryPaths.DesktopBackgroundExplorer)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string key = RegistryPaths.DesktopBackgroundExplorer;
            context.Registry.WriteValue(RegistryHive.ClassesRoot, key, string.Empty, "Restart explorer.exe", RegistryValueKind.String);

            bool win10 = context.WindowsVersion.Detect().IsWindows10;
            context.Registry.WriteValue(
                RegistryHive.ClassesRoot,
                key,
                "icon",
                win10 ? "imageres.dll,264" : "imageres.dll,265",
                RegistryValueKind.String);

            context.Registry.WriteValue(
                RegistryHive.ClassesRoot,
                key + @"\command",
                string.Empty,
                @"c:\Windows\system32\explorer.bat",
                RegistryValueKind.String);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteKey(RegistryHive.ClassesRoot, RegistryPaths.DesktopBackgroundExplorer);
        }
    }
}
