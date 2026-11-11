namespace WOTBO.Services.Tweaks
{
    public sealed class BufferbloatTweak : TweakBase
    {
        public override string Id { get { return "pro.bufferbloat"; } }
        public override string Category { get { return "Pro"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.StartCaptured("netsh int tcp set global autotuninglevel=disabled");
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.StartCaptured("netsh int tcp set global autotuninglevel=normal");
        }
    }
}
