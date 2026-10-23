using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement
{
    public class frmTeacherList : Form
    {
        public frmTeacherList()
        {
            Text = "Danh sách Giảng viên";
            ClientSize = new Size(640, 380);

            var dgv = new DataGridView { Dock = DockStyle.Fill };
            GridHelper.Setup(dgv);
            GridHelper.AddCol(dgv, "Mã GV", nameof(TeacherModel.Id), 70);
            GridHelper.AddCol(dgv, "Họ tên", nameof(TeacherModel.FullName), 160);
            GridHelper.AddCol(dgv, "Khoa/Bộ môn", nameof(TeacherModel.Department), 140);
            GridHelper.AddCol(dgv, "Email", nameof(TeacherModel.Email), 170);
            dgv.DataSource = DataStore.Teachers;
            Controls.Add(dgv);
        }
    }
}
