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
