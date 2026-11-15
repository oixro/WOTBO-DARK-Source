using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Чистое контекстное меню: убирает «Поделиться», «Передать на устройство»,
    /// «Предоставить доступ» и пункт «Отправить», а «Создать ярлык на рабочем
    /// столе» выносит в главное меню отдельным пунктом.
    /// </summary>
    public sealed class CleanContextMenuTweak : TweakBase
    {
        private const string Blocked =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

        private const string SendTo =
            @"SOFTWARE\Classes\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo";

        private const string ShortcutVerb =
            @"SOFTWARE\Classes\AllFilesystemObjects\shell\WOTBO_DesktopShortcut";

        private const string ShortcutName = "Создать ярлык на рабочем столе";

        // Скрипт лежит в ProgramData: рабочая папка приложения чистится на старте.
        private static readonly string ShortcutScript = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            @"oixro\wotbo\desktop-shortcut.ps1");

        private const string ScriptBody =
            "param([string]$Path)\r\n" +
            "if ([string]::IsNullOrWhiteSpace($Path)) { return }\r\n" +
            "if (-not (Test-Path -LiteralPath $Path)) { return }\r\n" +
            "$name = [System.IO.Path]::GetFileNameWithoutExtension($Path)\r\n" +
            "if ([System.IO.Directory]::Exists($Path)) { $name = [System.IO.Path]::GetFileName($Path) }\r\n" +
            "if ([string]::IsNullOrWhiteSpace($name)) { $name = 'Shortcut' }\r\n" +
            "$desktop = [Environment]::GetFolderPath('Desktop')\r\n" +
            "$link = Join-Path $desktop ($name + '.lnk')\r\n" +
            "$index = 1\r\n" +
            "while (Test-Path -LiteralPath $link) { $link = Join-Path $desktop ($name + ' (' + $index + ').lnk'); $index++ }\r\n" +
            "$shell = New-Object -ComObject WScript.Shell\r\n" +
            "$shortcut = $shell.CreateShortcut($link)\r\n" +
            "$shortcut.TargetPath = $Path\r\n" +
            "$shortcut.WorkingDirectory = [System.IO.Path]::GetDirectoryName($Path)\r\n" +
            "$shortcut.Save()\r\n";

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
            bool cleaned = Items[0].Matches(context)
                && !context.Registry.KeyExists(RegistryHive.LocalMachine, SendTo);

            bool shortcut = context.Registry.KeyExists(RegistryHive.LocalMachine, ShortcutVerb)
                && File.Exists(ShortcutScript);

            return cleaned && shortcut ? TweakState.Applied : TweakState.NotApplied;
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

            WriteShortcutScript(context);

            context.BackupRegistryKey(RegistryHive.LocalMachine, ShortcutVerb);
            context.Registry.WriteValue(
                RegistryHive.LocalMachine, ShortcutVerb, string.Empty, ShortcutName, RegistryValueKind.String);
            context.Registry.WriteValue(
                RegistryHive.LocalMachine,
                ShortcutVerb + @"\command",
                string.Empty,
                "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + ShortcutScript + "\" \"%1\"",
                RegistryValueKind.String);
        }

        public override void Rollback(TweakContext context)
        {
            // Ветки и файл восстанавливает бэкап TweakScope: скрипт был
            // записан через BackupFile, поэтому при откате он удаляется.
        }

        private static void WriteShortcutScript(TweakContext context)
        {
            string directory = Path.GetDirectoryName(ShortcutScript);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            context.BackupFile(ShortcutScript);
            File.WriteAllText(ShortcutScript, ScriptBody, new UTF8Encoding(false));
        }
    }
}
