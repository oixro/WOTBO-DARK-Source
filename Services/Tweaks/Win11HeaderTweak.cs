using System;
using System.Threading;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Возврат старой шапки проводника Windows 11.</summary>
    public sealed class Win11HeaderTweak : TweakBase
    {
        private const string DefaultDll = @"C:\Windows\System32\FixByVlado";
        private const string OriginalDll = @"C:\Windows\System32\Windows.UI.FileExplorer.dll";

        public override string Id { get { return "ui.win11header"; } }
        public override string Category { get { return "UI"; } }

        public override TweakState Detect(TweakContext context)
        {
            object current = context.Registry.GetValue(
                RegistryHive.ClassesRoot,
                RegistryPaths.Win11HeaderClsid + @"\InProcServer32",
                string.Empty);

            return Convert.ToString(current).IndexOf("FixByVlado", StringComparison.OrdinalIgnoreCase) >= 0
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.BackupRegistryKey(RegistryHive.ClassesRoot, RegistryPaths.Win11HeaderClsid);
            context.Processes.StartCaptured("taskkill /f /im explorer.exe >nul 2>&1");
            context.Processes.RunSuper(RegAdd(DefaultDll), context.Workspace.SuExecutable);
            Thread.Sleep(1000);
            context.Processes.StartCaptured("explorer.exe");
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.StartCaptured("taskkill /f /im explorer.exe >nul 2>&1");
            context.Processes.RunSuper(RegAdd(OriginalDll), context.Workspace.SuExecutable);
            context.Processes.StartCaptured("explorer.exe");
        }

        private static string RegAdd(string dll)
        {
            return "reg add \"HKCR\\CLSID\\{6480100b-5a83-4d1e-9f69-8ae5a88e9a33}\\InProcServer32\" " +
                   "/v \"\" /t REG_SZ /d \"" + dll + "\" /f";
        }
    }
}
