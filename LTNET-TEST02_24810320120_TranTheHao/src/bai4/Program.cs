using System;
using System.Windows.Forms;

namespace WarehouseAnalytics
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new frmWarehouseAnalytics());
        }
    }
}
