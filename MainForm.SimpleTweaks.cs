using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
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

                int step = Dpi.Scale(panel, 20);
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
        private static int FindInsertTop(Panel panel, int step)
        {
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
                return Dpi.Scale(panel, 5);
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

            var checkBox = new CheckBox
            {
                AutoSize = true,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                Location = new Point(Dpi.Scale(panel, 20), top),
                Name = "checkBox_" + definition.Id.Replace('.', '_'),
                Size = new Size(Dpi.Scale(panel, 220), Dpi.Scale(panel, 17)),
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
            var picture = new PictureBox
            {
                BackgroundImage = Resources.back,
                BackgroundImageLayout = ImageLayout.Stretch,
                Location = new Point(0, top),
                Name = "back_" + checkBox.Name.Replace("checkBox_", string.Empty),
                Size = new Size(Dpi.Scale(panel, 15), Dpi.Scale(panel, 15)),
                TabIndex = 0,
                TabStop = false,
                Visible = false,
            };

            panel.Controls.Add(picture);
            return picture;
        }
    }
}
