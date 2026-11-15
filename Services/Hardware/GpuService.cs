using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Hardware;
using WOTBO.Services.Processes;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Hardware
{
    public interface IGpuService
    {
        string GetName();

        bool IsNvidia(string name);

        bool IsHdcpDisabled();

        bool IsNvidiaTweaksApplied();

        /// <summary><c>true</c> — Ansel выключен, <c>false</c> — включён, <c>null</c> — утилита не найдена.</summary>
        System.Threading.Tasks.Task<bool?> IsAnselDisabledAsync();

        /// <summary>Включить/выключить Ansel. <c>false</c>, если утилита не найдена.</summary>
        bool SetAnsel(bool enabled);
    }

    public sealed class GpuService : IGpuService
    {
        private const string Category = "GPU";
        private const string HdcpPath = RegistryPaths.HdcpPath;
        private const string NvidiaTweaksPath = RegistryPaths.NvidiaTweaksPath;

        /// <summary>Сколько ищем NvCameraEnable.exe в DriverStore.</summary>
        private const int SearchBudgetMs = 3000;

        /// <summary>Сколько ждём ответа самого NvCameraEnable.exe.</summary>
        private const int ToolTimeoutMs = 5000;

        private static readonly string[] DirectPaths =
        {
            @"C:\Program Files\NVIDIA Corporation\NVIDIA Ansel\NvCameraEnable.exe",
            @"C:\Program Files\NVIDIA Corporation\Ansel\NvCameraEnable.exe",
            @"C:\Program Files\NVIDIA Corporation\NvCamera\NvCameraEnable.exe",
        };

        private readonly IWmiSystemInfoService _wmi;
        private readonly IRegistryService _registry;
        private readonly IProcessService _process;
        private readonly IAppLogger _log;

        public GpuService(
            IWmiSystemInfoService wmi,
            IRegistryService registry,
            IProcessService process,
            IAppLogger log)
        {
            _wmi = wmi ?? throw new ArgumentNullException(nameof(wmi));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public string GetName()
        {
            return _wmi.GetString("Win32_VideoController", "Name");
        }

        public bool IsNvidia(string name)
        {
            return !string.IsNullOrEmpty(name)
                && name.IndexOf("NVIDIA", StringComparison.Ordinal) >= 0;
        }

        public bool IsHdcpDisabled()
        {
            return _registry.ValueExists(RegistryHive.LocalMachine, HdcpPath, "RMHdcpKeyglobZero");
        }

        public bool IsNvidiaTweaksApplied()
        {
            return _registry.ValueExists(RegistryHive.LocalMachine, NvidiaTweaksPath, "DisplayPowerSaving");
        }

        public async System.Threading.Tasks.Task<bool?> IsAnselDisabledAsync()
        {
            string tool = FindAnselTool();
            if (string.IsNullOrEmpty(tool))
            {
                return null;
            }

            // ConfigureAwait(false) — чтобы не зависеть от UI-контекста:
            // раньше Detect вызывал этот метод через GetResult() в UI-потоке
            // и на этом самом await получался дедлок.
            string state = (await _process.ReadOutputAsync(tool, string.Empty, ToolTimeoutMs).ConfigureAwait(false)).Trim();
            return state == "0";
        }

        public bool SetAnsel(bool enabled)
        {
            string tool = FindAnselTool();
            if (string.IsNullOrEmpty(tool))
            {
                return false;
            }

            // Сохраняем прежнюю fire-and-forget семантику hcmd.
            _process.StartCaptured($"\"{tool}\" {(enabled ? "on" : "off")}");
            return true;
        }

        /// <summary>
        /// Ищет NvCameraEnable.exe. Раньше это был рекурсивный обход всего
        /// C:\Windows\System32\DriverStore — на живой системе десятки тысяч
        /// файлов, поиск занимал минуты и подвешивал окно. Теперь сначала
        /// проверяются известные пути, а DriverStore обходится не глубже
        /// каталога драйвера и с ограничением по времени.
        /// </summary>
        private string FindAnselTool()
        {
            foreach (string path in DirectPaths)
            {
                try
                {
                    if (File.Exists(path)) return path;
                }
                catch
                {
                    // нет доступа — просто идём дальше
                }
            }

            try
            {
                string repository = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "System32", "DriverStore", "FileRepository");

                if (!Directory.Exists(repository)) return null;

                var watch = Stopwatch.StartNew();

                foreach (string folder in Directory.EnumerateDirectories(repository, "nv*"))
                {
                    if (watch.ElapsedMilliseconds > SearchBudgetMs)
                    {
                        _log.Debug(Category, "поиск NvCameraEnable.exe остановлен по таймауту");
                        return null;
                    }

                    string candidate = Path.Combine(folder, "NvCameraEnable.exe");
                    if (File.Exists(candidate)) return candidate;

                    candidate = Path.Combine(folder, "NvCamera", "NvCameraEnable.exe");
                    if (File.Exists(candidate)) return candidate;
                }

                // На всякий случай — неглубокий обход внутри папок драйвера.
                foreach (string folder in Directory.EnumerateDirectories(repository, "nv*"))
                {
                    if (watch.ElapsedMilliseconds > SearchBudgetMs)
                    {
                        _log.Debug(Category, "поиск NvCameraEnable.exe остановлен по таймауту");
                        return null;
                    }

                    try
                    {
                        string[] files = Directory.GetFiles(folder, "NvCameraEnable.exe", SearchOption.AllDirectories);
                        if (files.Length > 0) return files[0];
                    }
                    catch
                    {
                        // папка может быть недоступна
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Debug(Category, "поиск NvCameraEnable.exe: " + ex.Message);
            }

            return null;
        }
    }
}
