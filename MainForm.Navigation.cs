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

            navigation.Bind(label_main, panel1, panel_main_navigate, button_main_1, button_main_2);
            navigation.Bind(button_main_1, panel1, panel_main_navigate, button_main_1, button_main_2);
            navigation.Bind(button_main_2, panel_main_2, panel_main_navigate, button_main_1, button_main_2);

            navigation.Bind(label_interface, panel_ui_1, panel_ui_navigate, button_ui_new_1, button_ui_new_2);
            navigation.Bind(button_ui_new_1, panel_ui_1, panel_ui_navigate);
            navigation.Bind(button_ui_new_2, panel_ui_2, panel_ui_navigate);

            navigation.Bind(label_dop, panel_dop, panel_dop_navigate, button_dop_new_1, button_dop_new_2);
            navigation.Bind(button_dop_new_1, panel_dop, panel_dop_navigate);
            navigation.Bind(button_dop_new_2, panel_dop_2, panel_dop_navigate);

            navigation.Bind(label_progs, panel_sorry);
            navigation.Bind(label_pc, panel_5);

            label_gpu.Click += (sender, args) => ShowGpuPanel();

            SetupNavHighlight();
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

            HighlightNav(panel1);
        }

        private void HighlightNav(Panel panel)
        {
            if (panel == null) return;

            Label active = null;

            if (panel == panel1 || panel == panel_main_2) active = label_main;
            else if (panel == panel_ui_1 || panel == panel_ui_2) active = label_interface;
            else if (panel == panel2) active = label_gpu;
            else if (panel == panel_dop || panel == panel_dop_2) active = label_dop;
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
