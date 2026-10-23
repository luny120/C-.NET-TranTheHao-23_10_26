using System;
using System.Windows.Forms;

namespace SchoolManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Vòng lặp đăng nhập: "Đăng xuất" trên form MDI sẽ quay lại màn hình đăng nhập.
            while (true)
            {
                using (var login = new frmLogin())
                {
                    if (login.ShowDialog() != DialogResult.OK) return;
                }

                var main = new frmSchoolMain();
                Application.Run(main);
                if (!main.LoggedOut) return;   // đóng cửa sổ/Thoát -> kết thúc chương trình
            }
        }
    }
}
