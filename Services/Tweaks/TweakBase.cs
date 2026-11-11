using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Общие пути пилотных твиков.</summary>
    public abstract class TweakBase : ITweak
    {
        public abstract string Id { get; }

        public abstract string Category { get; }

        public abstract TweakState Detect(TweakContext context);

        public abstract void Apply(TweakContext context);

        public abstract void Rollback(TweakContext context);

        protected static object Read(RegistryHive hive, string path, string name, TweakContext context)
        {
            return context.Registry.GetValue(hive, path, name);
        }
    }
}
