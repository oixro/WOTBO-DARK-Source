using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace WOTBO
{
    public partial class Form4 : Form
    {
        static string tempfolder = @"c:\Windows\oixro";
        public Form4()
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
        private void button1_Click(object sender, EventArgs e)
        {
            hcmd($"{tempfolder}\\MSI_util_v3.exe");
        }
    }
}
