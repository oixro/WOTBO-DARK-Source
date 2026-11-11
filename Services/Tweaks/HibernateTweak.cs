using System.IO;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключение гибернации. Признак применения — отсутствие hiberfil.sys.</summary>
    public sealed class HibernateTweak : TweakBase
    {
        private const string HiberFile = @"C:\hiberfil.sys";

        public override string Id
        {
            get { return "main.hibernate"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return File.Exists(HiberFile)
                ? TweakState.NotApplied
                : TweakState.Applied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.StartCaptured("powercfg -h off");
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.StartCaptured("powercfg -h on");
        }
    }
}
