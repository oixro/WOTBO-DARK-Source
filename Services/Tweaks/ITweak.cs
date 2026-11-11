using WOTBO.App.Logging;
using WOTBO.Services.Processes;
using WOTBO.Services.Registry;

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
        public TweakContext(IRegistryService registry, IProcessService processes, IAppLogger log)
        {
            Registry = registry;
            Processes = processes;
            Log = log;
        }

        public IRegistryService Registry { get; }

        public IProcessService Processes { get; }

        public IAppLogger Log { get; }
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
