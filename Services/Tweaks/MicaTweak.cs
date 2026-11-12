using System.IO;
using System.IO.Compression;
using System.Threading;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Прозрачный проводник (ExplorerBlurMica).</summary>
    public sealed class MicaTweak : TweakBase
    {
        private const string Dll = @"C:\Windows\ExplorerBlurMica.dll";
        private const string Config = @"C:\Windows\config.ini";

        public override string Id { get { return "ui.mica"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            return File.Exists(Dll) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string root = context.Workspace.Root;
            string zip = Path.Combine(root, "Release_x64.zip");
            if (!File.Exists(zip))
            {
                string url = context.LatestVersions
                    .GetProgramUrlAsync("explorerblurmica")
                    .GetAwaiter()
                    .GetResult();

                if (string.IsNullOrEmpty(url))
                {
                    url = "https://github.com/Maplespe/ExplorerBlurMica/releases/download/2.1.0/Release_x64.zip";
                }

                context.Network.DownloadFile(url, zip);
                ZipFile.ExtractToDirectory(zip, root);
            }

            context.BackupFile(Dll);
            context.BackupFile(Config);
            File.Copy(Path.Combine(root, "Release", "ExplorerBlurMica.dll"), Dll, true);
            File.Copy(Path.Combine(root, "Release", "config.ini"), Config, true);
            context.Processes.StartCaptured(
                "regsvr32 /s C:\\Windows\\ExplorerBlurMica.dll & taskkill /f /im explorer.exe & explorer.exe & timeout /t 1 & explorer.exe");
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.StartCaptured(
                "regsvr32 /s /u C:\\Windows\\ExplorerBlurMica.dll & taskkill /f /im explorer.exe & explorer.exe & timeout /t 1 & explorer.exe");
            Thread.Sleep(1000);
            if (File.Exists(Dll)) File.Delete(Dll);
            if (File.Exists(Config)) File.Delete(Config);
        }
    }
}
