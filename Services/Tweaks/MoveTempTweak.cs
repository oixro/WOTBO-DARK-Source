using System;
using System.IO;
using Microsoft.Win32;
using WOTBO.Services.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Перенос TEMP/TMP в C:\Temp.</summary>
    public sealed class MoveTempTweak : TweakBase
    {
        private const string Target = @"%systemdrive%\Temp";
        private const string Default = @"%USERPROFILE%\AppData\Local\Temp";

        public override string Id
        {
            get { return "extra.movetemp"; }
        }

        public override string Category
        {
            get { return "Extra"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object current = context.Registry.GetValue(RegistryHive.CurrentUser, RegistryPaths.Environment, "TEMP");
            string value = Convert.ToString(current);
            if (string.IsNullOrEmpty(value))
            {
                return TweakState.NotApplied;
            }

            string expanded = Environment.ExpandEnvironmentVariables(value)
                .TrimEnd('\\');
            string target = Environment.ExpandEnvironmentVariables(Target)
                .TrimEnd('\\');

            return string.Equals(expanded, target, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value.Trim(), Target, StringComparison.OrdinalIgnoreCase)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.Environment, "TEMP", Target, RegistryValueKind.ExpandString);
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.Environment, "TMP", Target, RegistryValueKind.ExpandString);
            Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(Target));
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.Environment, "TEMP", Default, RegistryValueKind.ExpandString);
            context.Registry.WriteValue(RegistryHive.CurrentUser, RegistryPaths.Environment, "TMP", Default, RegistryValueKind.ExpandString);
        }
    }
}
