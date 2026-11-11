namespace WOTBO.Services.Tweaks
{
    /// <summary>Ansel через NvCameraEnable.exe.</summary>
    public sealed class AnselTweak : TweakBase
    {
        public override string Id { get { return "gpu.ansel"; } }
        public override string Category { get { return "GPU"; } }

        public override TweakState Detect(TweakContext context)
        {
            bool? disabled = context.Gpu.IsAnselDisabledAsync().GetAwaiter().GetResult();
            if (!disabled.HasValue)
            {
                return TweakState.Unavailable;
            }

            return disabled.Value ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            if (!context.Gpu.SetAnsel(false))
            {
                context.Log.Warn("Tweak:gpu.ansel", "Файл NvCameraEnable.exe не найден");
            }
        }

        public override void Rollback(TweakContext context)
        {
            if (!context.Gpu.SetAnsel(true))
            {
                context.Log.Warn("Tweak:gpu.ansel", "Файл NvCameraEnable.exe не найден");
            }
        }
    }
}
