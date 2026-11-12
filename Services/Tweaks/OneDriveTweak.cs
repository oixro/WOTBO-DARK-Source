using System;
using System.IO;
using System.Linq;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Удаление OneDrive через OneDriveSetup.exe /uninstall.</summary>
    public sealed class OneDriveTweak : TweakBase
    {
        private const string OneDriveExe = @"C:\Users\__USER__\AppData\Local\Microsoft\OneDrive\OneDrive.exe";

        public override string Id { get { return "extra.onedrive"; } }
        public override string Category { get { return "Extra"; } }

        public override TweakState Detect(TweakContext context)
        {
            string path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"AppData\Local\Microsoft\OneDrive\OneDrive.exe");

            return File.Exists(path) ? TweakState.NotApplied : TweakState.Applied;
        }

        public override void Apply(TweakContext context)
        {
            string[] setups = Directory.GetFiles(@"C:\Windows\System32", "OneDriveSetup.exe", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(@"C:\Windows\SysWOW64", "OneDriveSetup.exe", SearchOption.TopDirectoryOnly))
                .ToArray();

            if (setups.Length == 0)
            {
                context.Log.Warn("Tweak:extra.onedrive", "Файл OneDriveSetup.exe не найден");
                return;
            }

            string setup = setups[0];
            context.BackupFile(setup);
            context.Log.Info("Tweak:extra.onedrive", $"путь к Onedrive найден - {setup}, переходим к удалению");
            context.Processes.StartCaptured(setup + " /uninstall");
        }

        public override void Rollback(TweakContext context)
        {
            // OneDrive удаляется без автоматического восстановления.
        }
    }
}
