using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Чистое контекстное меню: убирает «Поделиться», «Передать на устройство»,
    /// «Предоставить доступ» и пункт «Отправить».
    /// </summary>
    public sealed class CleanContextMenuTweak : TweakBase
    {
        private const string Blocked =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

        private const string SendTo =
            @"SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo";

        private static readonly TweakValue[] Items =
        {
            new TweakValue(RegistryHive.LocalMachine, Blocked, "{7AD84985-87B4-4a16-BE58-8B72A5B390F7}", "Play to Menu"),
            new TweakValue(RegistryHive.LocalMachine, Blocked, "{e2bf9676-5f8f-435c-97eb-11607a5bedf7}", "Share"),
            new TweakValue(RegistryHive.LocalMachine, Blocked, "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}", "Give access to"),
            new TweakValue(RegistryHive.LocalMachine, Blocked, "{596AB062-B4D2-4215-9F74-E9109B0A8153}", ""),
        };

        public override string Id
        {
            get { return "ui.cleancontext"; }
        }

        public override string Category
        {
            get { return "UI"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return Items[0].Matches(context) && !context.Registry.KeyExists(RegistryHive.LocalMachine, SendTo)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            foreach (TweakValue item in Items)
            {
                context.Registry.WriteValue(item.Hive, item.Path, item.Name, item.Value, item.Kind);
            }

            context.BackupRegistryKey(RegistryHive.LocalMachine, SendTo);
            if (context.Registry.KeyExists(RegistryHive.LocalMachine, SendTo))
            {
                context.Registry.DeleteKey(RegistryHive.LocalMachine, SendTo);
            }
        }

        public override void Rollback(TweakContext context)
        {
            // Значения и удалённую ветку восстанавливает бэкап TweakScope.
        }
    }
}
