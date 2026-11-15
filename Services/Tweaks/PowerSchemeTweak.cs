using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;
using Win32Registry = Microsoft.Win32.Registry;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Схема питания (U)_Shingeki_no_Windows_8.0.
    ///
    /// Ветка <c>PowerSchemes</c> закрыта даже для администратора, поэтому
    /// .reg импортируется одноразовой задачей от SYSTEM, после чего схема
    /// активируется через powercfg. Откат возвращает прежнюю схему и удаляет
    /// импортированную.
    /// </summary>
    public sealed class PowerSchemeTweak : TweakBase
    {
        private const string SchemeGuid = "f11ccd69-bf9c-459c-9114-66ebd91073e8";
        private const string PreviousName = "previousScheme";
        private const string PowerSchemes = @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes";
        private const string SchemeKey = PowerSchemes + @"\" + SchemeGuid;
        private const string ResourceSuffix = "scheme_shingeki.reg";
        private const string RestoreSchemeGuid = "381b4222-f694-41f0-9685-ff5bb260df2e";
        private const string HighPerformanceGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

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
            string active = GetActiveScheme(context);
            return string.Equals(active, SchemeGuid, StringComparison.OrdinalIgnoreCase)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string previous = GetActiveScheme(context);
            if (!string.IsNullOrEmpty(previous))
            {
                context.Store.WriteExtra(Id, PreviousName, previous);
            }

            string path = Path.Combine(context.Workspace.Root, "scheme_shingeki.reg");
            if (!WriteSchemeFile(path, context))
            {
                return;
            }

            // Ветки схемы до импорта нет — бэкап зафиксирует это и откат удалит её.
            context.BackupRegistryKey(RegistryHive.LocalMachine, SchemeKey);

            // Ветка PowerSchemes защищена: regedit от администратора получает
            // "Access is denied". Импорт делаем задачей от SYSTEM.
            RunAsSystem(context, "regedit.exe /s \"" + path + "\"");
            WaitForKey(context);

            if (!SchemeExists(context))
            {
                context.Log.Warn("Tweak:main.powerscheme", "схема не появилась после импорта .reg");
                return;
            }

            Run(context, "-setactive " + SchemeGuid);

            if (!IsActive(context, SchemeGuid))
            {
                context.Log.Warn("Tweak:main.powerscheme", "не удалось активировать схему");

                if (!TrySetActive(context, HighPerformanceGuid))
                {
                    TrySetActive(context, RestoreSchemeGuid);
                }
            }
        }

        public override void Rollback(TweakContext context)
        {
            string previous = context.Store.ReadExtra(Id, PreviousName) as string;

            if (string.IsNullOrEmpty(previous) || !TrySetActive(context, previous))
            {
                TrySetActive(context, RestoreSchemeGuid);
            }

            if (RegistryKeyExists(context))
            {
                Run(context, "-delete " + SchemeGuid);
            }

            if (RegistryKeyExists(context))
            {
                RunAsSystem(context, "reg.exe delete \"HKLM\\" + SchemeKey + "\" /f");
                WaitForKey(context, true);
            }
        }

        /// <summary>
        /// Выполнить команду от SYSTEM через одноразовую задачу: ключи
        /// PowerSchemes недоступны даже администратору.
        /// </summary>
        private static void RunAsSystem(TweakContext context, string command)
        {
            const string taskName = @"\WOTBO\PowerSchemeHelper";
            string runner = Path.Combine(context.Workspace.Root, "powerscheme-helper.cmd");

            try
            {
                File.WriteAllText(runner, "@echo off\r\n" + command + "\r\n");

                context.Processes
                    .ReadOutputAsync("schtasks.exe", "/create /tn \"" + taskName + "\" /tr \"" + runner + "\" /sc once /st 23:59 /ru SYSTEM /f")
                    .GetAwaiter()
                    .GetResult();

                context.Processes
                    .ReadOutputAsync("schtasks.exe", "/run /tn \"" + taskName + "\"")
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception exception)
            {
                context.Log.Error("Tweak:main.powerscheme", "не удалось выполнить команду от SYSTEM", exception);
            }
        }

        private static void WaitForKey(TweakContext context, bool absent = false)
        {
            for (int i = 0; i < 75; i++)
            {
                bool exists = RegistryKeyExists(context);
                if (absent ? !exists : exists)
                {
                    break;
                }

                Thread.Sleep(200);
            }

            try
            {
                context.Processes
                    .ReadOutputAsync("schtasks.exe", "/delete /tn \"\\WOTBO\\PowerSchemeHelper\" /f")
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
            }
        }

        private static string SchemeKeyOf(string guid)
        {
            return PowerSchemes + @"\" + guid;
        }

        private static bool RegistryKeyExists(TweakContext context)
        {
            try
            {
                using (RegistryKey key = Win32Registry.LocalMachine.OpenSubKey(SchemeKey))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool WriteSchemeFile(string path, TweakContext context)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                Assembly assembly = Assembly.GetExecutingAssembly();
                string name = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(name))
                {
                    context.Log.Warn("Tweak:main.powerscheme", "встроенный .reg схемы не найден");
                    return false;
                }

                using (Stream stream = assembly.GetManifestResourceStream(name))
                {
                    if (stream == null)
                    {
                        context.Log.Warn("Tweak:main.powerscheme", "не удалось открыть ресурс схемы");
                        return false;
                    }

                    using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(file);
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                context.Log.Error("Tweak:main.powerscheme", "не удалось записать .reg схемы", exception);
                return false;
            }
        }

        private static bool TrySetActive(TweakContext context, string guid)
        {
            if (string.IsNullOrEmpty(guid) || !SchemeExists(context, guid))
            {
                return false;
            }

            Run(context, "-setactive " + guid);
            return IsActive(context, guid);
        }

        private static bool IsActive(TweakContext context, string guid)
        {
            return string.Equals(GetActiveScheme(context), guid, StringComparison.OrdinalIgnoreCase);
        }

        private static bool SchemeExists(TweakContext context)
        {
            return SchemeExists(context, SchemeGuid);
        }

        private static bool SchemeExists(TweakContext context, string guid)
        {
            try
            {
                using (RegistryKey key = Win32Registry.LocalMachine.OpenSubKey(SchemeKeyOf(guid)))
                {
                    return key != null;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetActiveScheme(TweakContext context)
        {
            string output = context.Processes
                .ReadOutputAsync("powercfg.exe", "-getactivescheme")
                .GetAwaiter()
                .GetResult();

            // Вывод powercfg локализован: на русской Windows это
            // «GUID схемы электропитания: ...», поэтому ищем просто GUID,
            // а не английское «GUID:». Раньше строка не совпадала и твик
            // всегда выглядел неприменённым.
            Match match = Regex.Match(
                output ?? string.Empty,
                @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");

            if (!match.Success)
            {
                context.Log.Debug("Tweak:main.powerscheme", "getactivescheme без GUID: " + (output ?? string.Empty).Trim());
                return null;
            }

            return match.Value;
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
