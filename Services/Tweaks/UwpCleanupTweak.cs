using System;
using System.IO;
using System.Linq;
using System.Text;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Удаляет лишние UWP-приложения (включая Clipchamp).
    ///
    /// Отката нет: приложения можно поставить обратно только из Microsoft Store,
    /// и сам Store твик не трогает. Detect считает твик применённым, когда ни
    /// одного пакета из списка в системе не осталось.
    /// </summary>
    public sealed class UwpCleanupTweak : TweakBase
    {
        private const string CategoryName = "Extra";
        private const string LogName = "Tweak:extra.uwpcleanup";

        /// <summary>
        /// Пакеты, которые удаляем. Store, winget, Defender UI, «Параметры»,
        /// зависимости (VCLibs, UI.Xaml, .NET Native) и системные приложения
        /// (Notepad, Paint, Calculator, Photos, Camera, ScreenSketch) не входят.
        /// </summary>
        private static readonly string[] Packages =
        {
            "Clipchamp.Clipchamp",
            "Microsoft.549981C3F5F10",
            "Microsoft.BingNews",
            "Microsoft.BingWeather",
            "Microsoft.Copilot",
            "Microsoft.GetHelp",
            "Microsoft.Getstarted",
            "Microsoft.Microsoft3DViewer",
            "Microsoft.MicrosoftOfficeHub",
            "Microsoft.MicrosoftSolitaireCollection",
            "Microsoft.MicrosoftStickyNotes",
            "Microsoft.MixedReality.Portal",
            "Microsoft.NetworkSpeedTest",
            "Microsoft.OneConnect",
            "Microsoft.OutlookForWindows",
            "Microsoft.People",
            "Microsoft.PowerAutomateDesktop",
            "Microsoft.Print3D",
            "Microsoft.SkypeApp",
            "Microsoft.Teams",
            "Microsoft.TeamsApp",
            "MSTeams",
            "Microsoft.Todos",
            "Microsoft.Wallet",
            "Microsoft.Windows.DevHome",
            "Microsoft.WindowsAlarms",
            "Microsoft.WindowsCommunicationsApps",
            "Microsoft.WindowsFeedbackHub",
            "Microsoft.WindowsMaps",
            "Microsoft.WindowsSoundRecorder",
            "Microsoft.YourPhone",
            "Microsoft.ZuneMusic",
            "Microsoft.ZuneVideo",
            "MicrosoftCorporationII.QuickAssist",
            "MicrosoftTeams",
        };

        public override string Id
        {
            get { return "extra.uwpcleanup"; }
        }

        public override string Category
        {
            get { return CategoryName; }
        }

        public override TweakState Detect(TweakContext context)
        {
            string output = Run(context, BuildDetectScript());

            // «NONE» — ни одного пакета из списка не осталось.
            if (output.IndexOf("NONE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return TweakState.Applied;
            }

            return TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string path = Path.Combine(context.Workspace.Root, "uwp-cleanup.ps1");

            try
            {
                Directory.CreateDirectory(context.Workspace.Root);
                File.WriteAllText(path, BuildRemovalScript(), new UTF8Encoding(false));
            }
            catch (Exception exception)
            {
                context.Log.Error(LogName, "не удалось записать скрипт удаления", exception);
                return;
            }

            string output = context.Processes
                .ReadOutputAsync(
                    "powershell.exe",
                    "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + path + "\"")
                .GetAwaiter()
                .GetResult();

            if (!string.IsNullOrWhiteSpace(output))
            {
                context.Log.Info(LogName, output.Trim());
            }
        }

        public override void Rollback(TweakContext context)
        {
            // Отката нет: удалённые приложения ставятся только из Microsoft Store.
            context.Log.Info(LogName, "откат невозможен: приложения восстанавливаются из Microsoft Store");
        }

        private static string Run(TweakContext context, string script)
        {
            try
            {
                string path = Path.Combine(context.Workspace.Root, "uwp-detect.ps1");
                Directory.CreateDirectory(context.Workspace.Root);
                File.WriteAllText(path, script, new UTF8Encoding(false));

                return context.Processes
                    .ReadOutputAsync(
                        "powershell.exe",
                        "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + path + "\"")
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception exception)
            {
                context.Log.Debug(LogName, "detect: " + exception.Message);
                return string.Empty;
            }
        }

        private static string BuildDetectScript()
        {
            return "$ErrorActionPreference = 'SilentlyContinue'\r\n" +
                   "$list = @(" + Names() + ")\r\n" +
                   "$found = Get-AppxPackage | Where-Object { $list -contains $_.Name } | " +
                   "Select-Object -ExpandProperty Name -Unique\r\n" +
                   "if (-not $found) { 'NONE' } else { $found }\r\n";
        }

        private static string BuildRemovalScript()
        {
            return "$ErrorActionPreference = 'SilentlyContinue'\r\n" +
                   "$list = @(" + Names() + ")\r\n" +
                   "$all = Get-AppxPackage -AllUsers\r\n" +
                   "foreach ($name in $list) {\r\n" +
                   "  foreach ($p in ($all | Where-Object { $_.Name -eq $name })) {\r\n" +
                   "    Remove-AppxPackage -Package $p.PackageFullName -AllUsers -ErrorAction SilentlyContinue\r\n" +
                   "  }\r\n" +
                   "}\r\n" +
                   "$prov = Get-AppxProvisionedPackage -Online\r\n" +
                   "foreach ($name in $list) {\r\n" +
                   "  foreach ($d in ($prov | Where-Object { $_.DisplayName -eq $name })) {\r\n" +
                   "    Remove-AppxProvisionedPackage -Online -PackageName $d.PackageName -ErrorAction SilentlyContinue | Out-Null\r\n" +
                   "  }\r\n" +
                   "}\r\n" +
                   "'DONE'\r\n";
        }

        private static string Names()
        {
            return string.Join(", ", Packages.Select(name => "'" + name + "'").ToArray());
        }
    }
}
