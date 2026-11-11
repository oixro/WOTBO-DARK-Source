using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>FFmpeg: скачивает последний essentials build и регистрирует ассоциации.</summary>
    public sealed class FfmpegTweak : TweakBase
    {
        private const string FallbackUrl =
            "https://github.com/GyanD/codexffmpeg/releases/download/7.1/ffmpeg-7.1-essentials_build.zip";
        private const string RegUrl =
            "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/ffmpeg.reg";
        private const string TargetExe = @"C:\Windows\ffmpeg.exe";

        public override string Id { get { return "ui.ffmpeg"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            return File.Exists(TargetExe)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string root = context.Workspace.Root;
            string zip = Path.Combine(root, "ffmpeg.zip");
            string reg = Path.Combine(root, "ffmpeg.reg");
            string extract = Path.Combine(root, "ffmpeg-extract");

            string url = context.LatestVersions
                .GetLatestAssetUrlAsync("GyanD/codexffmpeg", "essentials_build", ".zip")
                .GetAwaiter()
                .GetResult();

            if (string.IsNullOrEmpty(url))
            {
                context.Log.Warn("Tweak:ui.ffmpeg", "не удалось определить последнюю версию, использую fallback");
                url = FallbackUrl;
            }

            context.Network.DownloadFile(url, zip);
            context.Network.DownloadFile(RegUrl, reg);

            if (Directory.Exists(extract))
            {
                Directory.Delete(extract, true);
            }

            ZipFile.ExtractToDirectory(zip, extract);

            string ffmpegExe = FindFfmpeg(extract);
            if (string.IsNullOrEmpty(ffmpegExe))
            {
                context.Log.Warn("Tweak:ui.ffmpeg", "в архиве не найден ffmpeg.exe");
                return;
            }

            if (File.Exists(TargetExe))
            {
                context.BackupFile(TargetExe);
                context.Processes.StartCaptured("taskkill /f /im ffmpeg.exe");
                File.Delete(TargetExe);
            }

            File.Copy(ffmpegExe, TargetExe, true);

            context.BackupRegistryKey(RegistryHive.CurrentUser, RegistryPaths.FileAssociations);
            context.BackupRegFile(reg);
            context.Processes.StartCaptured("regedit.exe /s \"" + reg + "\"");
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.StartCaptured("taskkill /f /im ffmpeg.exe");
            context.Registry.DeleteKey(RegistryHive.CurrentUser, RegistryPaths.FileAssociations);
            if (File.Exists(TargetExe))
            {
                File.Delete(TargetExe);
            }
        }

        private static string FindFfmpeg(string root)
        {
            foreach (string file in Directory.GetFiles(root, "ffmpeg.exe", SearchOption.AllDirectories))
            {
                return file;
            }

            return null;
        }
    }
}
