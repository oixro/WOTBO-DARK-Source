using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Удаление OneDrive (актуально для Windows 11, включая 24H2–26H2).
    ///
    /// Само удаление идёт отдельным процессом PowerShell: установщик OneDrive
    /// работает десятки секунд, и держать на это окно WOTBO нельзя. Detect
    /// при этом остаётся быстрым — проверяет процесс, файлы и автозапуск.
    /// </summary>
    public sealed class OneDriveTweak : TweakBase
    {
        private const string LogName = "Tweak:extra.onedrive";
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public override string Id
        {
            get { return "extra.onedrive"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            if (IsRunning())
            {
                return TweakState.NotApplied;
            }

            foreach (string directory in ProgramDirectories())
            {
                if (File.Exists(Path.Combine(directory, "OneDrive.exe")))
                {
                    return TweakState.NotApplied;
                }
            }

            foreach (string setup in SystemSetups())
            {
                if (File.Exists(setup))
                {
                    return TweakState.NotApplied;
                }
            }

            if (context.Registry.ValueExists(RegistryHive.CurrentUser, RunKey, "OneDrive"))
            {
                return TweakState.NotApplied;
            }

            return TweakState.Applied;
        }

        public override void Apply(TweakContext context)
        {
            string path = Path.Combine(context.Workspace.Root, "onedrive-remove.ps1");

            try
            {
                Directory.CreateDirectory(context.Workspace.Root);
                File.WriteAllText(path, Script, new UTF8Encoding(false));
            }
            catch (Exception exception)
            {
                context.Log.Error(LogName, "не удалось записать скрипт удаления", exception);
                return;
            }

            // Отдельный скрытый процесс: окно WOTBO не ждёт установщик.
            context.Processes.StartDetached(
                "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + path + "\"",
                true);

            context.Log.Info(LogName, "удаление OneDrive запущено в фоне");
        }

        public override void Rollback(TweakContext context)
        {
            // OneDrive удаляется без автоматического восстановления.
        }

        private static bool IsRunning()
        {
            try
            {
                return Process.GetProcessesByName("OneDrive").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<string> ProgramDirectories()
        {
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\OneDrive");
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft OneDrive");
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft OneDrive");
        }

        private static IEnumerable<string> SystemSetups()
        {
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            yield return Path.Combine(windows, @"System32\OneDriveSetup.exe");
            yield return Path.Combine(windows, @"SysWOW64\OneDriveSetup.exe");
        }

        private const string Script =
            "$ErrorActionPreference = 'SilentlyContinue'\r\n" +
            "$log = Join-Path $env:TEMP 'wotbo-onedrive.log'\r\n" +
            "function Log($m) { Add-Content -Path $log -Value ((Get-Date -Format 'HH:mm:ss') + ' ' + $m) }\r\n" +
            "Log 'start'\r\n" +
            "Get-Process OneDrive | Stop-Process -Force\r\n" +
            "$setups = @()\r\n" +
            "$setups += (Join-Path $env:windir 'System32\\OneDriveSetup.exe')\r\n" +
            "$setups += (Join-Path $env:windir 'SysWOW64\\OneDriveSetup.exe')\r\n" +
            "$setups += (Get-ChildItem (Join-Path $env:LOCALAPPDATA 'Microsoft\\OneDrive') -Filter OneDriveSetup.exe -Recurse | ForEach-Object { $_.FullName })\r\n" +
            "foreach ($s in ($setups | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique)) {\r\n" +
            "  Log ('uninstall ' + $s)\r\n" +
            "  Start-Process -FilePath $s -ArgumentList '/uninstall' -Wait -ErrorAction SilentlyContinue\r\n" +
            "}\r\n" +
            "Get-AppxPackage -AllUsers *OneDrive* | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\r\n" +
            "$dirs = @(\r\n" +
            "  (Join-Path $env:LOCALAPPDATA 'Microsoft\\OneDrive'),\r\n" +
            "  (Join-Path $env:ProgramFiles 'Microsoft OneDrive'),\r\n" +
            "  (Join-Path ${env:ProgramFiles(x86)} 'Microsoft OneDrive'),\r\n" +
            "  (Join-Path $env:ProgramData 'Microsoft OneDrive'),\r\n" +
            "  (Join-Path $env:APPDATA 'Microsoft\\OneDrive')\r\n" +
            ")\r\n" +
            "foreach ($d in $dirs) { if ($d -and (Test-Path -LiteralPath $d)) { Remove-Item -LiteralPath $d -Recurse -Force; Log ('rm ' + $d) } }\r\n" +
            "foreach ($f in @((Join-Path $env:windir 'System32\\OneDriveSetup.exe'), (Join-Path $env:windir 'SysWOW64\\OneDriveSetup.exe'))) {\r\n" +
            "  if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force; Log ('rm ' + $f) }\r\n" +
            "}\r\n" +
            "Remove-ItemProperty -Path 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run' -Name OneDrive\r\n" +
            "New-Item -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive' -Force | Out-Null\r\n" +
            "Set-ItemProperty -Path 'HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive' -Name DisableFileSyncNGSC -Value 1 -Type DWord\r\n" +
            "foreach ($k in @('HKCU:\\Software\\Classes\\CLSID\\{018D5C66-4533-4307-9B53-224DE2ED1FE6}', 'HKLM:\\SOFTWARE\\Classes\\CLSID\\{018D5C66-4533-4307-9B53-224DE2ED1FE6}')) {\r\n" +
            "  if (Test-Path $k) {\r\n" +
            "    Set-ItemProperty -Path $k -Name 'System.IsPinnedToNameSpaceTree' -Value 0 -Type DWord\r\n" +
            "    Set-ItemProperty -Path $k -Name 'Attributes' -Value 0 -Type DWord\r\n" +
            "  }\r\n" +
            "}\r\n" +
            "Get-ScheduledTask -TaskPath '\\Microsoft\\OneDrive\\*' | Unregister-ScheduledTask -Confirm:$false\r\n" +
            "Get-Process OneDrive | Stop-Process -Force\r\n" +
            "Log 'done'\r\n";
    }
}
