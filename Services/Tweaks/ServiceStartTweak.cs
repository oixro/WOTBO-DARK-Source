using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключение служб: пишет <c>Start=4</c>, останавливает их и запоминает
    /// прежний тип запуска, чтобы откат вернул всё как было.
    /// </summary>
    public abstract class ServiceStartTweak : TweakBase
    {
        private const string ServicesKey = @"SYSTEM\CurrentControlSet\Services\";
        private const string SavedExtra = "start";
        private const int Disabled = 4;

        protected abstract string[] Services { get; }

        public override TweakState Detect(TweakContext context)
        {
            foreach (string name in Services)
            {
                object start = context.Registry.GetValue(
                    RegistryHive.LocalMachine, ServicesKey + name, "Start");

                if (start == null)
                {
                    continue;
                }

                if (ToInt(start) != Disabled)
                {
                    return TweakState.NotApplied;
                }
            }

            return TweakState.Applied;
        }

        public override void Apply(TweakContext context)
        {
            var previous = new List<string>();

            foreach (string name in Services)
            {
                string path = ServicesKey + name;
                object start = context.Registry.GetValue(RegistryHive.LocalMachine, path, "Start");
                if (start == null)
                {
                    continue;
                }

                previous.Add(name + "=" + ToInt(start));
                context.Registry.WriteValue(
                    RegistryHive.LocalMachine, path, "Start", Disabled, RegistryValueKind.DWord);

                context.Processes.StartCaptured("sc stop " + name + " >nul 2>&1");
            }

            context.Store.WriteExtra(Id, SavedExtra, string.Join(";", previous.ToArray()));
        }

        public override void Rollback(TweakContext context)
        {
            string saved = context.Store.ReadExtra(Id, SavedExtra) as string;
            if (string.IsNullOrEmpty(saved))
            {
                return;
            }

            foreach (string pair in saved.Split(';'))
            {
                string[] parts = pair.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                string name = parts[0];
                int start;
                if (!int.TryParse(parts[1], out start) || start == Disabled)
                {
                    continue;
                }

                context.Registry.WriteValue(
                    RegistryHive.LocalMachine, ServicesKey + name, "Start", start, RegistryValueKind.DWord);

                context.Processes.StartCaptured("sc start " + name + " >nul 2>&1");
            }
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
