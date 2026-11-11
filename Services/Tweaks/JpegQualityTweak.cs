using System;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключение сжатия обоев (JPEGImportQuality = 100).</summary>
    public sealed class JpegQualityTweak : TweakBase
    {
        private const string Path = WOTBO.Services.Registry.RegistryPaths.Desktop;
        private const string Value = WOTBO.Services.Registry.RegistryPaths.JpegImportQuality;

        public override string Id
        {
            get { return "ui.jpegquality"; }
        }

        public override string Category
        {
            get { return "UI"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = Read(RegistryHive.CurrentUser, Path, Value, context);
            return current != null && Convert.ToInt32(current) == 100
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.CurrentUser, Path, Value, 100, RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.CurrentUser, Path, Value);
        }
    }
}
