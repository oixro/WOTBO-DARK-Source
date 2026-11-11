using System;
using System.Management;
using System.Threading.Tasks;
using WOTBO.App.Logging;

namespace WOTBO.Services.Hardware
{
    /// <summary>Снимок основных комплектующих, полученный параллельно.</summary>
    public sealed class SystemInfoSnapshot
    {
        public string CpuModel { get; set; }

        public double CpuFrequencyGhz { get; set; }

        public string Motherboard { get; set; }

        public string Gpu { get; set; }

        public string RamSize { get; set; }

        public string RamFrequency { get; set; }
    }

    public interface IWmiSystemInfoService
    {
        string GetString(string className, string propertyName);

        double GetDouble(string className, string propertyName);

        string GetRamSize();

        string GetRamFrequency();

        /// <summary>
        /// Один параллельный снимок: CPU, плата, GPU, RAM. Вместо 6
        /// последовательных WMI-запросов на старте.
        /// </summary>
        Task<SystemInfoSnapshot> GetSnapshotAsync();
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
                using (var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}"))
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
                using (var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}"))
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
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object memoryValue = obj["TotalPhysicalMemory"];
                        if (memoryValue == null)
                        {
                            continue;
                        }

                        ulong totalMemory = Convert.ToUInt64(memoryValue);
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
                using (var searcher = new ManagementObjectSearcher("SELECT Speed FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object speedValue = obj["Speed"];
                        if (speedValue == null)
                        {
                            continue;
                        }

                        uint speed = Convert.ToUInt32(speedValue);
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

        public async Task<SystemInfoSnapshot> GetSnapshotAsync()
        {
            Task<ProcessorInfo> cpu = Task.Run(() => QueryProcessor());
            Task<string> gpu = Task.Run(() => GetString("Win32_VideoController", "Name"));
            Task<string> motherboard = Task.Run(() => GetString("Win32_BaseBoard", "Product"));
            Task<string> ramSize = Task.Run(() => GetRamSize());
            Task<string> ramFrequency = Task.Run(() => GetRamFrequency());

            await Task.WhenAll(cpu, gpu, motherboard, ramSize, ramFrequency);

            return new SystemInfoSnapshot
            {
                CpuModel = cpu.Result.Name,
                CpuFrequencyGhz = cpu.Result.MaxClockSpeed / 1000.0,
                Gpu = gpu.Result,
                Motherboard = motherboard.Result,
                RamSize = ramSize.Result,
                RamFrequency = ramFrequency.Result,
            };
        }

        private ProcessorInfo QueryProcessor()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, MaxClockSpeed FROM Win32_Processor"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        return new ProcessorInfo
                        {
                            Name = obj["Name"]?.ToString() ?? string.Empty,
                            MaxClockSpeed = obj["MaxClockSpeed"] == null
                                ? -1000.0
                                : Convert.ToDouble(obj["MaxClockSpeed"]),
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug(Category, "WMI Win32_Processor: " + ex.Message);
            }

            return new ProcessorInfo { Name = string.Empty, MaxClockSpeed = -1000.0 };
        }

        private sealed class ProcessorInfo
        {
            public string Name { get; set; }

            public double MaxClockSpeed { get; set; }
        }
    }
}
