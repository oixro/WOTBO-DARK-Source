using System;
using System.IO;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключение Defender через pgkt04/defender-control (MIT).
    /// Инструмент запускается от TrustedInstaller, переименовывает драйверы
    /// и сохраняет restore-манифест в %ProgramData%\defender-control.
    /// </summary>
    public sealed class DefenderTweak : TweakBase
    {
        private const string LogCategory = "Tweak:main.defender";
        private const string Manifest = @"C:\ProgramData\defender-control\soft-deleted.txt";
        private const string DisableFallback =
            "https://github.com/pgkt04/defender-control/releases/download/v2.0/disable-defender.exe";
        private const string EnableFallback =
            "https://github.com/pgkt04/defender-control/releases/download/v2.0/enable-defender.exe";

        public override string Id { get { return "main.defender"; } }
        public override string Category { get { return "Main"; } }

        public override TweakState Detect(TweakContext context)
        {
            return File.Exists(Manifest) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string exe = Download(context, "disable-defender.exe", "defenderdisable", DisableFallback);
            if (exe == null) return;

            try
            {
                string output = context.Processes
                    .ReadOutputAsync(exe, "-s")
                    .GetAwaiter()
                    .GetResult();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    context.Log.Info(LogCategory, output.Trim());
                }

                context.Log.Warn(
                    LogCategory,
                    "defender-control: Tamper Protection нужно отключить вручную; после применения требуется перезагрузка");

                if (!File.Exists(Manifest))
                {
                    context.Log.Warn(LogCategory, "restore-манифест не создан — возможно, сработал Defender или не хватило прав");
                }
            }
            catch (Exception exception)
            {
                context.Log.Error(LogCategory, "не удалось запустить disable-defender.exe", exception);
            }
        }

        public override void Rollback(TweakContext context)
        {
            string exe = Download(context, "enable-defender.exe", "defenderenable", EnableFallback);
            if (exe == null) return;

            try
            {
                string output = context.Processes
                    .ReadOutputAsync(exe, "-s")
                    .GetAwaiter()
                    .GetResult();

                if (!string.IsNullOrWhiteSpace(output))
                {
                    context.Log.Info(LogCategory, output.Trim());
                }

                context.Log.Warn(LogCategory, "defender-control: для восстановления защитника требуется перезагрузка");
            }
            catch (Exception exception)
            {
                context.Log.Error(LogCategory, "не удалось запустить enable-defender.exe", exception);
            }
        }

        private static string Download(TweakContext context, string fileName, string key, string fallback)
        {
            string path = Path.Combine(context.Workspace.Root, fileName);
            if (File.Exists(path))
            {
                return path;
            }

            string url = null;
            try
            {
                url = context.LatestVersions.GetProgramUrlAsync(key).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                context.Log.Warn(LogCategory, "не удалось получить последнюю версию: " + exception.Message);
            }

            if (string.IsNullOrEmpty(url))
            {
                url = fallback;
            }

            try
            {
                context.Network.DownloadFile(url, path);
                return path;
            }
            catch (Exception exception)
            {
                context.Log.Error(LogCategory, "не удалось скачать " + fileName, exception);
                return null;
            }
        }
    }
}
