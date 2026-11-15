using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Убирает всплывающее окно Xbox Game Bar.
    ///
    /// Обработчики <c>ms-gamebar</c> и <c>ms-gamebarservices</c> перенаправляются
    /// на <c>systray.exe</c>: Windows больше не предлагает «Нажмите Win+G».
    /// NVIDIA-твики и Ansel не затрагиваются — это отдельные ветки реестра.
    /// </summary>
    public sealed class GameBarTweak : TweakBase
    {
        private const string GameBar = @"SOFTWARE\Classes\ms-gamebar";
        private const string GameBarServices = @"SOFTWARE\Classes\ms-gamebarservices";
        private const string Systray = @"C:\Windows\System32\systray.exe";

        public override string Id
        {
            get { return "gpu.gamebar"; }
        }

        public override string Category
        {
            get { return "GPU"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            string value = Read(RegistryHive.LocalMachine, GameBar, string.Empty, context) as string;
            return string.Equals(value, "URL:ms-gamebar", System.StringComparison.Ordinal)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            foreach (string path in new[] { GameBar, GameBarServices })
            {
                context.BackupRegistryKey(RegistryHive.LocalMachine, path);

                string scheme = path == GameBar ? "URL:ms-gamebar" : "URL:ms-gamebarservices";

                context.Registry.WriteValue(RegistryHive.LocalMachine, path, string.Empty, scheme, RegistryValueKind.String);
                context.Registry.WriteValue(RegistryHive.LocalMachine, path, "URL Protocol", string.Empty, RegistryValueKind.String);
                context.Registry.WriteValue(RegistryHive.LocalMachine, path + @"\shell\open\command", string.Empty, Systray, RegistryValueKind.String);
            }
        }

        public override void Rollback(TweakContext context)
        {
            // Ветки восстанавливает бэкап TweakScope.
        }
    }
}
