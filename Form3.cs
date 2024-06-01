using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
            if (!Directory.Exists(path_NVidiaProfileInspectorDmW))
            {
                File.WriteAllBytes(tempfolder + @"\NVidiaProfileInspectorDmW.zip", Resources.NVidiaProfileInspectorDmW);
                ZipFile.ExtractToDirectory(tempfolder + @"\NVidiaProfileInspectorDmW.zip", tempfolder);
            }

        }
        private void button1_Click(object sender, EventArgs e)
        {
            hcmd($"{path_NVidiaProfileInspectorDmW}\\nvidiaProfileInspector.exe \"{path_NVidiaProfileInspectorDmW}\\no ao 144fps+.nip\"");
            Close();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            hcmd($"{path_NVidiaProfileInspectorDmW}\\nvidiaProfileInspector.exe \"{path_NVidiaProfileInspectorDmW}\\new custom 100 fps.nip\"");
            Close();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            hcmd($"{path_NVidiaProfileInspectorDmW}\\nvidiaProfileInspector.exe \"{path_NVidiaProfileInspectorDmW}\\new custom 144 fps.nip\"");
            Close();
        }
        private void button4_Click(object sender, EventArgs e)
        {
            hcmd($"{path_NVidiaProfileInspectorDmW}\\nvidiaProfileInspector.exe \"{path_NVidiaProfileInspectorDmW}\\new custom 144+ fps.nip\"");
            Close();
        }
    }
}
