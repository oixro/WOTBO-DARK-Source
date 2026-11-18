using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Apps
{
    /// <summary>Установленный пакет Store/UWP.</summary>
    public sealed class AppPackage
    {
        public AppPackage(string name, string displayName, string fullName)
        {
            Name = name ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Name : displayName;
            FullName = fullName ?? string.Empty;
        }

        /// <summary>Техническое имя пакета (например, Microsoft.WindowsCalculator).</summary>
        public string Name { get; private set; }

        /// <summary>Имя из меню «Пуск»; если его нет — техническое.</summary>
        public string DisplayName { get; private set; }

        public string FullName { get; private set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    /// <summary>
    /// Список и удаление AppX-пакетов. Пакеты удаляются через
    /// Remove-AppxPackage, поэтому действие необратимо — предупреждает UI.
    /// </summary>
    public sealed class AppsService
    {
        private const string Category = "Apps";

        private static readonly Regex FullNamePattern = new Regex(
            @"^[A-Za-z0-9._\-]+_[0-9]+(\.[0-9]+){3}_[A-Za-z0-9_]+__[A-Za-z0-9_]+$",
            RegexOptions.CultureInvariant);

        private readonly IProcessService _processes;
        private readonly IAppLogger _log;

        public AppsService(IProcessService processes, IAppLogger log)
        {
            _processes = processes ?? throw new ArgumentNullException(nameof(processes));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<List<AppPackage>> GetAsync()
        {
            var result = new List<AppPackage>();
            string script = Path.Combine(Path.GetTempPath(), "wotbo-apps-list.ps1");

            File.WriteAllText(
                script,
                "$start = Get-StartApps\r\n"
                    + "Get-AppxPackage | Where-Object { -not $_.IsFramework -and -not $_.NonRemovable "
                    + "-and $_.Name -notmatch '^(Microsoft\\.(VCLibs|NET|UI\\.Xaml|WindowsAppRuntime|Services\\.Store))' "
                    + "-and $_.Name -notmatch '^(Microsoft\\.(WindowsStore|StorePurchaseApp|DesktopAppInstaller))$' } "
                    + "| Sort-Object Name | ForEach-Object {\r\n"
                    + "  $family = $_.PackageFamilyName\r\n"
                    + "  $friendly = ($start | Where-Object { $_.AppID -like ($family + '!*') } | Select-Object -First 1).Name\r\n"
                    + "  if (-not $friendly) { $friendly = $_.Name }\r\n"
                    + "  $friendly + [char]9 + $_.Name + [char]9 + $_.PackageFullName\r\n"
                    + "}",
                Encoding.UTF8);

            ProcessOutput output = await _processes.RunAsync(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"",
                30000,
                Encoding.UTF8);

            if (!output.Ok)
            {
                _log.Warn(Category, "Get-AppxPackage: код " + output.ExitCode + ", " + output.Text);
                return result;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in output.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                string[] parts = line.Split('\t');
                if (parts.Length < 3)
                {
                    continue;
                }

                string display = parts[0].Trim();
                string name = parts[1].Trim();
                string full = parts[2].Trim();
                if (name.Length == 0 || full.Length == 0 || !seen.Add(full))
                {
                    continue;
                }

                result.Add(new AppPackage(name, display, full));
            }

            _log.Info(Category, "найдено приложений: " + result.Count);
            return result;
        }

        public async Task<bool> RemoveAsync(AppPackage app)
        {
            if (app == null || !FullNamePattern.IsMatch(app.FullName))
            {
                _log.Warn(Category, "некорректное имя пакета: " + (app == null ? "<null>" : app.FullName));
                return false;
            }

            string escaped = app.FullName.Replace("'", "''");
            string command =
                "$ErrorActionPreference='Stop'; "
                + "Remove-AppxPackage -Package '" + escaped + "'";

            ProcessOutput output = await _processes.RunAsync(
                "powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"" + command + "\"",
                120000,
                Encoding.UTF8);

            if (!output.Ok)
            {
                _log.Warn(Category, "не удалось удалить " + app.Name + ": " + output.Text.Trim());
                return false;
            }

            _log.Info(Category, "удалено приложение: " + app.Name);
            return true;
        }
    }
}
