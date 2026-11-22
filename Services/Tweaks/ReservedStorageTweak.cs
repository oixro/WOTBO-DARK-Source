using System;
using System.Globalization;
using System.Text;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключение зарезервированного хранилища Windows (DISM).</summary>
    public sealed class ReservedStorageTweak : TweakBase
    {
        private const string LogCategory = "Tweak:main.reservedstorage";

        /// <summary>0x800F0975: зарезервированное хранилище занято обслуживанием Windows.</summary>
        private const int ReservedStorageInUse = unchecked((int)0x800F0975);

        /// <summary>DISM пишет в кодировке консоли, а не в UTF-8, даже после chcp.</summary>
        private static readonly Encoding ConsoleEncoding = CreateConsoleEncoding();

        public override string Id { get { return "main.reservedstorage"; } }
        public override string Category { get { return "Main"; } }

        public override TweakState Detect(TweakContext context)
        {
            // Реальное состояние спрашивает ReservedStorageService,
            // здесь — только сохранённый флаг для порядка.
            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            // Раньше команда уходила «в никуда»: если DISM отказывался, твик
            // всё равно помечался применённым. Теперь ждём результат и
            // проверяем код возврата.
            ProcessOutput result = RunDism(context, "/Online /Set-ReservedStorageState /State:Disabled", 180000);

            if (!result.Ok)
            {
                context.Log.Error(LogCategory, "DISM не смог отключить зарезервированное хранилище (код 0x"
                    + ((uint)result.ExitCode).ToString("X8") + "):\n" + result.Text);

                throw new TweakApplyException(DescribeFailure(result, "отключить"), null);
            }

            context.Log.Info(LogCategory, "DISM: " + Tail(result.Text));

            string state = RunDism(context, "/Online /Get-ReservedStorageState", 60000).Text;

            if (!IsDisabled(state))
            {
                // На части сборок Windows изменение вступает в силу только
                // после перезагрузки — твик считаем применённым, но пишем в лог.
                context.Log.Warn(LogCategory, "состояние пока включено, изменение применится после перезагрузки");
            }
        }

        public override void Rollback(TweakContext context)
        {
            ProcessOutput result = RunDism(context, "/Online /Set-ReservedStorageState /State:Enabled", 180000);

            if (!result.Ok)
            {
                context.Log.Error(LogCategory, "DISM не смог вернуть зарезервированное хранилище (код 0x"
                    + ((uint)result.ExitCode).ToString("X8") + "):\n" + result.Text);

                throw new TweakApplyException(DescribeFailure(result, "вернуть"), null);
            }
        }

        private static ProcessOutput RunDism(TweakContext context, string arguments, int timeoutMs)
        {
            return context.Processes
                .RunAsync("cmd.exe", "/c Dism " + arguments, timeoutMs, ConsoleEncoding)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Понятный текст вместо дампа локализованного вывода DISM.
        /// Код 0x800F0975 — не поломка WOTBO: Windows в этот момент ставит
        /// обновление и держит зарезервированное хранилище занятым.
        /// </summary>
        private static string DescribeFailure(ProcessOutput result, string action)
        {
            if (result.ExitCode == ReservedStorageInUse)
            {
                return "Windows сейчас использует зарезервированное хранилище — обычно идёт установка обновления.\n\n"
                    + "Перезагрузите ПК, дайте Windows завершить обновление и повторите.";
            }

            return "DISM не смог " + action + " зарезервированное хранилище (код 0x"
                + ((uint)result.ExitCode).ToString("X8") + ").\n\n"
                + "Подробности: C:\\Windows\\Logs\\DISM\\dism.log";
        }

        private static Encoding CreateConsoleEncoding()
        {
            try
            {
                return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            }
            catch
            {
                try
                {
                    return Encoding.GetEncoding(866);
                }
                catch
                {
                    return Encoding.Default;
                }
            }
        }

        private static bool IsDisabled(string state)
        {
            if (string.IsNullOrEmpty(state)) return false;

            string text = state.ToLowerInvariant();
            return text.Contains("disabled") || text.Contains("отключ") || text.Contains("выключ");
        }

        private static string Tail(string text)
        {
            string line = (text ?? string.Empty).Replace("\r", string.Empty).Trim();
            return line.Length <= 400 ? line : line.Substring(line.Length - 400);
        }
    }
}
