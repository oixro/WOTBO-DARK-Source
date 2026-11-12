using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;
using WOTBO.Properties;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Пункт WOTBO в контекстном меню рабочего стола.</summary>
    public sealed class WotboContextTweak : TweakBase
    {
        private const string Gear = @"C:\Windows\gear.ico";
        private const string WotboExe = @"C:\Windows\wotbo.exe";

        public override string Id { get { return "ui.wotbocontext"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Registry.KeyExists(RegistryHive.ClassesRoot, RegistryPaths.DesktopBackgroundWotbo)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.ClassesRoot, RegistryPaths.DesktopBackgroundWotbo);
            context.BackupFile(Gear);
            context.BackupFile(WotboExe);

            if (!File.Exists(Gear))
            {
                string zip = Path.Combine(context.Workspace.Root, "gear.zip");
                File.WriteAllBytes(zip, Resources.gear);
                ZipFile.ExtractToDirectory(zip, @"C:\Windows");
            }

            string key = RegistryPaths.DesktopBackgroundWotbo;
            context.Registry.WriteValue(RegistryHive.ClassesRoot, key, string.Empty, "WOTBO", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.ClassesRoot, key, "icon", Gear, RegistryValueKind.String);
            context.Registry.WriteValue(
                RegistryHive.ClassesRoot,
                key + @"\command",
                string.Empty,
                WotboExe,
                RegistryValueKind.String);

            if (!File.Exists(WotboExe))
            {
                File.Copy(Assembly.GetEntryAssembly().Location, WotboExe);
            }
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteKey(RegistryHive.ClassesRoot, RegistryPaths.DesktopBackgroundWotbo);
            if (File.Exists(Gear)) File.Delete(Gear);
            if (File.Exists(WotboExe)) File.Delete(WotboExe);
        }
    }
}
