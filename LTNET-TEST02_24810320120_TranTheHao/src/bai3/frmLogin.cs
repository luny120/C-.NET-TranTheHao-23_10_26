using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement
{
    public class frmLogin : Form
    {
        private readonly TextBox txtUser = new TextBox();
        private readonly TextBox txtPass = new TextBox { UseSystemPasswordChar = true };
        private readonly Label lblError = new Label { ForeColor = Color.Firebrick, AutoSize = false };

        public frmLogin()
        {
            Text = "Đăng nhập hệ thống";
            ClientSize = new Size(340, 235);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            Controls.Add(new Label { Text = "QUẢN LÝ TRƯỜNG HỌC", Left = 0, Top = 15, Width = 340, Height = 28,
                TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font.FontFamily, 13f, FontStyle.Bold) });

            Controls.Add(new Label { Text = "Tài khoản:", Left = 25, Top = 62, Width = 80 });
            txtUser.SetBounds(110, 58, 205, 25);
            txtUser.Text = AuthService.UserName;
            Controls.Add(txtUser);

            Controls.Add(new Label { Text = "Mật khẩu:", Left = 25, Top = 98, Width = 80 });
            txtPass.SetBounds(110, 94, 205, 25);
            Controls.Add(txtPass);

            lblError.SetBounds(25, 128, 290, 20);
            Controls.Add(lblError);

            var btnLogin = new Button { Text = "Đăng nhập", Left = 110, Top = 155, Width = 100, Height = 32 };
            var btnCancel = new Button { Text = "Thoát", Left = 215, Top = 155, Width = 100, Height = 32, DialogResult = DialogResult.Cancel };
            btnLogin.Click += (s, e) => DoLogin();
            Controls.Add(btnLogin);
            Controls.Add(btnCancel);
            AcceptButton = btnLogin;
            CancelButton = btnCancel;

            Controls.Add(new Label { Text = "Mặc định: admin / 123456", Left = 0, Top = 200, Width = 340,
                TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray });

            Shown += (s, e) => txtPass.Focus();
        }

        private void DoLogin()
        {
            if (AuthService.Login(txtUser.Text, txtPass.Text))
            {
                DialogResult = DialogResult.OK;
            }
            else
            {
                lblError.Text = "Sai tài khoản hoặc mật khẩu.";
                txtPass.Clear();
                txtPass.Focus();
            }
        }
    }
}
