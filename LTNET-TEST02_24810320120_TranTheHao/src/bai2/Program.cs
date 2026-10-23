using System;
using System.Windows.Forms;

namespace EmployeeManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new frmEmployeeManager());
        }
    }
}
