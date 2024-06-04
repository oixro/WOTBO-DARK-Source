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
            Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\PriorityControl", true).SetValue("Win32PrioritySeparation", Convert.ToInt32(comboBox1.Text));
            await Task.Delay(100);
            if (Convert.ToInt32(Registry.LocalMachine.OpenSubKey(@"SYSTEM\ControlSet001\Control\PriorityControl").GetValue("Win32PrioritySeparation")) == Convert.ToInt32(comboBox1.Text))
            {
                Close();
                //DialogResult result = MessageBox.Show("Применено! Закрыть окно?", "Win32PrioritySeparation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                //if (result == DialogResult.Yes)
                //{
                //    Close();
                //}
            }


        }

        private void Win32Priority_Load(object sender, EventArgs e)
        {
            if ((Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo").GetValue("Language").ToString()) == "en")
            {
                //MessageBox.Show("не null ебать и мы en");
                label1.Text = "Win32PrioritySeparation - parameter allowing to customize the amount of time allocated to background and active processes.\r\n" +
                    "It directly affects the “input delay”.\r\n" +
                    "There are several options of values that should be chosen depending on your needs.\r\n" +
                    "There is no definite answer as to which option is best, so feel free to try different options.\r\n" +
                    "No reboot is required, so you can leave the program open and try different values while the game is open.\r\n\r\n" +
                    "40 - will provide the most responsive input at the expense of a slight FPS drop.\r\n" +
                    "21 - will provide the smoothest gameplay.\r\n" +
                    "Values 21, 22, 24, 37, 38, 40 will shift the “balance” to one side.\r\n" +
                    "2 - Windows default value.\r\n\r\n" +
                    "Select the desired value in the field below and click apply.\r\n\r\n\r\n";
                button1.Text = "Apply";
            }
        }
    }
}

