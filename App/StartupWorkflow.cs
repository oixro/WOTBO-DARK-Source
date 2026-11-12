using System;
using Microsoft.Win32;
using WOTBO.App.Logging;
using WOTBO.Services.Processes;
using WOTBO.Services.Registry;
using WOTBO.Services.State;

namespace WOTBO.App
{
    /// <summary>Шаги старта, которые не связаны с контролами формы.</summary>
    public sealed class StartupWorkflow
    {
        private readonly IRegistryService _registry;
        private readonly IProcessService _processes;
        private readonly TweakStateStore _store;
        private readonly IAppLogger _log;

        public StartupWorkflow(
            IRegistryService registry,
            IProcessService processes,
            TweakStateStore store,
            IAppLogger log)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _processes = processes ?? throw new ArgumentNullException(nameof(processes));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>Завершает предыдущий экземпляр, если он уже запущен.</summary>
        public void EnsureSingleInstance(string exename, string exepath)
        {
            if (InstanceGuard.TryAcquire())
            {
                return;
            }

            _log.Warn("App", "InstanceExists, relaunch");
            _processes.StartCaptured($"taskkill /f /im {exename} && \"{exepath}\"");
            _log.Info("App", "cmd на перезапуск вызван, ждём когда приебашит меня");
        }

        public bool IsEulaAccepted()
        {
            return _registry.GetValue(RegistryHive.CurrentUser, RegistryPaths.AppRoot, "eula") != null;
        }

        public void MigrateState()
        {
            _store.MigrateLegacyFlags();
        }
    }
}
