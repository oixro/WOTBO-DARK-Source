using System;
using System.Management;
using WOTBO.App.Logging;

namespace WOTBO.Services.Hardware
{
    public interface IWmiSystemInfoService
    {
        string GetString(string className, string propertyName);
    }

    /// <summary>
    /// Простые WMI-запросы к железу.
    ///
    /// Подробный отчёт для вкладки «Инфо о ПК» собирает
    /// <see cref="PcReportService"/>; здесь осталось только то, что нужно
    /// другим сервисам (имя видеоадаптера для <see cref="GpuService"/>).
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
    }
}
