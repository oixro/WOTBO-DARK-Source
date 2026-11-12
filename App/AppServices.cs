using System;
using WOTBO.App.Logging;
using WOTBO.Services.Hardware;
using WOTBO.Services.Network;
using WOTBO.Services.Processes;
using WOTBO.Services.Registry;
using WOTBO.Services.State;
using WOTBO.Services.Storage;
using WOTBO.Services.Tweaks;
using WOTBO.Services.Windows;

namespace WOTBO.App
{
    /// <summary>
    /// Точка доступа к сервисам до появления DI-контейнера.
    ///
    /// Сервисы создаются лениво и один раз. По мере перевода форм на
    /// конструкторную инъекцию этот класс будет использоваться всё реже,
    /// но останется удобным местом для «одиночных» зависимостей.
    /// </summary>
    public static class AppServices
    {
        private static readonly Lazy<IRegistryService> RegistryLazy =
            new Lazy<IRegistryService>(() => new RegistryService(Log.Instance));

        private static readonly Lazy<TweakStateStore> TweaksLazy =
            new Lazy<TweakStateStore>(() => new TweakStateStore(Registry, Log.Instance));

        private static readonly Lazy<IProcessService> ProcessesLazy =
            new Lazy<IProcessService>(() => new ProcessService(Log.Instance));

        private static readonly Lazy<TempWorkspace> WorkspaceLazy =
            new Lazy<TempWorkspace>(() => new TempWorkspace(AppPaths.WorkDirectory, Log.Instance));

        private static readonly Lazy<INetworkService> NetworkLazy =
            new Lazy<INetworkService>(() => new NetworkService(Log.Instance));

        private static readonly Lazy<IGitHubUpdateService> UpdatesLazy =
            new Lazy<IGitHubUpdateService>(() => new GitHubUpdateService(Network, Log.Instance));

        private static readonly Lazy<ILatestVersionService> LatestVersionsLazy =
            new Lazy<ILatestVersionService>(() => new LatestVersionService(Network, Log.Instance));

        private static readonly Lazy<UpdateCoordinator> UpdateCoordinatorLazy =
            new Lazy<UpdateCoordinator>(() => new UpdateCoordinator(Updates, Network, Log.Instance));

        private static readonly Lazy<IWindowsVersionService> WindowsVersionLazy =
            new Lazy<IWindowsVersionService>(() => new WindowsVersionService(Registry));

        private static readonly Lazy<IWmiSystemInfoService> WmiLazy =
            new Lazy<IWmiSystemInfoService>(() => new WmiSystemInfoService(Log.Instance));

        private static readonly Lazy<IDisplayService> DisplayLazy =
            new Lazy<IDisplayService>(() => new DisplayService(Log.Instance));

        private static readonly Lazy<IGpuService> GpuLazy =
            new Lazy<IGpuService>(() => new GpuService(Wmi, Registry, Processes, Log.Instance));

        private static readonly Lazy<IReservedStorageService> ReservedStorageLazy =
            new Lazy<IReservedStorageService>(() => new ReservedStorageService(Processes, Log.Instance));

        private static readonly Lazy<IResourceExtractor> ResourceExtractorLazy =
            new Lazy<IResourceExtractor>(() => new ResourceExtractor(Log.Instance));

        private static readonly Lazy<ILegacyCleanupService> LegacyCleanupLazy =
            new Lazy<ILegacyCleanupService>(() => new LegacyCleanupService(Processes, Tweaks, Log.Instance));

        private static readonly Lazy<StartupWorkflow> StartupLazy =
            new Lazy<StartupWorkflow>(() => new StartupWorkflow(Registry, Processes, Tweaks, Log.Instance));

        private static readonly Lazy<TweakContext> TweakContextLazy =
            new Lazy<TweakContext>(() => new TweakContext(
                Registry, Processes, Network, LatestVersions, Gpu, Workspace, Tweaks, WindowsVersion, Log.Instance));

        private static readonly Lazy<TweakCatalog> TweakCatalogLazy =
            new Lazy<TweakCatalog>(TweakCatalog.CreateDefault);

        private static readonly Lazy<TweakStateReconciler> StateReconcilerLazy =
            new Lazy<TweakStateReconciler>(() => new TweakStateReconciler(
                TweakCatalog, TweakContext, Tweaks, Log.Instance));

        /// <summary>Реестр x64 (<c>RegistryView.Registry64</c>).</summary>
        public static IRegistryService Registry
        {
            get { return RegistryLazy.Value; }
        }

        /// <summary>Постоянное состояние твиков и их бэкапы.</summary>
        public static TweakStateStore Tweaks
        {
            get { return TweaksLazy.Value; }
        }

        /// <summary>Запуск процессов с прежней семантикой WOTBO.</summary>
        public static IProcessService Processes
        {
            get { return ProcessesLazy.Value; }
        }

        /// <summary>Рабочая папка и её пути.</summary>
        public static TempWorkspace Workspace
        {
            get { return WorkspaceLazy.Value; }
        }

        /// <summary>Проверка сети и загрузки.</summary>
        public static INetworkService Network
        {
            get { return NetworkLazy.Value; }
        }

        /// <summary>Проверка обновлений GitHub.</summary>
        public static IGitHubUpdateService Updates
        {
            get { return UpdatesLazy.Value; }
        }

        /// <summary>Проверка и установка обновления.</summary>
        public static UpdateCoordinator UpdateCoordinator
        {
            get { return UpdateCoordinatorLazy.Value; }
        }

        /// <summary>Последние версии программ и ffmpeg.</summary>
        public static ILatestVersionService LatestVersions
        {
            get { return LatestVersionsLazy.Value; }
        }

        /// <summary>Определение версии Windows.</summary>
        public static IWindowsVersionService WindowsVersion
        {
            get { return WindowsVersionLazy.Value; }
        }

        /// <summary>WMI-информация о железе.</summary>
        public static IWmiSystemInfoService Wmi
        {
            get { return WmiLazy.Value; }
        }

        /// <summary>Информация об экране.</summary>
        public static IDisplayService Display
        {
            get { return DisplayLazy.Value; }
        }

        /// <summary>NVIDIA/Ansel/HDCP.</summary>
        public static IGpuService Gpu
        {
            get { return GpuLazy.Value; }
        }

        /// <summary>Состояние зарезервированного хранилища (DISM).</summary>
        public static IReservedStorageService ReservedStorage
        {
            get { return ReservedStorageLazy.Value; }
        }

        /// <summary>Распаковка встроенных ресурсов.</summary>
        public static IResourceExtractor ResourceExtractor
        {
            get { return ResourceExtractorLazy.Value; }
        }

        /// <summary>Уборка legacy-артефактов при старте.</summary>
        public static ILegacyCleanupService LegacyCleanup
        {
            get { return LegacyCleanupLazy.Value; }
        }

        /// <summary>Стартовые шаги без привязки к контролам.</summary>
        public static StartupWorkflow Startup
        {
            get { return StartupLazy.Value; }
        }

        /// <summary>Зависимости твиков.</summary>
        public static TweakContext TweakContext
        {
            get { return TweakContextLazy.Value; }
        }

        /// <summary>Каталог твиков.</summary>
        public static TweakCatalog TweakCatalog
        {
            get { return TweakCatalogLazy.Value; }
        }

        /// <summary>Сверка состояния твиков с фактическим Detect.</summary>
        public static TweakStateReconciler StateReconciler
        {
            get { return StateReconcilerLazy.Value; }
        }
    }
}
