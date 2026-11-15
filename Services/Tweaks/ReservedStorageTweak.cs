using System.Text;
using WOTBO.Services.Processes;

namespace WOTBO.Services.Tweaks
{
    /// <summary>Отключение зарезервированного хранилища Windows (DISM).</summary>
    public sealed class ReservedStorageTweak : TweakBase
    {
        private const string LogCategory = "Tweak:main.reservedstorage";

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
            ProcessOutput result = context.Processes
                .RunAsync(
                    "cmd.exe",
                    "/c chcp 65001 && Dism /Online /Set-ReservedStorageState /State:Disabled",
                    180000,
                    Encoding.UTF8)
                .GetAwaiter()
                .GetResult();

            if (!result.Ok)
            {
                throw new TweakApplyException(
                    "DISM не смог отключить зарезервированное хранилище (код " + result.ExitCode + ").\n\n" + Tail(result.Text),
                    null);
            }

            context.Log.Info(LogCategory, "DISM: " + Tail(result.Text));

            string state = context.Processes
                .RunAsync(
                    "cmd.exe",
                    "/c chcp 65001 && Dism /Online /Get-ReservedStorageState",
                    60000,
                    Encoding.UTF8)
                .GetAwaiter()
                .GetResult()
                .Text;

            if (!IsDisabled(state))
            {
                // На части сборок Windows изменение вступает в силу только
                // после перезагрузки — твик считаем применённым, но пишем в лог.
                context.Log.Warn(LogCategory, "состояние пока включено, изменение применится после перезагрузки");
            }
        }

        public override void Rollback(TweakContext context)
        {
            ProcessOutput result = context.Processes
                .RunAsync(
                    "cmd.exe",
                    "/c chcp 65001 && Dism.exe /Online /Set-ReservedStorageState /State:Enabled",
                    180000,
                    Encoding.UTF8)
                .GetAwaiter()
                .GetResult();

            if (!result.Ok)
            {
                throw new TweakApplyException(
                    "DISM не смог вернуть зарезервированное хранилище (код " + result.ExitCode + ").\n\n" + Tail(result.Text),
                    null);
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
