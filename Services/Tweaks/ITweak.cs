using WOTBO.App.Logging;
using WOTBO.Services.Processes;
using WOTBO.Services.Registry;
using WOTBO.Services.State;
using WOTBO.Services.Storage;
using WOTBO.Services.Windows;

namespace WOTBO.Services.Tweaks
{
    public enum TweakState
    {
        Unknown,
        Applied,
        NotApplied,
        Unavailable,
    }

    /// <summary>Зависимости, которые нужны твику во время Detect/Apply/Rollback.</summary>
    public sealed class TweakContext
    {
        public TweakContext(
            IRegistryService registry,
            IProcessService processes,
            TempWorkspace workspace,
            TweakStateStore store,
            IWindowsVersionService windowsVersion,
            IAppLogger log)
        {
            Registry = registry;
            Processes = processes;
            Workspace = workspace;
            Store = store;
            WindowsVersion = windowsVersion;
            Log = log;
        }

        public IRegistryService Registry { get; }

        public IProcessService Processes { get; }

        public TempWorkspace Workspace { get; }

        public TweakStateStore Store { get; }

        public IWindowsVersionService WindowsVersion { get; }

        public IAppLogger Log { get; }

        /// <summary>Рекурсивно сохранить ветки, которые меняет .reg-файл.</summary>
        public void BackupRegFile(string path)
        {
            TweakBackupSession.RecordRegFile(path);
        }
    }

    /// <summary>
    /// Один твик: проверка состояния, применение и откат.
    /// Не знает о чекбоксах и стрелках — это делает Form1.
    /// </summary>
    public interface ITweak
    {
        string Id { get; }

        string Category { get; }

        TweakState Detect(TweakContext context);

        void Apply(TweakContext context);

        void Rollback(TweakContext context);
    }
}
