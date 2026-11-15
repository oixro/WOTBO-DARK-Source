using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключает алгоритм Nagle на всех активных сетевых интерфейсах
    /// (<c>TcpAckFrequency</c> и <c>TCPNoDelay</c>).
    ///
    /// В отличие от .reg-паков, где нужно вручную подставлять GUID адаптера,
    /// здесь перебираются все интерфейсы, у которых есть IP, а список
    /// применённых запоминается для точного отката.
    /// </summary>
    public sealed class NagleTweak : TweakBase
    {
        private const string Interfaces =
            @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
        private const string AckName = "TcpAckFrequency";
        private const string DelayName = "TCPNoDelay";
        private const string ListExtra = "interfaces";

        public override string Id
        {
            get { return "extra.nagle"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            List<string> active = ActiveInterfaces(context);
            if (active.Count == 0)
            {
                return TweakState.NotApplied;
            }

            foreach (string path in active)
            {
                if (ToInt(context.Registry.GetValue(RegistryHive.LocalMachine, path, AckName)) != 1
                    || ToInt(context.Registry.GetValue(RegistryHive.LocalMachine, path, DelayName)) != 1)
                {
                    return TweakState.NotApplied;
                }
            }

            return TweakState.Applied;
        }

        public override void Apply(TweakContext context)
        {
            var applied = new List<string>();

            foreach (string path in ActiveInterfaces(context))
            {
                context.BackupRegistryKey(RegistryHive.LocalMachine, path);
                context.Registry.WriteValue(RegistryHive.LocalMachine, path, AckName, 1, RegistryValueKind.DWord);
                context.Registry.WriteValue(RegistryHive.LocalMachine, path, DelayName, 1, RegistryValueKind.DWord);
                applied.Add(path.Substring(Interfaces.Length + 1));
            }

            context.Store.WriteExtra(Id, ListExtra, string.Join(";", applied.ToArray()));
        }

        public override void Rollback(TweakContext context)
        {
            string saved = context.Store.ReadExtra(Id, ListExtra) as string;
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            foreach (string name in saved.Split(';'))
            {
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                string path = Interfaces + "\\" + name;
                context.Registry.DeleteValue(RegistryHive.LocalMachine, path, AckName);
                context.Registry.DeleteValue(RegistryHive.LocalMachine, path, DelayName);
            }
        }

        /// <summary>Интерфейсы с выданным IP (DHCP или статическим).</summary>
        private static List<string> ActiveInterfaces(TweakContext context)
        {
            var result = new List<string>();

            foreach (string name in context.Registry.GetSubKeyNames(RegistryHive.LocalMachine, Interfaces))
            {
                string path = Interfaces + "\\" + name;

                string dhcp = Convert.ToString(context.Registry.GetValue(RegistryHive.LocalMachine, path, "DhcpIPAddress"));
                if (!string.IsNullOrEmpty(dhcp) && dhcp != "0.0.0.0")
                {
                    result.Add(path);
                    continue;
                }

                var addresses = context.Registry.GetValue(RegistryHive.LocalMachine, path, "IPAddress") as string[];
                if (addresses != null && addresses.Length > 0
                    && !string.IsNullOrEmpty(addresses[0]) && addresses[0] != "0.0.0.0")
                {
                    result.Add(path);
                }
            }

            return result;
        }

        private static int ToInt(object value)
        {
            if (value == null)
            {
                return 0;
            }

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }
    }
}
