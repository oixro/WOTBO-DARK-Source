using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>DefaultTTL для мобильного трафика.</summary>
    public sealed class MobileTrafficTweak : TweakBase
    {
        private const int Ttl = 0x41;

        public override string Id
        {
            get { return "extra.mobiletraffic"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            // Прежняя проверка смотрела только Tcpip\Parameters.
            return context.Registry.ValueExists(
                    RegistryHive.LocalMachine,
                    RegistryPaths.TcpipParameters,
                    RegistryPaths.DefaultTtl)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.TcpipParameters,
                RegistryPaths.DefaultTtl,
                Ttl,
                RegistryValueKind.DWord);

            context.Registry.WriteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.Tcpip6Parameters,
                RegistryPaths.DefaultTtl,
                Ttl,
                RegistryValueKind.DWord);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.TcpipParameters,
                RegistryPaths.DefaultTtl);

            context.Registry.DeleteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.Tcpip6Parameters,
                RegistryPaths.DefaultTtl);
        }
    }
}
