using System;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Файл подкачки: C:\pagefile.sys 16 32768.</summary>
    public sealed class PagefileTweak : TweakBase
    {
        private const string Value = "PagingFiles";
        private const string AppliedValue = "C:\\pagefile.sys 16 32768";
        private const string DefaultValue = "C:\\pagefile.sys 0 0";

        public override string Id
        {
            get { return "main.pagefile"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = Read(RegistryHive.LocalMachine, RegistryPaths.MemoryManagement, Value, context);
            string[] lines = current as string[];
            if (lines == null)
            {
                return TweakState.NotApplied;
            }

            foreach (string line in lines)
            {
                string[] parts = line.Split(' ');
                if (parts.Length >= 3
                    && parts[0].Equals(@"C:\pagefile.sys", StringComparison.OrdinalIgnoreCase)
                    && parts[1] == "16"
                    && parts[2] == "32768")
                {
                    return TweakState.Applied;
                }
            }

            return TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.MemoryManagement,
                Value,
                new[] { AppliedValue },
                RegistryValueKind.MultiString);
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.WriteValue(
                RegistryHive.LocalMachine,
                RegistryPaths.MemoryManagement,
                Value,
                new[] { DefaultValue },
                RegistryValueKind.MultiString);
        }
    }
}
