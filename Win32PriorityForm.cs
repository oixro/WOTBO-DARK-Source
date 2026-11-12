using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WOTBO.App;

namespace WOTBO
{
    public partial class Win32PriorityForm : Form
    {
        public Win32PriorityForm()
        {
            InitializeComponent();
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            AppServices.Registry.WriteValue(RegistryHive.LocalMachine, @"SYSTEM\ControlSet001\Control\PriorityControl", "Win32PrioritySeparation", Convert.ToInt32(comboBox1.Text));
            await Task.Delay(100);
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\PriorityControl")?.GetValue("Win32PrioritySeparation")) == Convert.ToInt32(comboBox1.Text))
            {
                Close();
            }


        }
    }
}

