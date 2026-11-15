namespace WOTBO.Services.Tweaks
{
    /// <summary>Службы телеметрии и диагностики, которые висят в памяти.</summary>
    public sealed class TelemetryServicesTweak : ServiceStartTweak
    {
        private static readonly string[] Names =
        {
            "DiagTrack",
            "dmwappushservice",
            "RetailDemo",
            "WMPNetworkSvc",
        };

        public override string Id
        {
            get { return "extra.telemetry"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        protected override string[] Services
        {
            get { return Names; }
        }
    }
}
