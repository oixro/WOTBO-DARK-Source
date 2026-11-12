using System;
using WOTBO.App.Logging;
using WOTBO.Services.Tweaks;

namespace WOTBO.Services.State
{
    /// <summary>
    /// Сверяет историю применения с фактическим Detect при старте.
    /// Нужно, чтобы чекбокс, стрелка отката и tooltip не расходились.
    /// </summary>
    public sealed class TweakStateReconciler
    {
        private const string Category = "State";

        private readonly TweakCatalog _catalog;
        private readonly TweakContext _context;
        private readonly TweakStateStore _store;
        private readonly IAppLogger _log;

        public TweakStateReconciler(
            TweakCatalog catalog,
            TweakContext context,
            TweakStateStore store,
            IAppLogger log)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public void Reconcile()
        {
            foreach (ITweak tweak in _catalog.All)
            {
                try
                {
                    TweakState state = tweak.Detect(_context);
                    bool stored = _store.IsApplied(tweak.Id);

                    if (state == TweakState.Applied && !stored)
                    {
                        _store.MarkApplied(tweak.Id, null);
                        _log.Info(Category, $"{tweak.Id}: фактически применён, состояние добавлено");
                    }
                    else if (state != TweakState.Applied
                             && state != TweakState.Unavailable
                             && stored)
                    {
                        _store.Remove(tweak.Id);
                        _log.Info(Category, $"{tweak.Id}: фактически не применён, состояние удалено");
                    }
                }
                catch (Exception exception)
                {
                    _log.Warn(Category, $"{tweak.Id}: reconcile не удался: {exception.Message}");
                }
            }
        }
    }
}
