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
        public AppPackage(string name, string fullName)
        {
            Name = name ?? string.Empty;
            FullName = fullName ?? string.Empty;
        }

        public string Name { get; private set; }

        public string FullName { get; private set; }

        public override string ToString()
        {
            return Name;
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
                "Get-AppxPackage | Where-Object { -not $_.IsFramework -and -not $_.NonRemovable "
                    + "-and $_.Name -notmatch '^(Microsoft\\.(VCLibs|NET|UI\\.Xaml|WindowsAppRuntime|Services\\.Store))' } "
                    + "| Sort-Object Name | ForEach-Object { $_.Name + '|' + $_.PackageFullName }",
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

                int split = line.IndexOf('|');
                if (split <= 0 || split + 1 >= line.Length)
                {
                    continue;
                }

                string name = line.Substring(0, split).Trim();
                string full = line.Substring(split + 1).Trim();
                if (name.Length == 0 || full.Length == 0 || !seen.Add(full))
                {
                    continue;
                }

                result.Add(new AppPackage(name, full));
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
