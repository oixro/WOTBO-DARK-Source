using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WOTBO.App.Logging;
using WOTBO.Properties;
using WOTBO.Services.Tweaks;
using WOTBO.UI;

namespace WOTBO
{
    /// <summary>
    /// Строки новых простых твиков создаются в коде: держать десятки
    /// чекбоксов в дизайнере бессмысленно, а позиции всё равно считаются
    /// от последней строки панели. Список твиков лежит в
    /// <see cref="SimpleTweaks"/>, здесь только раскладка и биндинг.
    /// </summary>
    public partial class MainForm
    {
        private readonly List<string> _simpleTweakIds = new List<string>();

        private void SetupSimpleTweaks()
        {
            foreach (IGrouping<string, SimpleTweakDefinition> group in
                SimpleTweaks.All.GroupBy(definition => definition.Panel))
            {
                Panel panel = SimpleTweakPanel(group.Key);
                if (panel == null)
                {
                    continue;
                }

                int step = RowStep(panel);
                int top = FindInsertTop(panel, step);
                int count = group.Count();

                ShiftRows(panel, top, step * count);

                foreach (SimpleTweakDefinition definition in group)
                {
                    CheckBox checkBox = CreateTweakRow(panel, definition, top);
                    PictureBox rollback = CreateRollbackRow(panel, checkBox, top);

                    Bind(definition.Id, checkBox, rollback);
                    _simpleTweakIds.Add(definition.Id);

                    top += step;
                }
            }
        }

        private Panel SimpleTweakPanel(string key)
        {
            switch (key)
            {
                case "main": return panel1;
                case "ui": return panel_ui_1;
                case "gpu": return panel2;
                case "extra": return panel_dop;
                default: return null;
            }
        }

        /// <summary>
        /// Куда вставлять строки: сразу после последнего чекбокса, но перед
        /// подписями-действиями, если они стоят вплотную под списком
        /// (например, «Активировать Windows» в «Дополнительно»).
        /// </summary>
        /// <summary>Шаг строк на этой панели: у масштабированного окна он не 20.</summary>
        private static int RowStep(Panel panel)
        {
            List<int> tops = panel.Controls
                .OfType<CheckBox>()
                .Select(control => control.Top)
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            int step = int.MaxValue;
            for (int i = 1; i < tops.Count; i++)
            {
                int delta = tops[i] - tops[i - 1];
                if (delta > 0 && delta < step)
                {
                    step = delta;
                }
            }

            return step == int.MaxValue ? Dpi.Px(20) : step;
        }

        private static int FindInsertTop(Panel panel, int step)
        {
            // На GPU сверху ссылки, а часть строк NVIDIA скрыта на чужой
            // карте: ставим новые строки сразу после ссылок, чтобы не
            // оставлять дыру на пол-списка.
            if (panel != null && panel.Name == "panel2")
            {
                CheckBox first = panel.Controls
                    .OfType<CheckBox>()
                    .OrderBy(control => control.Top)
                    .FirstOrDefault();

                if (first != null)
                {
                    return first.Top;
                }
            }

            int last = -1;
            int next = int.MaxValue;

            foreach (Control control in panel.Controls)
            {
                if (control.Name.EndsWith("_thumb", StringComparison.Ordinal))
                {
                    continue;
                }

                if (control is CheckBox)
                {
                    last = Math.Max(last, control.Top);
                }
            }

            if (last < 0)
            {
                return Dpi.Px(5);
            }

            foreach (Control control in panel.Controls)
            {
                if (control.Name.EndsWith("_thumb", StringComparison.Ordinal))
                {
                    continue;
                }

                if (control.Top > last)
                {
                    next = Math.Min(next, control.Top);
                }
            }

            int afterLast = last + step;
            if (next != int.MaxValue && next <= afterLast)
            {
                return next;
            }

            return afterLast;
        }

        /// <summary>Сдвинуть подписи и стрелки ниже точки вставки.</summary>
        private static void ShiftRows(Panel panel, int top, int delta)
        {
            if (delta == 0)
            {
                return;
            }

            foreach (Control control in panel.Controls)
            {
                if (control.Name.EndsWith("_thumb", StringComparison.Ordinal))
                {
                    continue;
                }

                if (control.Top >= top)
                {
                    control.Top += delta;
                }
            }
        }

        private CheckBox CreateTweakRow(Panel panel, SimpleTweakDefinition definition, int top)
        {
            CheckBox template = panel.Controls.OfType<CheckBox>().FirstOrDefault();
            int left = template != null ? template.Left : Dpi.Px(20);

            var checkBox = new CheckBox
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Location = new Point(left, top),
                Name = "checkBox_" + definition.Id.Replace('.', '_'),
                TabIndex = 0,
                Tag = "",
                Text = definition.Text,
                UseVisualStyleBackColor = true,
            };

            if (template != null)
            {
                checkBox.Font = template.Font;
            }

            checkBox.CheckedChanged += CheckBox_CheckedChanged;
            panel.Controls.Add(checkBox);
            toolTip1.SetToolTip(checkBox, definition.Tooltip);

            return checkBox;
        }

        private PictureBox CreateRollbackRow(Panel panel, CheckBox checkBox, int top)
        {
            PictureBox template = panel.Controls
                .OfType<PictureBox>()
                .FirstOrDefault(control => control.Name.StartsWith("back_", StringComparison.Ordinal));

            var picture = new PictureBox
            {
                BackgroundImage = Resources.back,
                BackgroundImageLayout = ImageLayout.Stretch,
                Location = new Point(0, top),
                Name = "back_" + checkBox.Name.Replace("checkBox_", string.Empty),
                Size = template != null ? template.Size : new Size(Dpi.Px(15), Dpi.Px(15)),
                TabIndex = 0,
                TabStop = false,
                Visible = false,
            };

            panel.Controls.Add(picture);
            return picture;
        }
    }
}
