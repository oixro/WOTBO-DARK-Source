namespace WOTBO.Services.Tweaks
{
    public sealed class BufferbloatTweak : TweakBase
    {
        public override string Id { get { return "pro.bufferbloat"; } }
        public override string Category { get { return "Pro"; } }

        public override TweakState Detect(TweakContext context)
        {
            try
            {
                string output = context.Processes
                    .ReadOutputAsync("netsh.exe", "int tcp show global")
                    .GetAwaiter()
                    .GetResult();

                foreach (string rawLine in output.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    string line = rawLine.ToLowerInvariant();
                    if (line.Contains("auto-tuning") || line.Contains("autotun") || line.Contains("автонастрой"))
                    {
                        bool disabled = line.Contains("disabled") || line.Contains("отключ") || line.Contains("выключ");
                        return disabled ? TweakState.Applied : TweakState.NotApplied;
                    }
                }
            }
            catch (System.Exception exception)
            {
                context.Log.Debug("Tweak:pro.bufferbloat", "detect: " + exception.Message);
            }

            return TweakState.NotApplied;
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
