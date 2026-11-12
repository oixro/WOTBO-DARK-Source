using System;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Скрытие Home/Gallery в проводнике Windows 11.</summary>
    public sealed class HomeGalleryTweak : TweakBase
    {
        public override string Id { get { return "pro.homegallery"; } }
        public override string Category { get { return "Pro"; } }

        public override TweakState Detect(TweakContext context)
        {
            if (context.WindowsVersion.Detect().IsWindows10)
            {
                return TweakState.NotApplied;
            }

            object hub = context.Registry.GetValue(RegistryHive.LocalMachine, RegistryPaths.Explorer, "HubMode");
            object home = context.Registry.GetValue(
                RegistryHive.ClassesRoot,
                RegistryPaths.HomeGalleryClsid,
                "System.IsPinnedToNameSpaceTree");
            object gallery = context.Registry.GetValue(
                RegistryHive.CurrentUser,
                RegistryPaths.GalleryClsid,
                "System.IsPinnedToNameSpaceTree");

            return Convert.ToInt32(hub ?? 0) == 1
                && Convert.ToInt32(home ?? -1) == 0
                && Convert.ToInt32(gallery ?? -1) == 1
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.LocalMachine, RegistryPaths.Explorer);
            context.BackupRegistryKey(RegistryHive.ClassesRoot, RegistryPaths.HomeGalleryClsid);
            context.BackupRegistryKey(RegistryHive.CurrentUser, RegistryPaths.GalleryClsid);

            context.Processes.StartCaptured(
                @"reg add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v ""HubMode"" /t REG_DWORD /d ""00000001"" /f >nul 2>&1");
            context.Processes.RunSuper(
                @"cmd.exe /c reg add ""HKCR\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}"" /v ""System.IsPinnedToNameSpaceTree"" /t REG_DWORD /d ""00000000"" /f",
                context.Workspace.SuExecutable);
            context.Processes.StartCaptured(
                "reg add \"HKEY_CURRENT_USER\\Software\\Classes\\CLSID\\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}\" /v \"System.IsPinnedToNameSpaceTree\" /t REG_DWORD /d \"00000000\" /f >nul 2>&1");
            context.Processes.RunSuper(
                @"cmd.exe /c reg add ""HKEY_CURRENT_USER\Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}"" /v ""System.IsPinnedToNameSpaceTree"" /t REG_DWORD /d ""00000001"" /f >nul 2>&1",
                context.Workspace.SuExecutable);
            context.Processes.StartCaptured("taskkill /f /im explorer.exe && timeout /t 1 && explorer.exe");
        }

        public override void Rollback(TweakContext context)
        {
            // Восстанавливается через TweakScope из снимков веток.
        }
    }
}
