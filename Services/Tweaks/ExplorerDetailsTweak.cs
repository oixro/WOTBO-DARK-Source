using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Дополнительные настройки проводника.</summary>
    public sealed class ExplorerDetailsTweak : TweakBase
    {
        public override string Id { get { return "ui.explorerdetails"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            bool win10 = context.WindowsVersion.Detect().IsWindows10;
            string ui = context.Workspace.Ui;
            string reg = Path.Combine(ui, "things.reg");

            if (File.Exists(reg))
            {
                context.BackupRegFile(reg);
                context.Processes.StartCaptured("regedit.exe /s \"" + reg + "\"");
            }

            context.Processes.StartCaptured(@"rd /s /q ""%userprofile%\3D Objects\""");

            if (!win10)
            {
                context.Processes.StartCaptured(
                    @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""UseCompactMode"" /t REG_DWORD /d ""00000001"" /f");
            }
            else
            {
                string taskmgr = Path.Combine(ui, "Taskmgr_win10_new.reg");
                if (File.Exists(taskmgr))
                {
                    context.BackupRegFile(taskmgr);
                    context.Processes.StartCaptured("regedit.exe /s \"" + taskmgr + "\"");
                }
            }

            context.Processes.StartCaptured("taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
        }

        public override void Rollback(TweakContext context)
        {
            bool win10 = context.WindowsVersion.Detect().IsWindows10;
            string ui = context.Workspace.Ui;
            string reg = Path.Combine(ui, "un_things.reg");

            if (File.Exists(reg))
            {
                context.Processes.StartCaptured("regedit.exe /s \"" + reg + "\"");
            }

            if (!win10)
            {
                context.Processes.StartCaptured(
                    @"reg add ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""UseCompactMode"" /t REG_DWORD /d ""00000000"" /f");
            }
            else
            {
                string taskmgr = Path.Combine(ui, "Taskmgr_win10_default.reg");
                if (File.Exists(taskmgr))
                {
                    context.Processes.StartCaptured("regedit.exe /s \"" + taskmgr + "\"");
                }
            }

            context.Processes.StartCaptured("taskkill /f /im explorer.exe & timeout /t 1 && explorer.exe");
        }
    }
}
