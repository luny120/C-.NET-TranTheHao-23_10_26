using System.Drawing;
using System.Windows.Forms;

namespace SchoolManagement
{
    public class frmClassList : Form
    {
        public frmClassList()
        {
            Text = "Danh mục Lớp học";
            ClientSize = new Size(600, 360);

            var dgv = new DataGridView { Dock = DockStyle.Fill };
            GridHelper.Setup(dgv);
            GridHelper.AddCol(dgv, "Mã lớp", nameof(ClassModel.Id), 70);
            GridHelper.AddCol(dgv, "Tên lớp", nameof(ClassModel.Name), 100);
            GridHelper.AddCol(dgv, "GV chủ nhiệm", nameof(ClassModel.Homeroom), 170);
            GridHelper.AddCol(dgv, "Sĩ số", nameof(ClassModel.Size), 60);
            dgv.DataSource = DataStore.Classes;
            Controls.Add(dgv);
        }
    }
}
