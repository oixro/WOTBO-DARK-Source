using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO;
using WOTBO.App;
using WOTBO.App.Logging;
using WOTBO.Properties;
using WOTBO.Services.Hardware;
using WOTBO.Services.Network;
using WOTBO.Services.Registry;
using WOTBO.Services.Tweaks;
using WOTBO.Services.Windows;
using WOTBO.UI;
namespace WOTBO
{
    public partial class MainForm
    {
        #region перемещение по пунктам
        private void SetupNavigation()
        {
            var navigation = new NavigationController(this, ChangePanel);

            navigation.Bind(label_main, panel1);
            navigation.Bind(label_interface, panel_ui_1);
            navigation.Bind(label_dop, panel_dop);

            navigation.Bind(label_progs, panel_sorry);
            navigation.Bind(label_pc, panel_5);

            label_gpu.Click += (sender, args) => ShowGpuPanel();

            SetupWelcome();
            SetupNavHighlight();
            SetupScrolling();
        }

        /// <summary>
        /// Стартовая панель вместо вкладки «Основное»: короткая подсказка по
        /// центру. Живёт до первого переключения раздела.
        /// </summary>
        private void SetupWelcome()
        {
            welcomePanel = new ScrollPanel
            {
                Name = "panel_welcome",
                Location = panel1.Location,
                Size = panel1.Size,
            };

            var text = new Label
            {
                Name = "label_welcome",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Silver,
                BackColor = Color.Transparent,
                Font = new Font(Font.FontFamily, 9.25f, FontStyle.Regular),
                Padding = new Padding(Dpi.Scale(this, 14), 0, Dpi.Scale(this, 14), 0),
                UseMnemonic = false,
                Text = "WOTBO - утилита для настройки Windows. "
                    + "Выберите необходимую вкладку, отметьте твики на ней "
                    + "и нажмите применить",
            };

            welcomePanel.Controls.Add(text);
            Controls.Add(welcomePanel);
            welcomePanel.BringToFront();

            ShowWelcome();
        }

        /// <summary>Стартовый экран: вкладки скрыты, подсветки в меню нет.</summary>
        private void ShowWelcome()
        {
            Panel[] panels = { panel1, panel_ui_1, panel_dop, panel2, panel_sorry, panel_5 };

            foreach (Panel panel in panels)
            {
                if (panel != null) panel.Visible = false;
            }

            if (welcomePanel != null)
            {
                welcomePanel.Visible = true;
                welcomePanel.BringToFront();
            }
        }

        private void HideWelcome()
        {
            if (welcomePanel != null)
            {
                welcomePanel.Visible = false;
            }
        }

        /// <summary>
        /// Вкладки листаются: вместо страниц «1/2» у панели своя тонкая
        /// полоса прокрутки, поэтому новые твики просто добавляются в список.
        /// </summary>
        private void SetupScrolling()
        {
            Panel[] panels = { panel1, panel_ui_1, panel_dop, panel2, panel_sorry, panel_5 };

            foreach (Panel panel in panels)
            {
                new PanelScroller(panel);
            }
        }

        private readonly Dictionary<Label, Font> _navFonts = new Dictionary<Label, Font>();
        private readonly Dictionary<Label, Font> _navActiveFonts = new Dictionary<Label, Font>();

        private static readonly Color NavActiveColor = Color.White;
        private static readonly Color NavInactiveColor = Color.Silver;

        /// <summary>
        /// Подсветка текущего раздела: активный пункт белый и жирный,
        /// остальные — серые. Раньше все пункты выглядели одинаково.
        /// </summary>
        private void SetupNavHighlight()
        {
            Label[] labels = { label_main, label_interface, label_gpu, label_dop, label_progs, label_pc };

            foreach (Label label in labels)
            {
                if (label == null) continue;
                _navFonts[label] = label.Font;
                // Подчёркивание не меняет ширину текста (в отличие от Bold):
                // «4. Дополнительно» в bold заезжало под панель контента.
                _navActiveFonts[label] = new Font(label.Font, FontStyle.Underline);
            }

            ClearNavHighlight();
        }

        /// <summary>Стартовый экран: ни один раздел не подсвечен.</summary>
        private void ClearNavHighlight()
        {
            foreach (Label label in _navFonts.Keys)
            {
                label.ForeColor = NavInactiveColor;
                label.Font = _navFonts[label];
            }
        }

        private void HighlightNav(Panel panel)
        {
            if (panel == null) return;

            Label active = null;

            if (panel == panel1) active = label_main;
            else if (panel == panel_ui_1) active = label_interface;
            else if (panel == panel2) active = label_gpu;
            else if (panel == panel_dop) active = label_dop;
            else if (panel == panel_sorry) active = label_progs;
            else if (panel == panel_5) active = label_pc;

            if (active == null) return;

            foreach (Label label in _navFonts.Keys)
            {
                bool on = label == active;
                label.ForeColor = on ? NavActiveColor : NavInactiveColor;
                label.Font = on ? _navActiveFonts[label] : _navFonts[label];
            }
        }

        private void ShowGpuPanel()
        {
            bool directplay = AppServices.TweakCatalog.Get("gpu.directplay").Detect(AppServices.TweakContext) == TweakState.Applied;
            AppServices.Tweaks.Reconcile("gpu.directplay", directplay);
            if (directplay)
            {
                checkBox_directplay.Enabled = false;
                back_gpu_1.Visible = true;
            }

            Log.Info("Tweak:gpu.directplay", $"DirectPlay установлен: {directplay}");
            ChangePanel(panel2);
        }
        #endregion
    }
}
