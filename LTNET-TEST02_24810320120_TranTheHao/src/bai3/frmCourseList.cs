using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SchoolManagement
{
    public class frmCourseList : Form
    {
        private readonly DataGridView dgvCourses = new DataGridView { Dock = DockStyle.Fill };
        private readonly BindingSource _bs = new BindingSource();
        private readonly TextBox txtId = new TextBox();
        private readonly TextBox txtName = new TextBox();
        private readonly NumericUpDown numCredits = new NumericUpDown { Minimum = 1, Maximum = 10, Value = 3 };
        private readonly ErrorProvider errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

        public frmCourseList()
        {
            Text = "Danh mục Môn học";
            ClientSize = new Size(640, 400);

            var pnl = new Panel { Dock = DockStyle.Top, Height = 125 };
            AddField(pnl, "Mã MH:", txtId, 15, 15, 140);
            AddField(pnl, "Tên môn:", txtName, 250, 15, 280);
            AddField(pnl, "Số tín chỉ:", numCredits, 15, 52, 80);

            AddButton(pnl, "Thêm", 15, (s, e) => AddCourse());
            AddButton(pnl, "Sửa", 105, (s, e) => UpdateCourse());
            AddButton(pnl, "Xóa", 195, (s, e) => DeleteCourse());
            AddButton(pnl, "Làm mới", 285, (s, e) => ResetInputs());

            txtId.TextChanged += (s, e) => errorProvider.SetError(txtId, "");
            txtName.TextChanged += (s, e) => errorProvider.SetError(txtName, "");

            GridHelper.Setup(dgvCourses);
            GridHelper.AddCol(dgvCourses, "Mã MH", nameof(CourseModel.Id), 70);
            GridHelper.AddCol(dgvCourses, "Tên môn học", nameof(CourseModel.Name), 220);
            GridHelper.AddCol(dgvCourses, "Số tín chỉ", nameof(CourseModel.Credits), 70);
            _bs.DataSource = DataStore.Courses;
            dgvCourses.DataSource = _bs;
            dgvCourses.SelectionChanged += (s, e) => ShowSelected();

            Controls.Add(dgvCourses);
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

        private CourseModel Selected()
        {
            return dgvCourses.SelectedRows.Count > 0
                ? dgvCourses.SelectedRows[0].DataBoundItem as CourseModel : null;
        }

        private void ShowSelected()
        {
            var c = Selected();
            if (c == null) return;
            txtId.Text = c.Id;
            txtId.ReadOnly = true;
            txtName.Text = c.Name;
            numCredits.Value = c.Credits;
        }

        private void ResetInputs()
        {
            dgvCourses.ClearSelection();
            txtId.ReadOnly = false;
            txtId.Clear();
            txtName.Clear();
            numCredits.Value = 3;
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
                    errorProvider.SetError(txtId, "Mã môn học không được để trống.");
                    ok = false;
                }
                else if (DataStore.Courses.Any(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)))
                {
                    errorProvider.SetError(txtId, "Mã môn học đã tồn tại.");
                    ok = false;
                }
            }
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                errorProvider.SetError(txtName, "Tên môn học không được để trống.");
                ok = false;
            }
            return ok;
        }

        private void AddCourse()
        {
            if (Selected() != null)
            {
                MessageBox.Show("Đang chọn một môn học. Hãy bấm \"Làm mới\" trước khi thêm môn mới.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateInput(true)) return;

            DataStore.Courses.Add(new CourseModel
            {
                Id = txtId.Text.Trim().ToUpper(),
                Name = txtName.Text.Trim(),
                Credits = (int)numCredits.Value
            });
            ResetInputs();
        }

        private void UpdateCourse()
        {
            var c = Selected();
            if (c == null)
            {
                MessageBox.Show("Vui lòng chọn một môn học để sửa.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateInput(false)) return;

            c.Name = txtName.Text.Trim();
            c.Credits = (int)numCredits.Value;
            _bs.ResetItem(dgvCourses.SelectedRows[0].Index);
        }

        private void DeleteCourse()
        {
            var c = Selected();
            if (c == null)
            {
                MessageBox.Show("Vui lòng chọn một môn học cần xóa.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var rs = MessageBox.Show($"Xóa môn học {c.Id} - {c.Name}?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (rs != DialogResult.Yes) return;

            DataStore.Courses.Remove(c);
            ResetInputs();
        }
    }
}
