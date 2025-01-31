using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WOTBO
{
    public partial class Form2 : Form
    {
        string exepath = Assembly.GetEntryAssembly().Location;
        string exename = AppDomain.CurrentDomain.FriendlyName;
        public Form2()
        {
            InitializeComponent();
        }

        int k = 0;
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
        private void Form2_Load(object sender, EventArgs e)
        {
            TopMost = true;
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo") == null)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\oixro\wotbo");
            }

            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\out") != null)
            {
                MessageBox.Show("В версии 0.959 был изменён путь к реестру, будет выполнена очитска данных","",MessageBoxButtons.OK, MessageBoxIcon.Information);
                Registry.CurrentUser.DeleteSubKeyTree(@"Software\oixro\out");
                hcmd($"taskkill /f /im \"{exename}\" & timeout /t 1 && \"{exepath}\"");
            }
            {
                label1.Text = $"Перед использованием программы прочитай:\n" +
"1. Программа не является идеальной, и не обязана дать вам 5000fps на днищенском компе\n" +
"2. Я не несу никакой отвественности за работоспособность вашего комьютера, при возникновении проблем - виноват ТЫ!\n" +
"3. При возникновении вопросов сначала нужно посмотреть гайд на канале, тупые вопросы в личку будут игнорироваться!\n" +
"4. Если ответа в видео нет - скинь полное описание своего компьютера и ситуацию при которой возникает проблема!\n" +
"5. На фразу \"не работает!\" - cразу игнор! Уважайте своё и моё время.\n" +
"6. Если ты мне напишешь - \"Я НАЖЫМАЮ НА ПРИНЯТЬ, ОНО НЕ РАБОТАЕТ\" - читай лучше этот текст!\n" +
$"7. Чтобы принять - нажмите правой кнопкой мыши по {button1.Text}" +
"\n8.За большинство настроек спасибо ТехноШахте - (discord.gg/GUC7ckWtmn)" +
"\n9. И Win 10 Tweaker (win10tweaker.ru)\n" +
"10. Работоспособность проверна только на оригинальных версиях Windows\n" +
"11. За ошибки на сборках, отличных от оригинальных я ответсвенности не несу!";
            }
        }

        private void button1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                k++;
                if (k >= 2)
                {
                        MessageBox.Show("Иди читай");
                }

            }
            if (e.Button == MouseButtons.Right)
            {
                Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo", true).SetValue("eula", 1);
                Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            hcmd($"taskkill /f /im \"{exename}\"");
        }
    }
}
