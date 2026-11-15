using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;
using WOTBO.Services.Registry;
using WOTBO.Services.Windows;

namespace WOTBO.Services.Hardware
{
    public interface IPcReportService
    {
        /// <summary>Полный отчёт о ПК: железо и системные флаги.</summary>
        Task<PcReport> BuildAsync();
    }

    /// <summary>
    /// Собирает отчёт для вкладки «Инфо о ПК».
    ///
    /// Все запросы идут параллельно и из фонового потока: окно не ждёт.
    /// Каждая секция считается отдельно и падение одной не ломает остальные —
    /// вместо значения показывается «—».
    /// </summary>
    public sealed class PcReportService : IPcReportService
    {
        private const string Category = "PcReport";
        private const string StorageNamespace = @"root\Microsoft\Windows\Storage";
        private const string TpmNamespace = @"root\CIMV2\Security\MicrosoftTpm";
        private const string DisplayClassKey =
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
        private const string VersionKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        private const string Dash = "—";

        private readonly IRegistryService _registry;
        private readonly IProcessService _processes;
        private readonly IDisplayService _display;
        private readonly IWindowsVersionService _windows;
        private readonly IAppLogger _log;

        public PcReportService(
            IRegistryService registry,
            IProcessService processes,
            IDisplayService display,
            IWindowsVersionService windows,
            IAppLogger log)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _processes = processes ?? throw new ArgumentNullException(nameof(processes));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _windows = windows ?? throw new ArgumentNullException(nameof(windows));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public async Task<PcReport> BuildAsync()
        {
            Task<PcReportSection> cpu = Task.Run(() => Safe("процессор", CpuSection));
            Task<PcReportSection> memory = Task.Run(() => Safe("память", MemorySection));
            Task<PcReportSection> board = Task.Run(() => Safe("плата", BoardSection));
            Task<PcReportSection> video = Task.Run(() => Safe("видео", VideoSection));
            Task<PcReportSection> disks = Task.Run(() => Safe("накопители", DiskSection));
            Task<PcReportSection> system = Task.Run(() => Safe("система", SystemSection));

            await Task.WhenAll(cpu, memory, board, video, disks, system);

            return new PcReport(new[]
            {
                cpu.Result,
                memory.Result,
                board.Result,
                video.Result,
                disks.Result,
                system.Result,
            });
        }

        private PcReportSection Safe(string name, Func<PcReportSection> section)
        {
            try
            {
                return section();
            }
            catch (Exception exception)
            {
                _log.Error(Category, "секция «" + name + "»", exception);
                return new PcReportSection(name.ToUpperInvariant(), Dash);
            }
        }

        #region процессор
        private PcReportSection CpuSection()
        {
            string name = Dash;
            string details = null;
            string virtualization = Dash;

            foreach (ManagementObject obj in Query(
                "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, " +
                "L2CacheSize, L3CacheSize, VirtualizationFirmwareEnabled FROM Win32_Processor"))
            {
                name = Text(obj, "Name", Dash);

                int cores = Int(obj, "NumberOfCores");
                int threads = Int(obj, "NumberOfLogicalProcessors");
                double mhz = Number(obj, "MaxClockSpeed");
                int l2 = Int(obj, "L2CacheSize");
                int l3 = Int(obj, "L3CacheSize");

                var parts = new List<string>();
                if (cores > 0 && threads > 0)
                {
                    parts.Add(cores + " " + Plural(cores, "ядро", "ядра", "ядер")
                        + " / " + threads + " " + Plural(threads, "поток", "потока", "поток"));
                }
                else if (threads > 0)
                {
                    parts.Add(threads + " " + Plural(threads, "поток", "потока", "поток"));
                }

                if (mhz > 0)
                {
                    parts.Add(Ghz(mhz) + " GHz");
                }

                if (l2 > 0)
                {
                    parts.Add("L2 " + Megabytes(l2));
                }

                if (l3 > 0)
                {
                    parts.Add("L3 " + Megabytes(l3));
                }

                details = string.Join(" · ", parts);

                object enabled = obj["VirtualizationFirmwareEnabled"];
                if (enabled is bool)
                {
                    virtualization = (bool)enabled ? "включена" : "выключена";
                }

                break;
            }

            return new PcReportSection(
                "ПРОЦЕССОР",
                name,
                details,
                "Виртуализация: " + virtualization);
        }
        #endregion

        #region память
        private PcReportSection MemorySection()
        {
            var capacities = new List<ulong>();
            var speeds = new List<int>();
            int typeCode = 0;

            foreach (ManagementObject obj in Query(
                "SELECT Capacity, Speed, ConfiguredClockSpeed, SMBIOSMemoryType FROM Win32_PhysicalMemory"))
            {
                ulong capacity = (ulong)Number(obj, "Capacity");
                if (capacity > 0)
                {
                    capacities.Add(capacity);
                }

                int speed = Int(obj, "ConfiguredClockSpeed");
                if (speed <= 0)
                {
                    speed = Int(obj, "Speed");
                }

                if (speed > 0)
                {
                    speeds.Add(speed);
                }

                int type = Int(obj, "SMBIOSMemoryType");
                if (type > 0)
                {
                    typeCode = type;
                }
            }

            if (capacities.Count == 0)
            {
                return new PcReportSection("ПАМЯТЬ", Dash);
            }

            var parts = new List<string> { Gigabytes(capacities.Aggregate(0UL, (sum, item) => sum + item)) + " GB" };

            string typeName = MemoryTypeName(typeCode);
            if (!string.IsNullOrEmpty(typeName))
            {
                parts.Add(typeName);
            }

            if (speeds.Count > 0)
            {
                parts.Add(speeds.Max() + " MHz");
            }

            parts.Add(Modules(capacities));

            return new PcReportSection("ПАМЯТЬ", string.Join(" · ", parts), MemoryUsage());
        }

        /// <summary>«2×16 GB» вместо списка планок.</summary>
        private static string Modules(IEnumerable<ulong> capacities)
        {
            var groups = capacities
                .GroupBy(Gigabytes)
                .Select(group => group.Count() + "×" + group.Key + " GB")
                .ToList();

            int count = capacities.Count();
            if (count == 1)
            {
                return "1 планка";
            }

            return string.Join(" + ", groups);
        }

        /// <summary>Текущее потребление ОЗУ: занято/всего и процент.</summary>
        private static string MemoryUsage()
        {
            try
            {
                var status = new MemoryStatusEx
                {
                    Length = (uint)Marshal.SizeOf(typeof(MemoryStatusEx)),
                };

                if (!GlobalMemoryStatusEx(status))
                {
                    return null;
                }

                double total = status.TotalPhys / 1024.0 / 1024.0 / 1024.0;
                double used = (status.TotalPhys - status.AvailPhys) / 1024.0 / 1024.0 / 1024.0;

                return "Занято: " + used.ToString("0.0", CultureInfo.InvariantCulture)
                    + " GB из " + total.ToString("0.0", CultureInfo.InvariantCulture)
                    + " GB (" + status.MemoryLoad + " %)";
            }
            catch
            {
                return null;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private sealed class MemoryStatusEx
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhys;
            public ulong AvailPhys;
            public ulong TotalPageFile;
            public ulong AvailPageFile;
            public ulong TotalVirtual;
            public ulong AvailVirtual;
            public ulong AvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

        private static string MemoryTypeName(int code)
        {
            switch (code)
            {
                case 20: return "DDR";
                case 21: return "DDR2";
                case 24: return "DDR3";
                case 26: return "DDR4";
                case 34: return "DDR5";
                default: return null;
            }
        }
        #endregion

        #region плата, BIOS, прошивка
        private PcReportSection BoardSection()
        {
            string board = null;
            foreach (ManagementObject obj in Query("SELECT Manufacturer, Product FROM Win32_BaseBoard"))
            {
                board = JoinVendor(
                    Text(obj, "Manufacturer", string.Empty),
                    Text(obj, "Product", string.Empty));
                break;
            }

            string bios = null;
            string biosDate = null;
            foreach (ManagementObject obj in Query("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS"))
            {
                bios = Text(obj, "SMBIOSBIOSVersion", null);
                biosDate = Date(obj, "ReleaseDate");
                break;
            }

            string first = string.Join(
                " · ",
                new[] { board, bios == null ? null : "BIOS " + bios + (biosDate == null ? string.Empty : " (" + biosDate + ")") }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));

            var flags = new List<string> { FirmwareName(), "Secure Boot: " + SecureBoot() };

            string tpm = TpmVersion();
            if (!string.IsNullOrEmpty(tpm))
            {
                flags.Add("TPM " + tpm);
            }

            return new PcReportSection(
                "МАТЕРИНСКАЯ ПЛАТА / BIOS",
                string.IsNullOrWhiteSpace(first) ? Dash : first,
                string.Join(" · ", flags));
        }

        private static string FirmwareName()
        {
            try
            {
                int type;
                if (!GetFirmwareType(out type))
                {
                    return Dash;
                }

                if (type == 2)
                {
                    return "UEFI";
                }

                if (type == 1)
                {
                    return "Legacy BIOS";
                }
            }
            catch
            {
            }

            return Dash;
        }

        private string SecureBoot()
        {
            object value = _registry.GetValue(
                RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\SecureBoot\State", "UEFISecureBootEnabled");

            if (value == null)
            {
                return Dash;
            }

            return ToInt(value) == 1 ? "включён" : "выключен";
        }

        private string TpmVersion()
        {
            try
            {
                foreach (ManagementObject obj in Query(
                    TpmNamespace, "SELECT SpecVersion FROM Win32_Tpm"))
                {
                    string spec = Text(obj, "SpecVersion", null);
                    if (string.IsNullOrEmpty(spec))
                    {
                        continue;
                    }

                    string version = spec.Split(',')[0].Trim();
                    return string.IsNullOrEmpty(version) ? null : version;
                }
            }
            catch (Exception exception)
            {
                _log.Debug(Category, "TPM: " + exception.Message);
            }

            return null;
        }
        #endregion

        #region видео
        private PcReportSection VideoSection()
        {
            var lines = new List<string>();

            foreach (ManagementObject obj in Query(
                "SELECT Name, DriverVersion, DriverDate FROM Win32_VideoController"))
            {
                string name = Text(obj, "Name", Dash);
                var parts = new List<string> { name };

                ulong vram = Vram(name);
                if (vram > 0)
                {
                    parts.Add(Gigabytes(vram) + " GB");
                }

                string driver = Text(obj, "DriverVersion", null);
                if (!string.IsNullOrEmpty(driver))
                {
                    string date = Date(obj, "DriverDate");
                    parts.Add("драйвер " + driver + (date == null ? string.Empty : " (" + date + ")"));
                }

                lines.Add(string.Join(" · ", parts));
            }

            string screen = Screen();
            if (!string.IsNullOrEmpty(screen))
            {
                lines.Add("Экран: " + screen);
            }

            return new PcReportSection("ВИДЕО", lines.ToArray());
        }

        /// <summary>
        /// AdapterRAM из WMI врёт на картах больше 4 GB (uint32), поэтому
        /// объём читаем из ветки класса дисплея.
        /// </summary>
        private ulong Vram(string adapterName)
        {
            ulong best = 0;

            foreach (string subKey in _registry.GetSubKeyNames(RegistryHive.LocalMachine, DisplayClassKey))
            {
                if (!Regex.IsMatch(subKey, @"^\d{4}$"))
                {
                    continue;
                }

                string path = DisplayClassKey + "\\" + subKey;
                object size = _registry.GetValue(RegistryHive.LocalMachine, path, "HardwareInformation.qwMemorySize");
                if (size == null)
                {
                    continue;
                }

                ulong bytes;
                try
                {
                    bytes = Convert.ToUInt64(size);
                }
                catch
                {
                    continue;
                }

                if (bytes == 0)
                {
                    continue;
                }

                object description = _registry.GetValue(RegistryHive.LocalMachine, path, "DriverDesc");
                if (description != null
                    && string.Equals(Convert.ToString(description).Trim(), adapterName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return bytes;
                }

                best = Math.Max(best, bytes);
            }

            return best;
        }

        private string Screen()
        {
            string info = (_display.GetScreenInfo() ?? string.Empty).Trim();
            if (info.Length == 0)
            {
                return null;
            }

            Match match = Regex.Match(info, @"(\d+)x(\d+)\s+(\d+)\s*Hz", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value + "×" + match.Groups[2].Value + " @ " + match.Groups[3].Value + " Hz";
            }

            return info;
        }
        #endregion

        #region накопители
        private PcReportSection DiskSection()
        {
            var lines = new List<string>();

            try
            {
                foreach (ManagementObject obj in Query(
                    StorageNamespace, "SELECT FriendlyName, MediaType, BusType, Size FROM MSFT_PhysicalDisk"))
                {
                    string name = Text(obj, "FriendlyName", Dash);
                    string type = DiskType(Int(obj, "MediaType"), Int(obj, "BusType"));
                    ulong size = (ulong)Number(obj, "Size");

                    lines.Add(name + (type == null ? string.Empty : " (" + type + ")")
                        + (size > 0 ? " · " + Gigabytes(size) + " GB" : string.Empty));
                }
            }
            catch (Exception exception)
            {
                _log.Debug(Category, "MSFT_PhysicalDisk: " + exception.Message);
            }

            if (lines.Count == 0)
            {
                // Старые системы без Storage-провайдера.
                foreach (ManagementObject obj in Query("SELECT Model, Size, InterfaceType FROM Win32_DiskDrive"))
                {
                    ulong size = (ulong)Number(obj, "Size");
                    string type = Text(obj, "InterfaceType", null);
                    lines.Add(Text(obj, "Model", Dash)
                        + (string.IsNullOrEmpty(type) ? string.Empty : " (" + type + ")")
                        + (size > 0 ? " · " + Gigabytes(size) + " GB" : string.Empty));
                }
            }

            string systemDisk = SystemDiskLine();
            if (!string.IsNullOrEmpty(systemDisk))
            {
                lines.Add(systemDisk);
            }

            return new PcReportSection("НАКОПИТЕЛИ", lines.ToArray());
        }

        private static string DiskType(int mediaType, int busType)
        {
            if (busType == 17)
            {
                return "NVMe";
            }

            switch (mediaType)
            {
                case 3: return "HDD";
                case 4: return "SSD";
                case 5: return "SCM";
                default: return null;
            }
        }

        private static string SystemDiskLine()
        {
            string drive = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (drive.Length < 2)
            {
                return null;
            }

            drive = drive.Substring(0, 2);

            foreach (ManagementObject obj in Query(
                "SELECT Size, FreeSpace FROM Win32_LogicalDisk WHERE DeviceID='" + drive.Replace("\\", "\\\\") + "'"))
            {
                ulong total = (ulong)Number(obj, "Size");
                ulong free = (ulong)Number(obj, "FreeSpace");

                if (total == 0)
                {
                    return null;
                }

                return drive + " свободно " + Gigabytes(free) + " GB из " + Gigabytes(total) + " GB";
            }

            return null;
        }
        #endregion

        #region система
        private PcReportSection SystemSection()
        {
            WindowsVersionInfo version = _windows.Detect();

            string product = Convert.ToString(
                _registry.GetValue(RegistryHive.LocalMachine, VersionKey, "ProductName")) ?? "Windows";

            // В реестре Win11 до сих пор лежит «Windows 10 ...».
            if (version.IsWindows11 && product.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
            {
                product = "Windows 11" + product.Substring("Windows 10".Length);
            }
            string ubr = Convert.ToString(_registry.GetValue(RegistryHive.LocalMachine, VersionKey, "UBR"));

            string build = version.Build;
            if (!string.IsNullOrEmpty(ubr))
            {
                build = build + "." + ubr;
            }

            string line = product.Trim();
            if (!string.IsNullOrEmpty(version.DisplayVersion))
            {
                line += " " + version.DisplayVersion;
            }

            if (!string.IsNullOrEmpty(build))
            {
                line += " (Build " + build + ")";
            }

            if (version.InstallDate.HasValue)
            {
                line += " · установлена " + version.InstallDate.Value.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
            }

            var lines = new List<string> { line };

            string power = ActivePowerScheme();
            if (!string.IsNullOrEmpty(power))
            {
                lines.Add("Схема питания: " + power);
            }

            string pageFile = PageFile();
            string uptime = Uptime();
            if (pageFile != null || uptime != null)
            {
                lines.Add(string.Join(
                    " · ",
                    new[]
                    {
                        pageFile == null ? null : "Файл подкачки: " + pageFile,
                        uptime == null ? null : "аптайм " + uptime,
                    }.Where(part => part != null)));
            }

            lines.Add("HVCI/VBS: " + Guard() + " · Hyper-V: " + Hypervisor());

            return new PcReportSection("СИСТЕМА", lines.ToArray());
        }

        private string ActivePowerScheme()
        {
            try
            {
                string output = _processes
                    .ReadOutputAsync("powercfg.exe", "-getactivescheme")
                    .GetAwaiter()
                    .GetResult();

                // «Power Scheme GUID: <guid>  ((U)_Shingeki_no_Windows_8.0)»
                Match match = Regex.Match(
                    output ?? string.Empty,
                    @"GUID:\s*[0-9a-fA-F-]{36}\s*(.+)$",
                    RegexOptions.Multiline);

                if (match.Success)
                {
                    string name = match.Groups[1].Value.Trim();
                    if (name.StartsWith("(") && name.EndsWith(")") && name.Length > 2)
                    {
                        name = name.Substring(1, name.Length - 2).Trim();
                    }

                    return name.Length == 0 ? null : name;
                }
            }
            catch (Exception exception)
            {
                _log.Debug(Category, "powercfg: " + exception.Message);
            }

            return null;
        }

        private static string PageFile()
        {
            foreach (ManagementObject obj in Query(
                "SELECT AllocatedBaseSize, Name FROM Win32_PageFileUsage"))
            {
                int megabytes = Int(obj, "AllocatedBaseSize");
                if (megabytes <= 0)
                {
                    continue;
                }

                string name = Text(obj, "Name", null);
                string location = string.IsNullOrEmpty(name) ? string.Empty : " (" + name + ")";

                string size = megabytes < 1024
                    ? megabytes + " MB"
                    : Math.Round(megabytes / 1024.0, 1).ToString("0.#", CultureInfo.InvariantCulture) + " GB";

                return size + location;
            }

            return null;
        }

        private static string Uptime()
        {
            TimeSpan span;
            try
            {
                span = TimeSpan.FromMilliseconds(GetTickCount64());
            }
            catch
            {
                return null;
            }

            if (span.TotalMinutes < 1)
            {
                return "меньше минуты";
            }

            if (span.TotalHours < 1)
            {
                return (int)span.TotalMinutes + " мин";
            }

            if (span.TotalDays < 1)
            {
                return (int)span.TotalHours + " ч " + span.Minutes + " мин";
            }

            return (int)span.TotalDays + " дн " + span.Hours + " ч";
        }

        private string Guard()
        {
            object hvci = _registry.GetValue(
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity",
                "Enabled");

            object vbs = _registry.GetValue(
                RegistryHive.LocalMachine,
                @"SYSTEM\CurrentControlSet\Control\DeviceGuard",
                "EnableVirtualizationBasedSecurity");

            bool on = ToInt(hvci) == 1 || ToInt(vbs) == 1;
            return on ? "включены" : "выключены";
        }

        private static string Hypervisor()
        {
            foreach (ManagementObject obj in Query(
                "SELECT HypervisorPresent FROM Win32_ComputerSystem"))
            {
                object value = obj["HypervisorPresent"];
                if (value is bool)
                {
                    return (bool)value ? "включён" : "выключен";
                }

                break;
            }

            return Dash;
        }
        #endregion

        #region WMI и форматирование
        private static IEnumerable<ManagementObject> Query(string query)
        {
            return Query(null, query);
        }

        private static IEnumerable<ManagementObject> Query(string scope, string query)
        {
            var queryObject = new ObjectQuery(query);
            ManagementObjectSearcher searcher = scope == null
                ? new ManagementObjectSearcher(queryObject)
                : new ManagementObjectSearcher(new ManagementScope(@"\\.\" + scope), queryObject);

            using (searcher)
            {
                foreach (ManagementBaseObject item in searcher.Get())
                {
                    yield return (ManagementObject)item;
                }
            }
        }

        private static string Text(ManagementObject obj, string property, string fallback)
        {
            object value = obj[property];
            if (value == null)
            {
                return fallback;
            }

            string text = Convert.ToString(value).Trim();
            return text.Length == 0 ? fallback : text;
        }

        private static int Int(ManagementObject obj, string property)
        {
            object value = obj[property];
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static double Number(ManagementObject obj, string property)
        {
            object value = obj[property];
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToDouble(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static string Date(ManagementObject obj, string property)
        {
            object value = obj[property];
            if (value == null)
            {
                return null;
            }

            try
            {
                DateTime date = ManagementDateTimeConverter.ToDateTime(Convert.ToString(value));
                return date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        private static int ToInt(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        private static string Plural(int value, string one, string few, string many)
        {
            int lastTwo = value % 100;
            if (lastTwo >= 11 && lastTwo <= 14)
            {
                return many;
            }

            switch (value % 10)
            {
                case 1: return one;
                case 2:
                case 3:
                case 4: return few;
                default: return many;
            }
        }

        /// <summary>«Parallels ARM Virtual Machine» + «Parallels ARM Virtual Platform» → без повтора.</summary>
        private static string JoinVendor(string manufacturer, string product)
        {
            manufacturer = (manufacturer ?? string.Empty).Trim();
            product = (product ?? string.Empty).Trim();

            if (product.Length == 0)
            {
                return manufacturer;
            }

            if (manufacturer.Length == 0)
            {
                return product;
            }

            string vendor = manufacturer.Split(' ')[0];
            if (product.StartsWith(vendor, StringComparison.OrdinalIgnoreCase))
            {
                return product;
            }

            return manufacturer + " " + product;
        }

        private static string Ghz(double megahertz)
        {
            return Math.Round(megahertz / 1000.0, 1).ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static string Gigabytes(ulong bytes)
        {
            double gigabytes = bytes / 1024.0 / 1024.0 / 1024.0;
            return Math.Round(gigabytes).ToString("0", CultureInfo.InvariantCulture);
        }

        private static string Megabytes(int kilobytes)
        {
            double megabytes = kilobytes / 1024.0;
            if (megabytes >= 1024)
            {
                return Math.Round(megabytes / 1024.0, 1).ToString("0.#", CultureInfo.InvariantCulture) + " MB";
            }

            return Math.Round(megabytes).ToString("0", CultureInfo.InvariantCulture) + " MB";
        }

        [DllImport("kernel32.dll")]
        private static extern bool GetFirmwareType(out int firmwareType);

        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();
        #endregion
    }
}
