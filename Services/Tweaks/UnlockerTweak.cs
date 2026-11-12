using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Unlocker в контекстном меню.</summary>
    public sealed class UnlockerTweak : TweakBase
    {
        private const string Directory = @"C:\Windows\unlocker";
        private const string Exe = @"C:\Windows\unlocker\Unlocker.exe";

        public override string Id { get { return "pro.unlocker"; } }
        public override string Category { get { return "Pro"; } }

        public override TweakState Detect(TweakContext context)
        {
            return File.Exists(Exe) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.ClassesRoot, RegistryPaths.UnlockerFile);
            context.BackupRegistryKey(RegistryHive.ClassesRoot, RegistryPaths.UnlockerDirectory);
            context.BackupDirectory(Directory);

            string root = context.Workspace.Root;
            string zip = Path.Combine(root, "unlocker.zip");
            if (!File.Exists(zip))
            {
                context.Network.DownloadFile(
                    "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/unlocker.zip",
                    zip);
                ZipFile.ExtractToDirectory(zip, root);
            }

            string source = Path.Combine(root, "unlocker");
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, true);
            }

            System.IO.Directory.Move(source, Directory);

            context.Registry.WriteValue(RegistryHive.ClassesRoot, RegistryPaths.UnlockerFile, "Icon", @"C:\Windows\unlocker\Icons\Unlocker.ico", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.ClassesRoot, RegistryPaths.UnlockerFile + @"\command", "", "\"C:\\Windows\\unlocker\\Unlocker.exe\" /contextMenu \"%1\"", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.ClassesRoot, RegistryPaths.UnlockerDirectory, "", "Уничтожить", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.ClassesRoot, RegistryPaths.UnlockerDirectory, "Icon", @"C:\Windows\unlocker\Icons\Unlocker.ico", RegistryValueKind.String);
            context.Registry.WriteValue(RegistryHive.ClassesRoot, RegistryPaths.UnlockerDirectory + @"\command", "", "\"C:\\Windows\\unlocker\\Unlocker.exe\" /contextMenu \"%1\"", RegistryValueKind.String);
        }

        public override void Rollback(TweakContext context)
        {
            if (System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.Delete(Directory, true);
            }

            context.Registry.DeleteKey(RegistryHive.ClassesRoot, RegistryPaths.UnlockerFile);
            context.Registry.DeleteKey(RegistryHive.ClassesRoot, RegistryPaths.UnlockerDirectory);
        }
    }
}
