using System;
using System.Windows.Forms;
using WOTBO.App;

namespace WOTBO
{
    public partial class MsiModeForm : Form
    {
        static string tempfolder = @"c:\Windows\oixro";
        public MsiModeForm()
        {
            InitializeComponent();
            AppIcon.Apply(this);
        }
        void hcmd(string line)
        {
            AppServices.Processes.StartDetached(line, true);
        }
        private void button1_Click(object sender, EventArgs e)
        {
            hcmd($"{tempfolder}\\MSI_util_v3.exe");
        }
    }
}
