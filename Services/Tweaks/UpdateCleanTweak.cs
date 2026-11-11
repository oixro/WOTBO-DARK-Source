namespace WOTBO.Services.Tweaks
{
    /// <summary>Очистка кэша Windows Update/Delivery Optimization.</summary>
    public sealed class UpdateCleanTweak : TweakBase
    {
        public override string Id { get { return "extra.updateclean"; } }
        public override string Category { get { return "Extra"; } }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.StartCaptured("net stop wuauserv >nul");
            context.Processes.StartCaptured(@"del /q /f /s %windir%\SoftwareDistribution\Download\*.* >nul");
            context.Processes.StartCaptured(@"rd /q /s %windir%\SoftwareDistribution\Download\ >nul");
            context.Processes.StartCaptured(@"del /q /f /s %windir%\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache\ >nul");
            context.Processes.StartCaptured(@"rd /q /s %windir%\ServiceProfiles\NetworkService\AppData\Local\Microsoft\Windows\DeliveryOptimization\Cache\ >nul");
        }

        public override void Rollback(TweakContext context)
        {
        }
    }
}
