using System;
using System.Globalization;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Windows
{
    /// <summary>Результат определения версии Windows.</summary>
    public sealed class WindowsVersionInfo
    {
        public string Build { get; set; }

        public string DisplayVersion { get; set; }

        /// <summary>Дата установки Windows (локальное время) или <c>null</c>.</summary>
        public DateTime? InstallDate { get; set; }

        public bool IsWindows11 { get; set; }

        public bool IsWindows10 { get; set; }

        /// <summary>Build удалось разобрать в число.</summary>
        public bool IsBuildValid { get; set; }

        /// <summary>Windows 10 старше 20H2 — показываем предупреждение.</summary>
        public bool IsOldWindows10 { get; set; }

        public bool IsKnown
        {
            get { return !string.IsNullOrEmpty(Build) && !string.IsNullOrEmpty(DisplayVersion); }
        }
    }

    public interface IWindowsVersionService
    {
        WindowsVersionInfo Detect();
    }

    /// <summary>
    /// Читает <c>HKLM\Software\Microsoft\Windows NT\CurrentVersion</c>.
    /// Никаких MessageBox — решение принимает UI.
    /// </summary>
    public sealed class WindowsVersionService : IWindowsVersionService
    {
        private const string Path = RegistryPaths.WindowsVersion;

        private readonly IRegistryService _registry;

        public WindowsVersionService(IRegistryService registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public WindowsVersionInfo Detect()
        {
            string build = _registry.GetValue(RegistryHive.LocalMachine, Path, "CurrentBuild") as string;
            string displayVersion = _registry.GetValue(RegistryHive.LocalMachine, Path, "DisplayVersion") as string;

            var info = new WindowsVersionInfo
            {
                Build = build,
                DisplayVersion = displayVersion,
            };

            object installDate = _registry.GetValue(RegistryHive.LocalMachine, Path, "InstallDate");
            if (installDate != null)
            {
                long seconds;
                if (long.TryParse(Convert.ToString(installDate, CultureInfo.InvariantCulture),
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds) && seconds > 0)
                {
                    info.InstallDate = DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
                }
            }

            if (!info.IsKnown)
            {
                return info;
            }

            ushort buildNumber;
            if (!ushort.TryParse(build, NumberStyles.Integer, CultureInfo.InvariantCulture, out buildNumber))
            {
                return info;
            }

            info.IsBuildValid = true;

            if (buildNumber >= 22000)
            {
                info.IsWindows11 = true;
            }
            else if (buildNumber <= 19042)
            {
                info.IsWindows10 = true;
                info.IsOldWindows10 = true;
            }
            else
            {
                info.IsWindows10 = true;
            }

            return info;
        }
    }
}
