using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WOTBO.Services.Hardware;

namespace WOTBO.UI
{
    /// <summary>
    /// Вывод отчёта о ПК на вкладку «Инфо о ПК»: блоки с заголовками,
    /// строки серебряным и ссылка «Скопировать в буфер» в конце.
    /// </summary>
    public sealed class PcInfoView
    {
        private const int Left = 4;
        private const int Top = 4;
        private const int SectionGap = 10;
        private const int HeaderGap = 2;
        private const int Width = 272;

        private static readonly Color HeaderColor = Color.White;
        private static readonly Color BodyColor = Color.FromArgb(192, 192, 192);
        private static readonly Color LinkColor = Color.FromArgb(200, 215, 230);

        private readonly Panel _host;
        private readonly Action<string> _copy;
        private readonly Font _headerFont;
        private readonly List<Control> _items = new List<Control>();
        private string _text = string.Empty;

        public PcInfoView(Panel host, Action<string> copy)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            _host = host;
            _copy = copy;
            _headerFont = new Font(host.Font, FontStyle.Bold);
        }

        /// <summary>Текст, который уйдёт в буфер обмена.</summary>
        public string Text
        {
            get { return _text; }
        }

        public void ShowLoading(string message)
        {
            Clear();
            _text = string.Empty;
            AddLabel(message, BodyColor, _host.Font, Dpi.Scale(_host, Top));
            PanelScroller.Refresh(_host);
        }

        public void ShowError(string message)
        {
            ShowLoading(message);
        }

        public void Render(PcReport report)
        {
            if (report == null)
            {
                return;
            }

            _text = report.ToText();
            Clear();

            int y = Dpi.Scale(_host, Top);

            foreach (PcReportSection section in report.Sections)
            {
                y = AddLabel(section.Title, HeaderColor, _headerFont, y);
                y += Dpi.Scale(_host, HeaderGap);

                foreach (string line in section.Lines)
                {
                    y = AddLabel(line, BodyColor, _host.Font, y);
                }

                y += Dpi.Scale(_host, SectionGap);
            }

            AddCopyLink(y + Dpi.Scale(_host, 4));
            PanelScroller.Refresh(_host);
        }

        private int AddLabel(string text, Color color, Font font, int y)
        {
            if (string.IsNullOrEmpty(text))
            {
                return y;
            }

            var label = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(Dpi.Scale(_host, Width), 0),
                ForeColor = color,
                BackColor = Color.Transparent,
                Font = font,
                // y уже в пикселях (label.Bottom), масштабировать его повторно нельзя.
                Location = new Point(Dpi.Scale(_host, Left), y),
                Text = text,
            };

            _host.Controls.Add(label);
            _items.Add(label);

            return label.Bottom;
        }

        private void AddCopyLink(int y)
        {
            var link = new Label
            {
                AutoSize = true,
                Cursor = WOTBO.UI.SystemCursors.Hand,
                ForeColor = LinkColor,
                BackColor = Color.Transparent,
                Font = new Font(_host.Font, FontStyle.Underline),
                Location = new Point(Dpi.Scale(_host, Left), y),
                Text = "Скопировать в буфер",
            };

            link.Click += delegate
            {
                if (_copy != null && _text.Length > 0)
                {
                    _copy(_text);
                }
            };

            HoverAnimator.Attach(new Control[] { link }, LinkColor, Color.White);

            _host.Controls.Add(link);
            _items.Add(link);
        }

        private void Clear()
        {
            foreach (Control item in _items)
            {
                _host.Controls.Remove(item);
                item.Dispose();
            }

            _items.Clear();
        }
    }
}
