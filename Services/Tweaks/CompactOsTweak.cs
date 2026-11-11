namespace WOTBO.Services.Tweaks
{
    /// <summary>CompactOS — односторонняя операция.</summary>
    public sealed class CompactOsTweak : TweakBase
    {
        public override string Id { get { return "extra.compactos"; } }
        public override string Category { get { return "Extra"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.StartDetached("compact /CompactOS:always");
        }

        public override void Rollback(TweakContext context)
        {
            // Операция необратима в рамках твика.
        }
    }
}
