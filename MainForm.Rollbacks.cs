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
        #region откаты






        void back_ui_tooltips_Click(object sender, EventArgs e)
        {
            RollbackTweakTooltips();
        }
        void back_gpu_4_Click(object sender, EventArgs e)
        {
            using (TweakScope.Rollback("gpu.ansel"))
            {
                AnselTweak ansel = (AnselTweak)AppServices.TweakCatalog.Get("gpu.ansel");
                if (ansel.Detect(AppServices.TweakContext) == TweakState.Unavailable)
                {
                    MessageBox.Show("Файл NvCameraEnable.exe не найден");
                }
                ansel.Rollback(AppServices.TweakContext);
                checkBox_ansel.Checked = false;
                checkBox_ansel.Enabled = true;
                back_gpu_4.Visible = false;
            }
        }//ansel
        #endregion
    }
}
