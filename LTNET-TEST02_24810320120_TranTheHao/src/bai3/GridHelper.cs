using System.Windows.Forms;

namespace SchoolManagement
{
    public static class GridHelper
    {
        // Cấu hình chuẩn: khai báo cột bằng code, chọn nguyên dòng, chỉ đọc.
        public static void Setup(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.RowHeadersVisible = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.Columns.Clear();
        }

        public static void AddCol(DataGridView dgv, string header, string prop, float weight = 100, string format = null)
        {
            var c = new DataGridViewTextBoxColumn
            {
                Name = "col" + prop,
                HeaderText = header,
                DataPropertyName = prop,
                FillWeight = weight
            };
            if (format != null) c.DefaultCellStyle.Format = format;
            dgv.Columns.Add(c);
        }
    }
}
