using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WOTBO.Services.Hardware
{
    /// <summary>Блок отчёта о ПК: заголовок и строки под ним.</summary>
    public sealed class PcReportSection
    {
        public PcReportSection(string title, params string[] lines)
        {
            Title = title ?? string.Empty;
            Lines = (lines ?? new string[0])
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim())
                .ToList();
        }

        public string Title { get; private set; }

        public IList<string> Lines { get; private set; }
    }

    /// <summary>Готовый отчёт: то, что показываем и кладём в буфер обмена.</summary>
    public sealed class PcReport
    {
        public PcReport(IEnumerable<PcReportSection> sections)
        {
            Sections = (sections ?? Enumerable.Empty<PcReportSection>()).ToList();
        }

        public IList<PcReportSection> Sections { get; private set; }

        /// <summary>Текст для буфера обмена: заголовки капсом, секции через пустую строку.</summary>
        public string ToText()
        {
            var builder = new StringBuilder();

            foreach (PcReportSection section in Sections)
            {
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.AppendLine(section.Title);
                foreach (string line in section.Lines)
                {
                    builder.AppendLine(line);
                }
            }

            return builder.ToString().TrimEnd();
        }
    }
}
