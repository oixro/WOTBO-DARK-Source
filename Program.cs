using System;
using System.Windows.Forms;
using WOTBO.App;

namespace WOTBO
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
