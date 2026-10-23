using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SchoolManagement
{
    public class frmStudentList : Form
    {
        private readonly DataGridView dgvStudents = new DataGridView { Dock = DockStyle.Fill };
        private readonly BindingSource _bs = new BindingSource();
        private readonly TextBox txtId = new TextBox();
        private readonly TextBox txtName = new TextBox();
        private readonly ComboBox cboClass = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DateTimePicker dtpBirth = new DateTimePicker { Format = DateTimePickerFormat.Short };
        private readonly ErrorProvider errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        public frmStudentList()
        {
            Text = "Danh sách Sinh viên";
            ClientSize = new Size(720, 440);

            var pnl = new Panel { Dock = DockStyle.Top, Height = 125 };
            AddField(pnl, "Mã SV:", txtId, 15, 15, 140);
            AddField(pnl, "Họ tên:", txtName, 250, 15, 250);
            AddField(pnl, "Lớp:", cboClass, 15, 52, 140);
            AddField(pnl, "Ngày sinh:", dtpBirth, 250, 52, 140);
            cboClass.Items.AddRange(DataStore.Classes.Select(c => (object)c.Name).ToArray());

            AddButton(pnl, "Thêm", 15, (s, e) => AddStudent());
            AddButton(pnl, "Sửa", 105, (s, e) => UpdateStudent());
            AddButton(pnl, "Xóa", 195, (s, e) => DeleteStudent());
            AddButton(pnl, "Làm mới", 285, (s, e) => ResetInputs());

            txtId.TextChanged += (s, e) => errorProvider.SetError(txtId, "");
            txtName.TextChanged += (s, e) => errorProvider.SetError(txtName, "");
            cboClass.SelectedIndexChanged += (s, e) => errorProvider.SetError(cboClass, "");

            GridHelper.Setup(dgvStudents);
            GridHelper.AddCol(dgvStudents, "Mã SV", nameof(StudentModel.Id), 70);
            GridHelper.AddCol(dgvStudents, "Họ tên", nameof(StudentModel.FullName), 170);
            GridHelper.AddCol(dgvStudents, "Lớp", nameof(StudentModel.ClassName), 90);
            GridHelper.AddCol(dgvStudents, "Ngày sinh", nameof(StudentModel.BirthDate), 100, "dd/MM/yyyy");
            _bs.DataSource = DataStore.Students;
            dgvStudents.DataSource = _bs;
            dgvStudents.SelectionChanged += (s, e) => ShowSelected();

            Controls.Add(dgvStudents);   // Fill thêm trước, Top thêm sau
            Controls.Add(pnl);

            Shown += (s, e) => ResetInputs();
        }

        private static void AddField(Panel p, string caption, Control c, int x, int y, int w)
        {
            p.Controls.Add(new Label { Text = caption, Left = x, Top = y + 3, Width = 65 });
            c.SetBounds(x + 70, y, w, 25);
            p.Controls.Add(c);
        }

        private static void AddButton(Panel p, string text, int x, EventHandler onClick)
        {
            var b = new Button { Text = text, Left = x, Top = 88, Width = 80, Height = 28 };
            b.Click += onClick;
            p.Controls.Add(b);
        }

        private StudentModel Selected()
        {
            return dgvStudents.SelectedRows.Count > 0
                ? dgvStudents.SelectedRows[0].DataBoundItem as StudentModel : null;
        }

        private void ShowSelected()
        {
            var st = Selected();
            if (st == null) return;
            txtId.Text = st.Id;
            txtId.ReadOnly = true;             // không cho đổi mã khi đang sửa
            txtName.Text = st.FullName;
            cboClass.SelectedItem = st.ClassName;
            dtpBirth.Value = st.BirthDate;
        }

        private void ResetInputs()
        {
            dgvStudents.ClearSelection();
            txtId.ReadOnly = false;
            txtId.Clear();
            txtName.Clear();
            cboClass.SelectedIndex = cboClass.Items.Count > 0 ? 0 : -1;
            dtpBirth.Value = new DateTime(2005, 1, 1);
            errorProvider.Clear();
            txtId.Focus();
        }

        private bool ValidateInput(bool isNew)
        {
            bool ok = true;
            errorProvider.Clear();

            if (isNew)
            {
                string id = txtId.Text.Trim();
                if (id.Length == 0)
                {
                    errorProvider.SetError(txtId, "Mã sinh viên không được để trống.");
                    ok = false;
                }
                else if (DataStore.Students.Any(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)))
                {
                    errorProvider.SetError(txtId, "Mã sinh viên đã tồn tại.");
                    ok = false;
                }
            }
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                errorProvider.SetError(txtName, "Họ tên không được để trống.");
                ok = false;
            }
            if (cboClass.SelectedIndex < 0)
            {
                errorProvider.SetError(cboClass, "Vui lòng chọn lớp.");
                ok = false;
            }
            return ok;
        }

        private void AddStudent()
        {
            if (Selected() != null)
            {
                MessageBox.Show("Đang chọn một sinh viên. Hãy bấm \"Làm mới\" trước khi thêm sinh viên mới.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateInput(true)) return;

            DataStore.Students.Add(new StudentModel
            {
                Id = txtId.Text.Trim().ToUpper(),
                FullName = txtName.Text.Trim(),
                ClassName = cboClass.SelectedItem.ToString(),
                BirthDate = dtpBirth.Value.Date
            });
            ResetInputs();
        }

        private void UpdateStudent()
        {
            var st = Selected();
            if (st == null)
            {
                MessageBox.Show("Vui lòng chọn một sinh viên để sửa.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateInput(false)) return;

            st.FullName = txtName.Text.Trim();
            st.ClassName = cboClass.SelectedItem.ToString();
            st.BirthDate = dtpBirth.Value.Date;
            _bs.ResetItem(dgvStudents.SelectedRows[0].Index);   // làm mới đúng dòng đang sửa
        }

        private void DeleteStudent()
        {
            var st = Selected();
            if (st == null)
            {
                MessageBox.Show("Vui lòng chọn một sinh viên cần xóa.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var rs = MessageBox.Show($"Xóa sinh viên {st.Id} - {st.FullName}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (rs != DialogResult.Yes) return;

            DataStore.Students.Remove(st);
            ResetInputs();
        }
    }
}
