using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace EmployeeManagement
{
    public class frmEmployeeManager : Form
    {
        // ====== Thanh điều khiển ======
        private MenuStrip menuStrip;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblCount, lblPayroll, lblSpring, lblTime;
        private ToolStripTextBox txtSearch;

        // ====== Khu vực chi tiết ======
        private TextBox txtId, txtFullName, txtPosition, txtSalary;
        private ComboBox cboDepartment;
        private PictureBox picAvatar;
        private Button btnChooseImage, btnAdd, btnUpdate, btnDelete, btnReset;
        private ErrorProvider errorProvider;

        // ====== Grid + dữ liệu ======
        private DataGridView dgvEmployees;
        private readonly BindingList<EmployeeModel> _employees = new BindingList<EmployeeModel>();
        private readonly BindingSource _bs = new BindingSource();

        private string _avatarPath;        // đường dẫn ảnh đang hiển thị ở khu vực nhập
        private string _currentFile;       // file CSV đang mở/lưu gần nhất
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer { Interval = 1000 };

        private const string CsvHeader = "Id,FullName,Department,Position,Salary,AvatarPath";

        public frmEmployeeManager()
        {
            BuildUI();
            SetupGrid();

            _bs.DataSource = _employees;
            dgvEmployees.DataSource = _bs;

            // Mọi thay đổi (Thêm/Sửa/Xóa/Import) đều gây ListChanged -> cập nhật StatusStrip
            _employees.ListChanged += (s, e) => UpdateStatus();
            dgvEmployees.SelectionChanged += (s, e) => ShowSelectedToInputs();

            _timer.Tick += (s, e) => lblTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            _timer.Start();
            FormClosed += (s, e) => _timer.Dispose();

            SeedData();
            ResetInputs();
            UpdateStatus();
            UpdateTitle();
        }

        // =====================================================
        // 1. GIAO DIỆN
        // =====================================================
        private void BuildUI()
        {
            Text = "Quản lý hồ sơ nhân sự";
            Size = new Size(1020, 700);
            StartPosition = FormStartPosition.CenterScreen;
            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            BuildMenu();
            BuildToolStrip();
            BuildStatusStrip();

            // --- Khu vực chi tiết ---
            var grp = new GroupBox { Text = "Thông tin nhân viên", Dock = DockStyle.Top, Height = 250, Padding = new Padding(10) };

            txtId = new TextBox { ReadOnly = true };
            txtFullName = new TextBox();
            cboDepartment = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown };
            cboDepartment.Items.AddRange(new object[] { "Kế toán", "Nhân sự", "Kỹ thuật", "Kinh doanh", "Marketing" });
            txtPosition = new TextBox();
            txtSalary = new TextBox();

            AddRow(grp, "Mã nhân viên:", txtId, 0);
            AddRow(grp, "Họ và tên:", txtFullName, 1);
            AddRow(grp, "Phòng ban:", cboDepartment, 2);
            AddRow(grp, "Chức vụ:", txtPosition, 3);
            AddRow(grp, "Lương (VNĐ):", txtSalary, 4);

            txtFullName.TextChanged += (s, e) => errorProvider.SetError(txtFullName, "");
            cboDepartment.TextChanged += (s, e) => errorProvider.SetError(cboDepartment, "");
            txtSalary.TextChanged += (s, e) => errorProvider.SetError(txtSalary, "");

            picAvatar = new PictureBox
            {
                Name = "picAvatar",
                Left = 420, Top = 28, Width = 140, Height = 170,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.WhiteSmoke
            };
            picAvatar.Paint += (s, e) =>
            {
                if (picAvatar.Image != null) return;
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString("Chưa có ảnh", Font, Brushes.Gray, picAvatar.ClientRectangle, sf);
            };
            grp.Controls.Add(picAvatar);

            btnChooseImage = new Button { Text = "Chọn ảnh", Left = 420, Top = 205, Width = 140, Height = 30 };
            btnChooseImage.Click += (s, e) => ChooseImage();
            grp.Controls.Add(btnChooseImage);

            btnAdd = MakeButton(grp, "Thêm", 0, (s, e) => AddEmployee());
            btnUpdate = MakeButton(grp, "Sửa", 1, (s, e) => UpdateEmployee());
            btnDelete = MakeButton(grp, "Xóa", 2, (s, e) => DeleteEmployee());
            btnReset = MakeButton(grp, "Làm mới", 3, (s, e) => ResetInputs());

            // --- Grid ---
            dgvEmployees = new DataGridView { Name = "dgvEmployees", Dock = DockStyle.Fill };

            // Thứ tự Add: Fill trước, rồi Top, MenuStrip cuối cùng (nằm trên cùng), StatusStrip dưới đáy
            Controls.Add(dgvEmployees);
            Controls.Add(grp);
            Controls.Add(toolStrip);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            MainMenuStrip = menuStrip;
        }

        private void AddRow(GroupBox parent, string caption, Control ctl, int row)
        {
            int y = 28 + row * 40;
            parent.Controls.Add(new Label { Text = caption, Left = 15, Top = y + 3, Width = 100 });
            ctl.Left = 120; ctl.Top = y; ctl.Width = 270;
            parent.Controls.Add(ctl);
        }

        private Button MakeButton(GroupBox parent, string text, int index, EventHandler onClick)
        {
            var b = new Button { Text = text, Left = 620, Top = 28 + index * 48, Width = 120, Height = 36 };
            b.Click += onClick;
            parent.Controls.Add(b);
            return b;
        }

        private void BuildMenu()
        {
            menuStrip = new MenuStrip();

            var mFile = new ToolStripMenuItem("&File");
            mFile.DropDownItems.Add(new ToolStripMenuItem("Mở file CSV", IconOpen(), (s, e) => OpenCsv()) { ShortcutKeys = Keys.Control | Keys.O });
            mFile.DropDownItems.Add(new ToolStripMenuItem("Lưu file CSV", IconSave(), (s, e) => SaveCsv()) { ShortcutKeys = Keys.Control | Keys.S });
            mFile.DropDownItems.Add(new ToolStripSeparator());
            mFile.DropDownItems.Add(new ToolStripMenuItem("Thoát", null, (s, e) => Close()) { ShortcutKeys = Keys.Alt | Keys.F4 });

            var mTask = new ToolStripMenuItem("&Tác vụ");
            mTask.DropDownItems.Add(new ToolStripMenuItem("Thêm", IconAdd(), (s, e) => AddEmployee()) { ShortcutKeys = Keys.Control | Keys.N });
            mTask.DropDownItems.Add(new ToolStripMenuItem("Xóa", null, (s, e) => DeleteEmployee()) { ShortcutKeys = Keys.Control | Keys.D });
            mTask.DropDownItems.Add(new ToolStripMenuItem("Tìm kiếm", IconSearch(), (s, e) => SearchFromMenu()) { ShortcutKeys = Keys.Control | Keys.F });

            menuStrip.Items.Add(mFile);
            menuStrip.Items.Add(mTask);
        }

        private void BuildToolStrip()
        {
            toolStrip = new ToolStrip { ImageScalingSize = new Size(24, 24) };

            toolStrip.Items.Add(new ToolStripButton("Mở file", IconOpen(), (s, e) => OpenCsv()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, ToolTipText = "Mở file CSV (Ctrl+O)" });
            toolStrip.Items.Add(new ToolStripButton("Lưu file", IconSave(), (s, e) => SaveCsv()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, ToolTipText = "Lưu file CSV (Ctrl+S)" });
            toolStrip.Items.Add(new ToolStripButton("Thêm mới", IconAdd(), (s, e) => { ResetInputs(); txtFullName.Focus(); }) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText, ToolTipText = "Xóa trắng form để nhập nhân viên mới" });
            toolStrip.Items.Add(new ToolStripSeparator());

            txtSearch = new ToolStripTextBox { Width = 180, ToolTipText = "Nhập mã / tên / phòng ban / chức vụ rồi nhấn Enter" };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; FindNext(); }
            };
            toolStrip.Items.Add(new ToolStripLabel("Tìm:"));
            toolStrip.Items.Add(txtSearch);
            toolStrip.Items.Add(new ToolStripButton("Tìm tiếp", IconSearch(), (s, e) => FindNext()) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText });
        }

        private void BuildStatusStrip()
        {
            statusStrip = new StatusStrip();
            lblCount = new ToolStripStatusLabel("Tổng nhân viên: 0") { BorderSides = ToolStripStatusLabelBorderSides.Right };
            lblPayroll = new ToolStripStatusLabel("Tổng quỹ lương: 0 VNĐ") { BorderSides = ToolStripStatusLabelBorderSides.Right };
            lblSpring = new ToolStripStatusLabel { Spring = true };
            lblTime = new ToolStripStatusLabel(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            statusStrip.Items.AddRange(new ToolStripItem[] { lblCount, lblPayroll, lblSpring, lblTime });
        }

        private void SetupGrid()
        {
            dgvEmployees.AutoGenerateColumns = false;
            dgvEmployees.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvEmployees.MultiSelect = false;
            dgvEmployees.ReadOnly = true;
            dgvEmployees.AllowUserToAddRows = false;
            dgvEmployees.AllowUserToDeleteRows = false;
            dgvEmployees.RowHeadersVisible = false;
            dgvEmployees.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvEmployees.Columns.Clear();
            dgvEmployees.Columns.Add(Col("colId", "Mã NV", nameof(EmployeeModel.Id), 70));
            dgvEmployees.Columns.Add(Col("colName", "Họ tên", nameof(EmployeeModel.FullName), 160));
            dgvEmployees.Columns.Add(Col("colDept", "Phòng ban", nameof(EmployeeModel.Department), 110));
            dgvEmployees.Columns.Add(Col("colPos", "Chức vụ", nameof(EmployeeModel.Position), 110));
            var sal = Col("colSalary", "Lương", nameof(EmployeeModel.Salary), 100, "N0");
            sal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvEmployees.Columns.Add(sal);
            dgvEmployees.Columns.Add(Col("colAvatar", "Ảnh (đường dẫn)", nameof(EmployeeModel.AvatarPath), 200));
        }

        private DataGridViewTextBoxColumn Col(string name, string header, string prop, float weight, string format = null)
        {
            var c = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, DataPropertyName = prop, FillWeight = weight };
            if (format != null) c.DefaultCellStyle.Format = format;
            return c;
        }

        // =====================================================
        // 2. ICON VẼ BẰNG CODE (không cần file ảnh)
        // =====================================================
        private static Bitmap MakeIcon(Action<Graphics> draw)
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                draw(g);
            }
            return bmp;
        }

        private static Bitmap IconOpen() => MakeIcon(g =>
        {
            g.FillRectangle(Brushes.DarkGoldenrod, 2, 4, 9, 4);
            g.FillRectangle(Brushes.Goldenrod, 2, 7, 20, 14);
            g.FillRectangle(Brushes.Gold, 5, 11, 17, 10);
        });

        private static Bitmap IconSave() => MakeIcon(g =>
        {
            g.FillRectangle(Brushes.SteelBlue, 3, 3, 18, 18);
            g.FillRectangle(Brushes.White, 7, 3, 10, 7);
            g.FillRectangle(Brushes.LightSteelBlue, 6, 13, 12, 8);
        });

        private static Bitmap IconAdd() => MakeIcon(g =>
        {
            g.FillEllipse(Brushes.SeaGreen, 2, 2, 20, 20);
            using (var p = new Pen(Color.White, 3))
            {
                g.DrawLine(p, 12, 6, 12, 18);
                g.DrawLine(p, 6, 12, 18, 12);
            }
        });

        private static Bitmap IconSearch() => MakeIcon(g =>
        {
            using (var p = new Pen(Color.DimGray, 2.5f))
            {
                g.DrawEllipse(p, 3, 3, 11, 11);
                g.DrawLine(p, 13, 13, 21, 21);
            }
        });

        // =====================================================
        // 3. HÀM HỖ TRỢ
        // =====================================================
        private void SeedData()
        {
            _employees.Add(new EmployeeModel { Id = "NV001", FullName = "Nguyễn Văn An", Department = "Kỹ thuật", Position = "Lập trình viên", Salary = 20000000 });
            _employees.Add(new EmployeeModel { Id = "NV002", FullName = "Trần Thị Bình", Department = "Kế toán", Position = "Kế toán trưởng", Salary = 25000000 });
            _employees.Add(new EmployeeModel { Id = "NV003", FullName = "Lê Hoàng Cường", Department = "Kinh doanh", Position = "Nhân viên", Salary = 12000000 });
        }

        private string NextId()
        {
            int max = _employees
                .Select(e => e.Id != null && e.Id.StartsWith("NV") && int.TryParse(e.Id.Substring(2), out int n) ? n : 0)
                .DefaultIfEmpty(0).Max();
            return "NV" + (max + 1).ToString("D3");
        }

        private EmployeeModel SelectedEmployee()
        {
            return dgvEmployees.SelectedRows.Count > 0
                ? dgvEmployees.SelectedRows[0].DataBoundItem as EmployeeModel
                : null;
        }

        private void UpdateStatus()
        {
            lblCount.Text = "Tổng nhân viên: " + _employees.Count;
            lblPayroll.Text = "Tổng quỹ lương: " + _employees.Sum(e => e.Salary).ToString("N0") + " VNĐ";
        }

        private void UpdateTitle()
        {
            Text = "Quản lý hồ sơ nhân sự" + (_currentFile == null ? "" : " - " + Path.GetFileName(_currentFile));
        }

        private void ClearErrors()
        {
            errorProvider.SetError(txtFullName, "");
            errorProvider.SetError(cboDepartment, "");
            errorProvider.SetError(txtSalary, "");
        }

        private void ResetInputs()
        {
            dgvEmployees.ClearSelection();
            txtId.Text = NextId();
            txtFullName.Clear();
            cboDepartment.SelectedIndex = -1;
            cboDepartment.Text = "";
            txtPosition.Clear();
            txtSalary.Clear();
            _avatarPath = null;
            LoadAvatar(null);
            ClearErrors();
        }

        // Grid -> ô nhập liệu
        private void ShowSelectedToInputs()
        {
            var emp = SelectedEmployee();
            if (emp == null) return;

            txtId.Text = emp.Id;
            txtFullName.Text = emp.FullName;
            cboDepartment.Text = emp.Department;
            txtPosition.Text = emp.Position;
            txtSalary.Text = emp.Salary.ToString("0.##", CultureInfo.CurrentCulture);
            _avatarPath = emp.AvatarPath;
            LoadAvatar(_avatarPath);
            ClearErrors();
        }

        // Nạp ảnh bằng cách copy ra bitmap để không khóa file trên đĩa
        private void LoadAvatar(string path)
        {
            var old = picAvatar.Image;
            picAvatar.Image = null;
            old?.Dispose();

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                picAvatar.Invalidate();
                return;
            }
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (var img = Image.FromStream(fs))
                    picAvatar.Image = new Bitmap(img);
            }
            catch (Exception)
            {
                picAvatar.Image = null;
            }
            picAvatar.Invalidate();
        }

        // =====================================================
        // 4. OpenFileDialog - CHỌN ẢNH
        // =====================================================
        private void ChooseImage()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn ảnh chân dung nhân viên";
                ofd.Filter = "Ảnh (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Tất cả các file (*.*)|*.*";
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                _avatarPath = ofd.FileName;
                LoadAvatar(_avatarPath);

                // Đang chọn một nhân viên -> lưu đường dẫn vào đối tượng ngay
                var emp = SelectedEmployee();
                if (emp != null) emp.AvatarPath = _avatarPath;
            }
        }

        // =====================================================
        // 5. THÊM / SỬA / XÓA / TÌM
        // =====================================================
        private bool ValidateInputs(out decimal salary)
        {
            bool ok = true;
            salary = 0;
            ClearErrors();

            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                errorProvider.SetError(txtFullName, "Họ tên không được để trống.");
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(cboDepartment.Text))
            {
                errorProvider.SetError(cboDepartment, "Vui lòng chọn hoặc nhập phòng ban.");
                ok = false;
            }
            if (!decimal.TryParse(txtSalary.Text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out salary) || salary <= 0)
            {
                errorProvider.SetError(txtSalary, "Lương phải là số lớn hơn 0.");
                ok = false;
            }
            return ok;
        }

        private void FillModel(EmployeeModel m, decimal salary)
        {
            m.FullName = txtFullName.Text.Trim();
            m.Department = cboDepartment.Text.Trim();
            m.Position = txtPosition.Text.Trim();
            m.Salary = salary;
            m.AvatarPath = _avatarPath;
        }

        private void AddEmployee()
        {
            if (!ValidateInputs(out decimal salary)) return;

            var emp = new EmployeeModel { Id = NextId() };
            FillModel(emp, salary);
            _employees.Add(emp);

            if (dgvEmployees.RowCount > 0)
                dgvEmployees.FirstDisplayedScrollingRowIndex = dgvEmployees.RowCount - 1;
            ResetInputs();
        }

        private void UpdateEmployee()
        {
            var emp = SelectedEmployee();
            if (emp == null)
            {
                MessageBox.Show("Vui lòng chọn một nhân viên trên danh sách để sửa.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateInputs(out decimal salary)) return;

            FillModel(emp, salary);
            MessageBox.Show("Cập nhật thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteEmployee()
        {
            var emp = SelectedEmployee();
            if (emp == null)
            {
                MessageBox.Show("Vui lòng chọn một nhân viên cần xóa.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rs = MessageBox.Show($"Bạn có chắc muốn xóa nhân viên {emp.Id} - {emp.FullName}?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (rs != DialogResult.Yes) return;

            _employees.Remove(emp);
            ResetInputs();
        }

        private void SearchFromMenu()
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text)) txtSearch.Focus();
            else FindNext();
        }

        private void FindNext()
        {
            string key = txtSearch.Text.Trim().ToLowerInvariant();
            if (key.Length == 0) { txtSearch.Focus(); return; }

            int n = _employees.Count;
            int start = dgvEmployees.SelectedRows.Count > 0 ? dgvEmployees.SelectedRows[0].Index : -1;

            for (int k = 1; k <= n; k++)
            {
                int i = (start + k) % n;
                var e = _employees[i];
                string hay = (e.Id + " " + e.FullName + " " + e.Department + " " + e.Position).ToLowerInvariant();
                if (hay.Contains(key))
                {
                    dgvEmployees.CurrentCell = dgvEmployees.Rows[i].Cells[0];
                    dgvEmployees.Rows[i].Selected = true;
                    return;
                }
            }
            MessageBox.Show("Không tìm thấy nhân viên phù hợp.", "Tìm kiếm",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // =====================================================
        // 6. SaveFileDialog / OpenFileDialog - CSV
        // =====================================================
        private void SaveCsv()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = "Lưu danh sách nhân sự";
                sfd.Filter = "File CSV (*.csv)|*.csv";
                sfd.DefaultExt = "csv";
                sfd.AddExtension = true;
                sfd.FileName = _currentFile != null ? Path.GetFileName(_currentFile) : "nhansu.csv";
                if (_currentFile != null) sfd.InitialDirectory = Path.GetDirectoryName(_currentFile);
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    var lines = new List<string> { CsvHeader };
                    foreach (var e in _employees)
                    {
                        lines.Add(string.Join(",",
                            CsvEscape(e.Id),
                            CsvEscape(e.FullName),
                            CsvEscape(e.Department),
                            CsvEscape(e.Position),
                            e.Salary.ToString(CultureInfo.InvariantCulture),
                            CsvEscape(e.AvatarPath)));
                    }
                    // UTF-8 có BOM để Excel đọc đúng tiếng Việt
                    File.WriteAllLines(sfd.FileName, lines, new UTF8Encoding(true));

                    _currentFile = sfd.FileName;
                    UpdateTitle();
                    MessageBox.Show($"Đã lưu {_employees.Count} nhân viên vào file:\n{sfd.FileName}", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể lưu file:\n" + ex.Message, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OpenCsv()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Mở file danh sách nhân sự";
                ofd.Filter = "File CSV (*.csv)|*.csv|Tất cả các file (*.*)|*.*";
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    var loaded = new List<EmployeeModel>();
                    var errors = new List<int>();
                    string[] lines = File.ReadAllLines(ofd.FileName, Encoding.UTF8);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (i == 0 && line.TrimStart('\uFEFF').StartsWith("Id,", StringComparison.OrdinalIgnoreCase)) continue; // dòng tiêu đề

                        var f = ParseCsvLine(line);
                        if (f.Count < 5 || !decimal.TryParse(f[4], NumberStyles.Number, CultureInfo.InvariantCulture, out decimal salary))
                        {
                            errors.Add(i + 1);
                            continue;
                        }
                        loaded.Add(new EmployeeModel
                        {
                            Id = f[0].Trim(),
                            FullName = f[1].Trim(),
                            Department = f[2].Trim(),
                            Position = f[3].Trim(),
                            Salary = salary,
                            AvatarPath = f.Count > 5 && f[5].Trim().Length > 0 ? f[5].Trim() : null
                        });
                    }

                    // Nạp vào BindingList (tắt thông báo khi nạp hàng loạt rồi Reset 1 lần)
                    _employees.RaiseListChangedEvents = false;
                    _employees.Clear();
                    foreach (var emp in loaded) _employees.Add(emp);
                    _employees.RaiseListChangedEvents = true;
                    _employees.ResetBindings();

                    _currentFile = ofd.FileName;
                    UpdateTitle();
                    ResetInputs();

                    string msg = $"Đã nạp {loaded.Count} nhân viên.";
                    if (errors.Count > 0)
                        msg += $"\nBỏ qua {errors.Count} dòng lỗi (dòng: {string.Join(", ", errors.Take(10))}{(errors.Count > 10 ? ", ..." : "")}).";
                    MessageBox.Show(msg, "Kết quả mở file", MessageBoxButtons.OK,
                        errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _employees.RaiseListChangedEvents = true;
                    MessageBox.Show("Không thể đọc file:\n" + ex.Message, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Bọc giá trị trong "..." nếu chứa dấu phẩy, nháy kép hoặc xuống dòng
        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace("\r", " ").Replace("\n", " ");
            if (value.IndexOfAny(new[] { ',', '"' }) >= 0)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }

        // Tách 1 dòng CSV, hỗ trợ trường nằm trong "..." và "" là dấu nháy kép
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            fields.Add(sb.ToString());
            return fields;
        }
    }
}
