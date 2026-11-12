using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using WOTBO.UI;
using WOTBO.App;

namespace WOTBO
{
    public partial class EulaForm : Form
    {
        string exepath = Assembly.GetEntryAssembly().Location;
        string exename = AppDomain.CurrentDomain.FriendlyName;
        public EulaForm()
        {
            InitializeComponent();
            Theme();
        }

        private void Theme()
        {
            BackColor = Color.FromArgb(35, 44, 55);
            FormPainter.Apply(this, Color.FromArgb(44, 57, 67), Color.FromArgb(35, 44, 55));

            Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 204);
            label1.Font = Font;
            label1.BackColor = Color.Transparent;
            label1.ForeColor = Color.White;

            StyleButton(button1);
            StyleButton(button2);
        }

        private static void StyleButton(Button button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(120, 137, 150);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 52, 59);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(33, 46, 52);
            button.BackColor = Color.FromArgb(44, 57, 67);
            button.ForeColor = Color.White;
            button.UseVisualStyleBackColor = false;
        }

        int k = 0;
        void hcmd(string line)
        {
            AppServices.Processes.StartDetached(line, true);
        }
        private void EulaForm_Load(object sender, EventArgs e)
        {
            TopMost = true;
            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\wotbo") == null)
            {
                Registry.CurrentUser.CreateSubKey(@"Software\oixro\wotbo");
            }

            if (Registry.CurrentUser.OpenSubKey(@"Software\oixro\out") != null)
            {
                MessageBox.Show("В версии 0.959 был изменён путь к реестру, будет выполнена очитска данных", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppServices.Registry.DeleteKey(RegistryHive.CurrentUser, @"Software\oixro\out");
                hcmd($"taskkill /f /im \"{exename}\" & timeout /t 1 && \"{exepath}\"");
            }
            {
                label1.Text = "Перед использованием WOTBO прочитайте это:\n\n" +
"1. WOTBO — не волшебная кнопка и не обещает «+5000 FPS»: результат зависит от системы.\n" +
"2. Все изменения вы вносите на свой страх и риск. За стабильность и сохранность данных после твиков автор не отвечает.\n" +
"3. Сначала посмотрите гайд на канале: вопросы, ответ на которые уже есть в видео, останутся без ответа.\n" +
"4. Если гайд не помог — опишите конфигурацию ПК и что именно вы делали перед появлением проблемы. Без этих деталей разобраться невозможно.\n" +
"5. Сообщения в стиле «не работает» без описания проблемы игнорируются. Уважайте своё и чужое время.\n" +
$"6. Если кнопка «{button1.Text}» не срабатывает — перечитайте этот текст.\n" +
"7. Принять EULA можно правым кликом по кнопке.\n" +
"8. WOTBO проверялся только на оригинальных версиях Windows. На кастомных сборках возможны ошибки, автор за их последствия не отвечает.";
            }
        }

        private void button1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                k++;
                if (k >= 2)
                {
                    MessageBox.Show("Пожалуйста, прочитайте условия использования WOTBO", "WOTBO", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            if (e.Button == MouseButtons.Right)
            {
                AppServices.Registry.WriteValue(RegistryHive.CurrentUser, @"Software\oixro\wotbo", "eula", 1);
                Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            hcmd($"taskkill /f /im \"{exename}\"");
        }
    }
}
