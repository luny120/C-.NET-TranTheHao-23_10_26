using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SchoolManagement
{
    public class frmSchoolMain : Form
    {
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblUser, lblStatus, lblWindows;
        private ToolStripProgressBar progressBar;
        private readonly List<ToolStripItem> _windowCommands = new List<ToolStripItem>();
        private readonly System.Windows.Forms.Timer _progressTimer = new System.Windows.Forms.Timer { Interval = 20 };
        private int _ticks;

        // true khi người dùng chọn "Đăng xuất" -> Program quay lại màn hình đăng nhập
        public bool LoggedOut { get; private set; }

        public frmSchoolMain()
        {
            Text = "Hệ thống quản lý trường học & đào tạo";
            IsMdiContainer = true;                       // Form cha MDI
            Size = new Size(1100, 720);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;

            BuildMenu();
            BuildStatusStrip();

            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            MainMenuStrip = menuStrip;

            _progressTimer.Tick += ProgressTick;
            MdiChildActivate += (s, e) => UpdateWindowCount();   // chạy khi mở/đóng/chuyển Form con
            FormClosing += OnFormClosing;
            FormClosed += (s, e) => _progressTimer.Dispose();
            UpdateWindowCount();
        }

        // =====================================================
        // MENU
        // =====================================================
        private void BuildMenu()
        {
            menuStrip = new MenuStrip();

            var mSystem = new ToolStripMenuItem("&Hệ thống");
            mSystem.DropDownItems.Add("Đăng xuất", null, (s, e) => Logout());
            mSystem.DropDownItems.Add("Đổi mật khẩu", null, (s, e) => ChangePassword());
            mSystem.DropDownItems.Add(new ToolStripSeparator());
            mSystem.DropDownItems.Add("Thoát", null, (s, e) => Close());

            var mManage = new ToolStripMenuItem("&Quản lý");
            mManage.DropDownItems.Add("Danh sách Sinh viên", null, (s, e) => OpenChild<frmStudentList>());
            mManage.DropDownItems.Add("Danh sách Giảng viên", null, (s, e) => OpenChild<frmTeacherList>());
            mManage.DropDownItems.Add("Danh mục Lớp học", null, (s, e) => OpenChild<frmClassList>());
            mManage.DropDownItems.Add("Danh mục Môn học", null, (s, e) => OpenChild<frmCourseList>());

            var mWindow = new ToolStripMenuItem("&Cửa sổ");
            _windowCommands.Add(mWindow.DropDownItems.Add("Sắp xếp xếp tầng (Cascade)", null, (s, e) => ArrangeWindows(MdiLayout.Cascade)));
            _windowCommands.Add(mWindow.DropDownItems.Add("Sắp xếp ngang (Tile Horizontal)", null, (s, e) => ArrangeWindows(MdiLayout.TileHorizontal)));
            _windowCommands.Add(mWindow.DropDownItems.Add("Sắp xếp dọc (Tile Vertical)", null, (s, e) => ArrangeWindows(MdiLayout.TileVertical)));
            mWindow.DropDownItems.Add(new ToolStripSeparator());
            _windowCommands.Add(mWindow.DropDownItems.Add("Đóng tất cả", null, (s, e) => CloseAllChildren()));
            // Chỉ cho dùng các lệnh sắp xếp khi đang có Form con
            mWindow.DropDownOpening += (s, e) =>
            {
                foreach (var item in _windowCommands) item.Enabled = MdiChildren.Length > 0;
            };

            menuStrip.Items.AddRange(new ToolStripItem[] { mSystem, mManage, mWindow });
            menuStrip.MdiWindowListItem = mWindow;   // tự liệt kê các cửa sổ con đang mở cuối menu Cửa sổ
        }

        private void BuildStatusStrip()
        {
            statusStrip = new StatusStrip();
            lblUser = new ToolStripStatusLabel("Cán bộ quản lý: " + AuthService.FullName + " (" + AuthService.UserName + ")")
            { BorderSides = ToolStripStatusLabelBorderSides.Right };
            lblStatus = new ToolStripStatusLabel("Sẵn sàng") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            lblWindows = new ToolStripStatusLabel { BorderSides = ToolStripStatusLabelBorderSides.Left };
            progressBar = new ToolStripProgressBar { Width = 140, Minimum = 0, Maximum = 100 };
            statusStrip.Items.AddRange(new ToolStripItem[] { lblUser, lblStatus, lblWindows, progressBar });
        }

        // =====================================================
        // QUẢN LÝ FORM CON MDI
        // =====================================================
        // Mở Form con kiểu T: đã mở thì Activate(), chưa mở thì tạo mới.
        private void OpenChild<T>() where T : Form, new()
        {
            // 1. Tìm trong danh sách MdiChildren
            T existing = MdiChildren.OfType<T>().FirstOrDefault();

            if (existing != null)
            {
                // 2. Đã mở -> đưa lên trước (khôi phục nếu đang thu nhỏ)
                if (existing.WindowState == FormWindowState.Minimized)
                    existing.WindowState = FormWindowState.Normal;
                existing.Activate();
                RunProgress("Đang hiển thị: " + existing.Text);
                return;
            }

            // 3. Chưa mở -> tạo Instance mới, gán MdiParent rồi Show()
            var child = new T { MdiParent = this };
            child.Show();
            RunProgress("Đã mở: " + child.Text);
        }

        private void ArrangeWindows(MdiLayout layout)
        {
            if (MdiChildren.Length == 0) return;

            // Form con đang Maximized sẽ không xếp được -> đưa về Normal trước
            foreach (Form f in MdiChildren)
                if (f.WindowState == FormWindowState.Maximized)
                    f.WindowState = FormWindowState.Normal;

            LayoutMdi(layout);
        }

        private void CloseAllChildren()
        {
            foreach (Form f in MdiChildren.ToArray())
                f.Close();
            RunProgress("Đã đóng tất cả cửa sổ");
        }

        private void UpdateWindowCount()
        {
            lblWindows.Text = "Cửa sổ đang mở: " + MdiChildren.Length;
        }

        // =====================================================
        // HỆ THỐNG
        // =====================================================
        private void ChangePassword()
        {
            using (var f = new frmChangePassword())
                f.ShowDialog(this);
        }

        private void Logout()
        {
            var rs = MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Đăng xuất",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (rs != DialogResult.Yes) return;

            LoggedOut = true;
            Close();    // Program.cs sẽ hiện lại form đăng nhập
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (LoggedOut || e.CloseReason != CloseReason.UserClosing) return;

            var rs = MessageBox.Show("Bạn có chắc muốn thoát chương trình?", "Thoát",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (rs != DialogResult.Yes) e.Cancel = true;
        }

        // =====================================================
        // THANH TIẾN TRÌNH (hiệu ứng khi mở/đóng cửa sổ)
        // =====================================================
        private void RunProgress(string message)
        {
            lblStatus.Text = message;
            _ticks = 0;
            progressBar.Value = 0;
            _progressTimer.Start();
        }

        private void ProgressTick(object sender, EventArgs e)
        {
            _ticks++;
            progressBar.Value = Math.Min(100, _ticks * 10);   // 10 tick đầu chạy tới 100%
            if (_ticks >= 20)                                 // giữ 100% thêm một chút rồi reset
            {
                _progressTimer.Stop();
                progressBar.Value = 0;
                lblStatus.Text = "Sẵn sàng";
            }
        }
    }
}
