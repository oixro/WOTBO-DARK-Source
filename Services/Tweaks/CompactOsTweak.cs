using System;
using System.Globalization;
using System.Text;

namespace WOTBO.Services.Tweaks
{
    /// <summary>
    /// CompactOS — односторонняя операция.
    ///
    /// Признак «применён» раньше брался только из сохранённого флага. Теперь
    /// дополнительно спрашиваем саму Windows: <c>compact /CompactOS:query</c>.
    /// Вывод локализован, поэтому ищем отрицание («not in» / «не наход…»):
    /// без него система уже в сжатом состоянии.
    /// </summary>
    public sealed class CompactOsTweak : TweakBase
    {
        public override string Id { get { return "extra.compactos"; } }
        public override string Category { get { return "Extra"; } }

        public override TweakState Detect(TweakContext context)
        {
            try
            {
                // compact пишет в кодировке консоли (OEM), а не в ANSI —
                // без этого русский текст приходил мусором и разбор не срабатывал.
                Encoding oem = Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

                string output = context.Processes
                    .ReadOutputAsync("cmd.exe", "/c compact /CompactOS:query", 20000, oem)
                    .GetAwaiter()
                    .GetResult();

                context.Log.Debug("Tweak:extra.compactos", "query: " + (output ?? string.Empty).Trim());

                bool? compressed = ParseState(output);
                if (compressed.HasValue)
                {
                    return compressed.Value ? TweakState.Applied : TweakState.NotApplied;
                }

                context.Log.Debug("Tweak:extra.compactos", "не удалось разобрать вывод: " + (output ?? string.Empty).Trim());
            }
            catch (Exception exception)
            {
                context.Log.Debug("Tweak:extra.compactos", "compact:query: " + exception.Message);
            }

            return context.Store.IsApplied(Id) ? TweakState.Applied : TweakState.NotApplied;
        }

        public override void Apply(TweakContext context)
        {
            context.Processes.StartDetached("compact /CompactOS:always");
        }

        public override void Rollback(TweakContext context)
        {
            // Операция необратима в рамках твика.
        }

        /// <summary>
        /// <c>true</c> — сжатие включено, <c>false</c> — выключено,
        /// <c>null</c> — вывод не распознан.
        /// </summary>
        private static bool? ParseState(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return null;

            string text = output.ToLowerInvariant();

            bool mentions = text.Contains("compact") || text.Contains("сжат") || text.Contains("сжатом");
            if (!mentions) return null;

            bool negative = text.Contains("not in")
                || text.Contains("is not")
                || text.Contains("не наход")
                || text.Contains("не в ")
                || text.Contains("не включ")
                || text.Contains("отключ");

            return !negative;
        }
    }
}
