using System;
using System.IO;

namespace WOTBO.Services.Tweaks
{
    public sealed class DirectPlayTweak : TweakBase
    {
        public override string Id { get { return "gpu.directplay"; } }
        public override string Category { get { return "GPU"; } }

        public override TweakState Detect(TweakContext context)
        {
            string[] roots = { @"C:\Windows\SysWOW64", @"C:\Windows\System32" };
            foreach (string root in roots)
            {
                try
                {
                    if (Directory.GetFiles(root, "dpnet.dll.mui", SearchOption.AllDirectories).Length > 0)
                    {
                        return TweakState.Applied;
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException)
                {
                }
            }

            return TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.StartCaptured("dism /online /Enable-Feature /FeatureName:DirectPlay /All");
        }

        public override void Rollback(TweakContext context)
        {
            context.Processes.StartCaptured("Dism /online /Disable-Feature /FeatureName:DirectPlay");
        }
    }
}
