using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Отключает автозапуск тяжёлых программ так же, как это делает
    /// диспетчер задач: пишет «отключено» в <c>StartupApproved\Run</c>.
    ///
    /// Сами значения <c>Run</c> не трогаются, поэтому вернуть программу можно
    /// галочкой в диспетчере задач.
    /// </summary>
    public sealed class AutostartTweak : TweakBase
    {
        private const string RunHkcu = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunHklm = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string ApprovedHkcu = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
        private const string ApprovedHklm = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

        private static readonly string[] Patterns =
        {
            "onedrive",
            "teams",
            "discord",
            "steam",
            "epicgameslauncher",
            "adobe",
            "skype",
            "zoom",
            "slack",
            "spotify",
            "cortana",
            "msteams",
        };

        private static readonly byte[] DisabledFlag = { 0x03, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        private sealed class Entry
        {
            public RegistryHive Hive;
            public string ApprovedPath;
            public string Name;
        }

        public override string Id
        {
            get { return "extra.autostart"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            List<Entry> entries = Find(context);
            if (entries.Count == 0)
            {
                return TweakState.Applied;
            }

            return entries.All(entry => IsDisabled(context, entry))
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            foreach (Entry entry in Find(context))
            {
                context.Registry.WriteValue(
                    entry.Hive, entry.ApprovedPath, entry.Name, DisabledFlag, RegistryValueKind.Binary);

                context.Log.Info(Id, "автозапуск отключён: " + entry.Name);
            }
        }

        public override void Rollback(TweakContext context)
        {
            // Значения StartupApproved вернёт бэкап TweakScope.
        }

        private static List<Entry> Find(TweakContext context)
        {
            var result = new List<Entry>();
            Collect(context, RegistryHive.CurrentUser, RunHkcu, ApprovedHkcu, result);
            Collect(context, RegistryHive.LocalMachine, RunHklm, ApprovedHklm, result);
            return result;
        }

        private static void Collect(
            TweakContext context, RegistryHive hive, string runPath, string approvedPath, List<Entry> into)
        {
            foreach (KeyValuePair<string, object> pair in context.Registry.ReadValues(hive, runPath))
            {
                string text = ((pair.Key ?? string.Empty) + " " + Convert.ToString(pair.Value)).ToLowerInvariant();

                if (!Patterns.Any(pattern => text.Contains(pattern)))
                {
                    continue;
                }

                into.Add(new Entry { Hive = hive, ApprovedPath = approvedPath, Name = pair.Key });
            }
        }

        private static bool IsDisabled(TweakContext context, Entry entry)
        {
            var value = context.Registry.GetValue(entry.Hive, entry.ApprovedPath, entry.Name) as byte[];
            return value != null && value.Length > 0 && value[0] == 0x03;
        }
    }
}
