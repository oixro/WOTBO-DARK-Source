using System;
using System.Windows.Forms;
using WOTBO.App;

namespace test1
{
    internal static class Program
    {
        /// <summary>Главная точка входа для приложения.</summary>
        [STAThread]
        private static void Main()
        {
            AppBootstrap.Run();
        }
    }
}
