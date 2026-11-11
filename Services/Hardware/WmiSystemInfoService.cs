using System;
using System.Management;
using WOTBO.App.Logging;

namespace WOTBO.Services.Hardware
{
    public interface IWmiSystemInfoService
    {
        string GetString(string className, string propertyName);

        double GetDouble(string className, string propertyName);

        string GetRamSize();

        string GetRamFrequency();
    }

    /// <summary>
    /// WMI-запросы из прежних <c>GetStringFromWMI/GetDoubleFromWMI</c>.
    /// Ошибки больше не глотаются молча — пишутся в лог на уровне DEBUG.
    /// </summary>
    public sealed class WmiSystemInfoService : IWmiSystemInfoService
    {
        private const string Category = "Hardware";

        private readonly IAppLogger _log;

        public WmiSystemInfoService(IAppLogger log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public string GetString(string className, string propertyName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM {className}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return obj[propertyName]?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug(Category, $"WMI {className}.{propertyName}: {ex.Message}");
            }

            return string.Empty;
        }

        public double GetDouble(string className, string propertyName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM {className}"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return Convert.ToDouble(obj[propertyName]);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug(Category, $"WMI {className}.{propertyName}: {ex.Message}");
            }

            return -1.0;
        }

        public string GetRamSize()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        ulong totalMemory = (ulong)obj["TotalPhysicalMemory"];
                        return $"{Math.Round((double)totalMemory / (1024 * 1024 * 1024), 0)} GB";
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug(Category, "WMI TotalPhysicalMemory: " + ex.Message);
            }

            return string.Empty;
        }

        public string GetRamFrequency()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        uint speed = (uint)obj["Speed"];
                        if (speed > 0)
                        {
                            return $"{speed} MHz";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug(Category, "WMI PhysicalMemory.Speed: " + ex.Message);
            }

            return string.Empty;
        }
    }
}
