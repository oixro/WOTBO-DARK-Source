using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// Курсоры «W11 Cursors HDPI» (Jepri Creations).
    ///
    /// Раньше установка шла через Install.inf и rundll32 с запросом UAC, а
    /// пользователь вручную выбирал светлый/тёмный вариант. Теперь вариант
    /// подбирается по теме Windows, файлы копируются в
    /// <c>%SystemRoot%\Cursors\WOTBO_*</c>, значения пишутся напрямую в
    /// <c>HKCU\Control Panel\Cursors</c> и применяются через SPI_SETCURSORS.
    /// Откат возвращает прежние значения и удаляет файлы.
    /// </summary>
    public sealed class CursorsTweak : TweakBase
    {
        private const string LogCategory = "Tweak:ui.cursors";
        private const string SchemeName = "WOTBO Cursors";
        private const string ZipUrl = "https://raw.githubusercontent.com/oixro/WOTBO/main/resources/cursors.zip";
        private const string CursorsKey = @"Control Panel\Cursors";
        private const string VariantExtra = "variant";

        private const uint SpiSetCursors = 0x0057;
        private const uint SpifUpdateIniFile = 0x01;
        private const uint SpifSendChange = 0x02;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint action, uint param, IntPtr data, uint flags);

        /// <summary>Файл курсора → имя значения в HKCU\Control Panel\Cursors.</summary>
        private static readonly KeyValuePair<string, string>[] Values =
        {
            new KeyValuePair<string, string>("pointer.cur", "Arrow"),
            new KeyValuePair<string, string>("help.cur", "Help"),
            new KeyValuePair<string, string>("working.ani", "AppStarting"),
            new KeyValuePair<string, string>("busy.ani", "Wait"),
            new KeyValuePair<string, string>("precision.cur", "Crosshair"),
            new KeyValuePair<string, string>("precision.cur", "precisionhair"),
            new KeyValuePair<string, string>("beam.cur", "IBeam"),
            new KeyValuePair<string, string>("handwriting.cur", "NWPen"),
            new KeyValuePair<string, string>("unavailable.cur", "No"),
            new KeyValuePair<string, string>("vert.cur", "SizeNS"),
            new KeyValuePair<string, string>("horz.cur", "SizeWE"),
            new KeyValuePair<string, string>("dgn1.cur", "SizeNWSE"),
            new KeyValuePair<string, string>("dgn2.cur", "SizeNESW"),
            new KeyValuePair<string, string>("move.cur", "SizeAll"),
            new KeyValuePair<string, string>("alternate.cur", "UpArrow"),
            new KeyValuePair<string, string>("link.cur", "Hand"),
            new KeyValuePair<string, string>("pin.cur", "Pin"),
            new KeyValuePair<string, string>("person.cur", "Person"),
        };

        /// <summary>Порядок файлов в строке схемы — как в Install.inf.</summary>
        private static readonly string[] SchemeOrder =
        {
            "pointer.cur", "help.cur", "working.ani", "busy.ani", "precision.cur",
            "beam.cur", "handwriting.cur", "unavailable.cur", "vert.cur", "horz.cur",
            "dgn1.cur", "dgn2.cur", "move.cur", "alternate.cur", "link.cur",
            "person.cur", "pin.cur",
        };

        public override string Id
        {
            get { return "ui.cursors"; }
        }

        public override string Category
        {
            get { return "UI"; }
        }

        public override TweakState Detect(TweakContext context)
        {
            object name = context.Registry.GetValue(
                RegistryHive.CurrentUser, CursorsKey, string.Empty);

            if (!string.Equals(Convert.ToString(name), SchemeName, StringComparison.Ordinal))
            {
                return TweakState.NotApplied;
            }

            object arrow = context.Registry.GetValue(
                RegistryHive.CurrentUser, CursorsKey, "Arrow");

            string path = Convert.ToString(arrow);
            return !string.IsNullOrEmpty(path) && File.Exists(path)
                ? TweakState.Applied
                : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            string variant = IsLightTheme(context) ? "light" : "dark";
            string target = GetTargetDirectory(variant);

            if (!EnsureFiles(context, variant, target))
            {
                return;
            }

            string scheme = BuildSchemeString(target);

            foreach (KeyValuePair<string, string> pair in Values)
            {
                context.Registry.WriteValue(
                    RegistryHive.CurrentUser,
                    CursorsKey,
                    pair.Value,
                    Path.Combine(target, pair.Key),
                    RegistryValueKind.String);
            }

            context.Registry.WriteValue(
                RegistryHive.CurrentUser, CursorsKey, string.Empty, SchemeName, RegistryValueKind.String);

            context.Registry.WriteValue(
                RegistryHive.CurrentUser,
                CursorsKey + @"\Schemes",
                SchemeName,
                scheme,
                RegistryValueKind.String);

            context.Store.WriteExtra(Id, VariantExtra, variant);
            RefreshCursors();
        }

        public override void Rollback(TweakContext context)
        {
            context.Registry.DeleteValue(RegistryHive.CurrentUser, CursorsKey + @"\Schemes", SchemeName);

            // Значения HKCU\Control Panel\Cursors вернёт штатный бэкап твика
            // сразу после этого метода, поэтому файлы удаляем и курсоры
            // обновляем после восстановления.
            Task.Run(delegate
            {
                WaitForRestore(context);
                DeleteDirectories();
                RefreshCursors();
            });
        }

        /// <summary>Дождаться, пока бэкап вернёт прежние значения курсоров.</summary>
        private static void WaitForRestore(TweakContext context)
        {
            for (int i = 0; i < 30; i++)
            {
                object name = context.Registry.GetValue(
                    RegistryHive.CurrentUser, CursorsKey, string.Empty);

                if (!string.Equals(Convert.ToString(name), SchemeName, StringComparison.Ordinal))
                {
                    return;
                }

                Thread.Sleep(100);
            }
        }

        private static void DeleteDirectories()
        {
            foreach (string name in new[] { "light", "dark" })
            {
                try
                {
                    string directory = GetTargetDirectory(name);
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, true);
                    }
                }
                catch
                {
                }
            }
        }

        private static bool IsLightTheme(TweakContext context)
        {
            object value = context.Registry.GetValue(
                RegistryHive.CurrentUser,
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme");

            return value != null && Convert.ToInt32(value) == 1;
        }

        private static string GetTargetDirectory(string variant)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                @"Cursors\WOTBO_" + variant);
        }

        private static string BuildSchemeString(string target)
        {
            var paths = new List<string>();
            foreach (string file in SchemeOrder)
            {
                paths.Add(Path.Combine(target, file));
            }

            return string.Join(",", paths.ToArray());
        }

        private static bool EnsureFiles(TweakContext context, string variant, string target)
        {
            string zip = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"oixro\wotbo\cache\cursors.zip");

            try
            {
                if (!File.Exists(zip))
                {
                    if (!context.Network.IsAvailable())
                    {
                        context.Log.Warn(LogCategory, "нет интернета — курсоры не скачать");
                        return false;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(zip));
                    context.Network.DownloadFile(ZipUrl, zip);
                }

                Directory.CreateDirectory(target);

                using (var archive = ZipFile.OpenRead(zip))
                {
                    string prefix = "cursors/" + variant + "/small/base/";

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (!entry.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string file = entry.Name;
                        if (!file.EndsWith(".cur", StringComparison.OrdinalIgnoreCase)
                            && !file.EndsWith(".ani", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        entry.ExtractToFile(Path.Combine(target, file), true);
                    }
                }

                return true;
            }
            catch (Exception exception)
            {
                context.Log.Error(LogCategory, "не удалось подготовить файлы курсоров", exception);
                return false;
            }
        }

        private static void RefreshCursors()
        {
            try
            {
                SystemParametersInfo(SpiSetCursors, 0, IntPtr.Zero, SpifUpdateIniFile | SpifSendChange);
            }
            catch
            {
            }
        }
    }
}
