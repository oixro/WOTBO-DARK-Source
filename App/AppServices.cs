using System;
using WOTBO.App.Logging;
using WOTBO.Services.Network;
using WOTBO.Services.Processes;
using WOTBO.Services.Registry;
using WOTBO.Services.State;
using WOTBO.Services.Storage;
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

        private static readonly Lazy<IWindowsVersionService> WindowsVersionLazy =
            new Lazy<IWindowsVersionService>(() => new WindowsVersionService(Registry));

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

        /// <summary>Определение версии Windows.</summary>
        public static IWindowsVersionService WindowsVersion
        {
            get { return WindowsVersionLazy.Value; }
        }
    }
}
