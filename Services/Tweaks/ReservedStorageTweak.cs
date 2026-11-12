namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключение зарезервированного хранилища Windows (DISM).</summary>
    public sealed class ReservedStorageTweak : TweakBase
    {
        public override string Id { get { return "main.reservedstorage"; } }
        public override string Category { get { return "Main"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.StartCaptured(@"Dism /Online /Set-ReservedStorageState /State:Disabled");
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.StartCaptured("Dism.exe /Online /Set-ReservedStorageState /State:Enabled");
        }
    }
}
