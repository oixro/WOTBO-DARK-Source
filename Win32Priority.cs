using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WOTBO
{
    public partial class Win32Priority : Form
    {
        public Win32Priority()
        {
            InitializeComponent();
        }
        private async void button1_Click(object sender, EventArgs e)
        {
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\PriorityControl", true)?.SetValue("Win32PrioritySeparation", Convert.ToInt32(comboBox1.Text));
            await Task.Delay(100);
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\PriorityControl")?.GetValue("Win32PrioritySeparation")) == Convert.ToInt32(comboBox1.Text))
            {
                Close();
            }


        }
    }
}

