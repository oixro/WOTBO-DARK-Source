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
            navigation.Bind(label_apps, panel_apps);
            navigation.Bind(label_startup, panel_startup);
            navigation.Bind(label_cleanup, panel_cleanup);

            navigation.Bind(label_progs, panel_sorry);
            navigation.Bind(label_pc, panel_5);

            label_gpu.Click += (sender, args) => ShowGpuPanel();

            SetupWelcome();
            label_apps.MouseUp += Label_MouseUp;
            label_startup.MouseUp += Label_MouseUp;
            label_cleanup.MouseUp += Label_MouseUp;

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
            Panel[] panels = { panel1, panel_ui_1, panel_dop, panel2, panel_sorry, panel_5, panel_settings, panel_apps, panel_cleanup, panel_startup };

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

        private List<Label> _navLabels;
        private NavIndicator navIndicator;
        private Label _activeNavLabel;

        private static readonly Color NavActiveColor = Color.White;
        private static readonly Color NavInactiveColor = Color.Silver;

        /// <summary>
        /// Подсветка текущего раздела: активный пункт белый, под ним
        /// анимированное подчёркивание (<see cref="NavIndicator"/>),
        /// остальные пункты серые.
        /// </summary>
        private void SetupNavHighlight()
        {
            Label[] labels = { label_main, label_interface, label_gpu, label_dop, label_apps, label_cleanup, label_startup, label_progs, label_pc };
            _navLabels = new List<Label>();

            foreach (Label label in labels)
            {
                if (label != null) _navLabels.Add(label);
            }

            navIndicator = new NavIndicator { Name = "nav_indicator" };
            PanelMain.Controls.Add(navIndicator);
            navIndicator.BringToFront();

            ClearNavHighlight();
        }

        /// <summary>Стартовый экран: ни один раздел не подсвечен.</summary>
        private void ClearNavHighlight()
        {
            foreach (Label label in _navLabels)
            {
                label.ForeColor = NavInactiveColor;
            }

            _activeNavLabel = null;

            if (navIndicator != null) navIndicator.Clear();
        }

        /// <summary>
        /// Цвет пункта, когда курсор с него ушёл: активный раздел остаётся
        /// белым, остальные — серыми. Без этого активный пункт серел после
        /// любого наведения.
        /// </summary>
        private Color NavRestingColor(Control control)
        {
            return control == _activeNavLabel ? NavActiveColor : NavInactiveColor;
        }

        private void HighlightNav(Panel panel)
        {
            if (panel == null) return;

            Label active = null;

            if (panel == panel1) active = label_main;
            else if (panel == panel_ui_1) active = label_interface;
            else if (panel == panel2) active = label_gpu;
            else if (panel == panel_dop) active = label_dop;
            else if (panel == panel_apps) active = label_apps;
            else if (panel == panel_cleanup) active = label_cleanup;
            else if (panel == panel_startup) active = label_startup;
            else if (panel == panel_sorry) active = label_progs;
            else if (panel == panel_5) active = label_pc;

            if (active == null) return;

            foreach (Label label in _navLabels)
            {
                label.ForeColor = label == active ? NavActiveColor : NavInactiveColor;
            }

            _activeNavLabel = active;
            navIndicator.PointTo(active, !ScreenshotMode);
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
