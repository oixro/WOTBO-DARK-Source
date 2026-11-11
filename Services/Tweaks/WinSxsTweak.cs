namespace WOTBO.Services.Tweaks
{
    /// <summary>Очистка WinSxS — односторонняя операция.</summary>
    public sealed class WinSxsTweak : TweakBase
    {
        public override string Id { get { return "extra.winsxs"; } }
        public override string Category { get { return "Extra"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.StartDetached("Dism.exe /online /Cleanup-Image /StartComponentCleanup /ResetBase");
        }

        public override void Rollback(TweakContext context)
        {
        }
    }
}
