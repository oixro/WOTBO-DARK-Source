using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using WOTBO.Properties;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Импорт и активация power scheme.
    ///
    /// Отличия от старого кода: команды powercfg выполняются по очереди и
    /// ожидаются, перед импортом запоминается активная схема, откат
    /// возвращает её и удаляет импортированную. Старый
    /// <c>-restoredefaultschemes</c> при apply убран.
    /// </summary>
    public sealed class PowerSchemeTweak : TweakBase
    {
        private const string PreviousName = "previousScheme";
        private const string ImportedName = "importedScheme";
        private const string GuidPattern = @"GUID:\s*([0-9a-fA-F-]{36})";
        private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
        private const string BalancedGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";

        public override string Id
        {
            get { return "main.powerscheme"; }
        }

        public override string Category
        {
            get { return "Main"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            return context.Store.IsApplied(Id)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string previous = GetActiveScheme(context);
            string path = context.Workspace.Scheme;
            File.WriteAllBytes(path, Resources.scheme);

            string guid = Guid.NewGuid().ToString();
            context.Log.Debug("Tweak:main.powerscheme", guid);

            Run(context, $"-import \"{path}\" {guid}");
            bool imported = SchemeExists(context, guid);

            if (imported)
            {
                Run(context, $"-setactive {guid}");
            }

            if (!imported || !IsActive(context, guid))
            {
                // Встроенный scheme.pow может не импортироваться на новых
                // сборках Windows. Не оставляем пользователя без схемы.
                context.Log.Warn("Tweak:main.powerscheme", "импорт схемы не удался, включаю High performance");
                if (!TrySetActive(context, HighPerformanceGuid))
                {
                    context.Log.Warn("Tweak:main.powerscheme", "High performance недоступна, включаю Balanced");
                    TrySetActive(context, BalancedGuid);
                }
            }

            if (!string.IsNullOrEmpty(previous))
            {
                context.Store.WriteExtra(Id, PreviousName, previous);
            }

            if (imported)
            {
                context.Store.WriteExtra(Id, ImportedName, guid);
            }
        }

        public override void Rollback(TweakContext context)
        {
            string previous = context.Store.ReadExtra(Id, PreviousName) as string;
            string imported = context.Store.ReadExtra(Id, ImportedName) as string;

            if (string.IsNullOrEmpty(previous) || !TrySetActive(context, previous))
            {
                // Совместимость со старым поведением, если GUID не сохранился
                // или схема уже удалена.
                Run(context, "-restoredefaultschemes");
            }

            if (!string.IsNullOrEmpty(imported))
            {
                Run(context, $"-delete {imported}");
            }
        }

        private static bool TrySetActive(TweakContext context, string guid)
        {
            if (string.IsNullOrEmpty(guid) || !SchemeExists(context, guid))
            {
                return false;
            }

            Run(context, $"-setactive {guid}");
            return IsActive(context, guid);
        }

        private static bool IsActive(TweakContext context, string guid)
        {
            return string.Equals(GetActiveScheme(context), guid, StringComparison.OrdinalIgnoreCase);
        }

        private static bool SchemeExists(TweakContext context, string guid)
        {
            string list = context.Processes
                .ReadOutputAsync("powercfg.exe", "-list")
                .GetAwaiter()
                .GetResult();

            return !string.IsNullOrEmpty(list)
                && list.IndexOf(guid, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetActiveScheme(TweakContext context)
        {
            string output = context.Processes
                .ReadOutputAsync("powercfg.exe", "-getactivescheme")
                .GetAwaiter()
                .GetResult();

            Match match = Regex.Match(output ?? string.Empty, GuidPattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static void Run(TweakContext context, string arguments)
        {
            string output = context.Processes
                .ReadOutputAsync("powercfg.exe", arguments)
                .GetAwaiter()
                .GetResult();

            if (!string.IsNullOrWhiteSpace(output))
            {
                context.Log.Debug("Tweak:main.powerscheme", output.Trim());
            }
        }
    }
}
