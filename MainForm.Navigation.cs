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
        #region main    
        void label1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel1);
            //panel1.Visible = true;
            //panel1.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = Dpi.Scale(this, 195, 284);

        } //main        
        void button_main_1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel1);
            //panel1.Visible = true;
            //panel1.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = Dpi.Scale(this, 195, 284);
        }
        void button_main_2_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_main_2);
            //panel_main_2.Visible = true;
            //panel_main_2.Location = panel1.Location;
            button_main_1.Visible = true;
            button_main_2.Visible = true;
            panel_main_navigate.Visible = true;
            panel_main_navigate.Location = Dpi.Scale(this, 195, 284);
        }
        #endregion
        #region ui
        void label_interface_Click(object sender, EventArgs e)
        {

            ChangePanel(panel_ui_1);
            //panel_ui_1.Visible = true;
            //panel_ui_1.Location = panel1.Location;
            button_ui_new_1.Visible = true;
            button_ui_new_2.Visible = true;
            panel_ui_navigate.Visible = true;
            panel_ui_navigate.Location = Dpi.Scale(this, 195, 284);

        }//ui
        void button_ui_1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_ui_1);
            //panel_ui_1.Visible = true;
            //panel_ui_1.Location = panel1.Location;
            panel_ui_navigate.Visible = true;
        }
        void button_ui_2_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_ui_2);
            //panel_ui_2.Visible = true;
            //panel_ui_2.Location = panel1.Location;
            panel_ui_navigate.Visible = true;
        }
        #endregion
        #region gpu
        void label2_Click(object sender, EventArgs e)
        {
            #region directplay
            bool directplay = AppServices.TweakCatalog.Get("gpu.directplay").Detect(AppServices.TweakContext) == TweakState.Applied;
            if (directplay)
            {
                checkBox_directplay.Enabled = false;
                back_gpu_1.Visible = true;
            }

            Log.Info("Tweak:gpu.directplay", $"DirectPlay установлен: {directplay}");
            #endregion
            ChangePanel(panel2);
        } //gpu
        #endregion
        #region dop
        void label3_Click(object sender, EventArgs e) //dop
        {
            ChangePanel(panel_dop);
            //panel_dop.Visible = true;
            //panel_dop.Location = panel1.Location;
            button_dop_new_1.Visible = true;
            button_dop_new_2.Visible = true;
            panel_dop_navigate.Visible = true;
            panel_dop_navigate.Location = Dpi.Scale(this, 195, 284);
        }
        void button_dop_new_1_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_dop);
            //panel_dop.Visible = true;
            //panel_dop.Location = panel1.Location;
            panel_dop_navigate.Visible = true;
        }
        void button_dop_new_2_Click(object sender, EventArgs e)
        {
            ChangePanel(panel_dop_2);
            //panel_dop_2.Visible = true;
            //panel_dop_2.Location = panel1.Location;
            panel_dop_navigate.Visible = true;
        }
        #endregion
        #region dowloads
        void label6_Click(object sender, EventArgs e) // проги
        {
            ChangePanel(panel_sorry);
        }
        #endregion
        #region pc info
        void label8_Click(object sender, EventArgs e) //pc info
        {
            ChangePanel(panel_5);
        }
        #endregion

        #endregion
    }
}
