using System;
using System.IO;
using System.IO.Compression;
using WOTBO.Properties;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Синие папки проводника (imageres.dll).</summary>
    public sealed class BlueFoldersTweak : TweakBase
    {
        private const string Imageres = @"C:\Windows\System32\imageres.dll";
        private const string ImageresMun = @"C:\Windows\SystemResources\imageres.dll.mun";

        public override string Id { get { return "ui.bluefolders"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            return File.Exists(Imageres + "_bak") || File.Exists(ImageresMun + "_bak")
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupFile(Imageres);
            context.BackupFile(ImageresMun);

            string root = context.Workspace.Root;
            if (context.WindowsVersion.Detect().IsWindows10)
            {
                string bat = Path.Combine(root, "blueiconsw10.bat");
                File.WriteAllText(bat, Resources.blueiconsw10);

                string zip = Path.Combine(root, "imageres.zip");
                if (!File.Exists(zip))
                {
                    context.Network.DownloadFile(
                        "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/imageres.zip",
                        zip);
                    ZipFile.ExtractToDirectory(zip, root);
                }

                context.Processes.StartCaptured(bat);
            }
            else
            {
                string bat = Path.Combine(root, "blueicons.bat");
                File.WriteAllText(bat, Resources.blueicons);

                string zip = Path.Combine(root, "BlueIcon_Minimal.zip");
                if (!File.Exists(zip))
                {
                    context.Network.DownloadFile(
                        "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/BlueIcon_Minimal.zip",
                        zip);
                    ZipFile.ExtractToDirectory(zip, Path.Combine(root, "BlueIcon Minimal"));
                }

                context.Processes.StartCaptured(bat);
            }
        }

        public override void Rollback(TweakContext context)
        {
            string root = context.Workspace.Root;
            if (context.WindowsVersion.Detect().IsWindows10)
            {
                string bat = Path.Combine(root, "deficonsw10.bat");
                File.WriteAllText(bat, Resources.deficonsw10);
                context.Processes.StartCaptured(bat);
            }
            else
            {
                string bat = Path.Combine(root, "defblueicons.bat");
                File.WriteAllText(bat, Resources.defblueicons);
                context.Processes.StartCaptured(bat);
            }
        }
    }
}
