using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement
{
    public class frmChangePassword : Form
    {
        private readonly TextBox txtOld = new TextBox { UseSystemPasswordChar = true };
        private readonly TextBox txtNew = new TextBox { UseSystemPasswordChar = true };
        private readonly TextBox txtConfirm = new TextBox { UseSystemPasswordChar = true };
        private readonly ErrorProvider errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        public frmChangePassword()
        {
            Text = "Đổi mật khẩu";
            ClientSize = new Size(380, 215);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            AddRow("Mật khẩu hiện tại:", txtOld, 0);
            AddRow("Mật khẩu mới:", txtNew, 1);
            AddRow("Nhập lại mật khẩu:", txtConfirm, 2);

            txtOld.TextChanged += (s, e) => errorProvider.SetError(txtOld, "");
            txtNew.TextChanged += (s, e) => errorProvider.SetError(txtNew, "");
            txtConfirm.TextChanged += (s, e) => errorProvider.SetError(txtConfirm, "");

            var btnOk = new Button { Text = "Đổi mật khẩu", Left = 130, Top = 165, Width = 120, Height = 32 };
            var btnCancel = new Button { Text = "Hủy", Left = 260, Top = 165, Width = 100, Height = 32, DialogResult = DialogResult.Cancel };
            btnOk.Click += (s, e) => Save();
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void AddRow(string caption, TextBox box, int row)
        {
            int y = 25 + row * 42;
            Controls.Add(new Label { Text = caption, Left = 20, Top = y + 3, Width = 125 });
            box.SetBounds(150, y, 200, 25);
            Controls.Add(box);
        }

        private void Save()
        {
            bool ok = true;
            errorProvider.SetError(txtOld, "");
            errorProvider.SetError(txtNew, "");
            errorProvider.SetError(txtConfirm, "");

            if (!AuthService.CheckPassword(txtOld.Text))
            {
                errorProvider.SetError(txtOld, "Mật khẩu hiện tại không đúng.");
                ok = false;
            }
            if (txtNew.Text.Length < 6)
            {
                errorProvider.SetError(txtNew, "Mật khẩu mới phải có ít nhất 6 ký tự.");
                ok = false;
            }
            else if (txtNew.Text == txtOld.Text)
            {
                errorProvider.SetError(txtNew, "Mật khẩu mới phải khác mật khẩu hiện tại.");
                ok = false;
            }
            if (txtConfirm.Text != txtNew.Text)
            {
                errorProvider.SetError(txtConfirm, "Mật khẩu nhập lại không khớp.");
                ok = false;
            }
            if (!ok) return;

            AuthService.ChangePassword(txtNew.Text);
            MessageBox.Show("Đổi mật khẩu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
        }
    }
}
