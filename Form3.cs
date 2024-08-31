using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Windows.Forms;
using WOTBO.Properties;

namespace WOTBO
{
    public partial class Form3 : Form
    {
        static string tempfolder = @"c:\Windows\oixro";
        static string path_NVidiaProfileInspectorDmW = tempfolder + @"\NVidiaProfileInspectorDmW";

        public Form3()
        {
            InitializeComponent();
        }

        void hcmd(string line)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {line}",
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch { }
        }
        private void Form3_Load(object sender, EventArgs e)
        {

        }
    }
}
