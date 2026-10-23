using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WarehouseAnalytics
{
    public class frmWarehouseAnalytics : Form
    {
        private const string AllWarehouses = "Tất cả kho";
        private static readonly string[] Warehouses = { "Kho Hà Nội", "Kho Đà Nẵng", "Kho TP.HCM" };

        // ====== Controls ======
        private TabControl tabMain;
        private ComboBox cboWarehouse;
        private DateTimePicker dtpFrom, dtpTo;
        private Button btnAnalyze, btnCancel;
        private ProgressBar pgbStatus;
        private Label lblPercent, lblStatus, lblSummary;
        private DataGridView dgvReportData;
        private TextBox txtLog;
        private Button btnUiTest;
        private ToolStripStatusLabel lblClock;
        private readonly System.Windows.Forms.Timer _clock = new System.Windows.Forms.Timer { Interval = 1000 };

        private CancellationTokenSource _cts;   // dùng để ngắt tác vụ nền
        private int _clickCount;

        public frmWarehouseAnalytics()
        {
            BuildUI();
            SetupGrid();

            cboWarehouse.SelectedIndex = 0;
            dtpFrom.Value = DateTime.Today.AddMonths(-1);
            dtpTo.Value = DateTime.Today;
            SetRunning(false);

            _clock.Tick += (s, e) => lblClock.Text = "Đồng hồ UI: " + DateTime.Now.ToString("HH:mm:ss");
            _clock.Start();
            FormClosing += (s, e) => _cts?.Cancel();     // đóng form -> ngắt tác vụ nền
            FormClosed += (s, e) => _clock.Dispose();
            Log("Sẵn sàng.");
        }

        // =====================================================
        // 1. GIAO DIỆN
        // =====================================================
        private void BuildUI()
        {
            Text = "Bảng điều khiển báo cáo kho hàng";
            Size = new Size(1000, 660);
            StartPosition = FormStartPosition.CenterScreen;

            // ----- Tab 1: Báo cáo -----
            var pnlCriteria = new Panel { Dock = DockStyle.Top, Height = 85 };
            pnlCriteria.Controls.Add(new Label { Text = "Kho hàng:", Left = 15, Top = 16, Width = 70 });
            cboWarehouse = new ComboBox { Name = "cboWarehouse", Left = 90, Top = 12, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cboWarehouse.Items.Add(AllWarehouses);
            cboWarehouse.Items.AddRange(Warehouses);
            pnlCriteria.Controls.Add(cboWarehouse);

            pnlCriteria.Controls.Add(new Label { Text = "Từ ngày:", Left = 275, Top = 16, Width = 60 });
            dtpFrom = new DateTimePicker { Name = "dtpFrom", Left = 340, Top = 12, Width = 120, Format = DateTimePickerFormat.Short };
            pnlCriteria.Controls.Add(dtpFrom);

            pnlCriteria.Controls.Add(new Label { Text = "Đến ngày:", Left = 480, Top = 16, Width = 65 });
            dtpTo = new DateTimePicker { Name = "dtpTo", Left = 550, Top = 12, Width = 120, Format = DateTimePickerFormat.Short };
            pnlCriteria.Controls.Add(dtpTo);

            btnAnalyze = new Button { Name = "btnAnalyze", Text = "Thống Kê Kho", Left = 15, Top = 46, Width = 130, Height = 32 };
            btnCancel = new Button { Name = "btnCancel", Text = "Hủy Tác Vụ", Left = 155, Top = 46, Width = 130, Height = 32 };
            btnAnalyze.Click += async (s, e) => await AnalyzeAsync();
            btnCancel.Click += (s, e) => CancelAnalysis();
            pnlCriteria.Controls.Add(btnAnalyze);
            pnlCriteria.Controls.Add(btnCancel);

            var pnlStatus = new Panel { Dock = DockStyle.Top, Height = 62 };
            pgbStatus = new ProgressBar { Name = "pgbStatus", Left = 15, Top = 8, Width = 800, Height = 24, Minimum = 0, Maximum = 100 };
            pgbStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblPercent = new Label { Name = "lblPercent", Text = "0%", Left = 825, Top = 11, Width = 60, Font = new Font(Font, FontStyle.Bold) };
            lblPercent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblStatus = new Label { Text = "Chưa có dữ liệu. Chọn tiêu chí rồi bấm \"Thống Kê Kho\".", Left = 15, Top = 38, Width = 800, ForeColor = Color.DimGray };
            pnlStatus.Controls.AddRange(new Control[] { pgbStatus, lblPercent, lblStatus });

            dgvReportData = new DataGridView { Name = "dgvReportData", Dock = DockStyle.Fill };
            lblSummary = new Label { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), Font = new Font(Font, FontStyle.Bold) };

            var tabReport = new TabPage("Báo cáo tồn kho");
            tabReport.Controls.Add(dgvReportData);   // Fill thêm trước
            tabReport.Controls.Add(lblSummary);      // Bottom
            tabReport.Controls.Add(pnlStatus);       // Top
            tabReport.Controls.Add(pnlCriteria);     // Top (nằm trên cùng)

            // ----- Tab 2: Nhật ký -----
            txtLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White };
            var tabLog = new TabPage("Nhật ký");
            tabLog.Controls.Add(txtLog);

            // ----- Tab 3: Thử nghiệm UI -----
            var tabTest = new TabPage("Thử nghiệm UI");
            tabTest.Controls.Add(new Label
            {
                Left = 15, Top = 15, Width = 700, Height = 45,
                Text = "Trong lúc đang thống kê, hãy thử: gõ chữ vào ô bên dưới, bấm nút đếm, kéo/phóng to/thu nhỏ Form, chuyển Tab. Giao diện vẫn mượt vì tác vụ nặng chạy ở luồng nền."
            });
            tabTest.Controls.Add(new TextBox { Left = 15, Top = 70, Width = 400 });
            btnUiTest = new Button { Text = "Bấm thử (0)", Left = 15, Top = 105, Width = 140, Height = 32 };
            btnUiTest.Click += (s, e) => btnUiTest.Text = $"Bấm thử ({++_clickCount})";
            tabTest.Controls.Add(btnUiTest);

            tabMain = new TabControl { Dock = DockStyle.Fill };
            tabMain.TabPages.AddRange(new[] { tabReport, tabLog, tabTest });

            var status = new StatusStrip();
            lblClock = new ToolStripStatusLabel("Đồng hồ UI: " + DateTime.Now.ToString("HH:mm:ss"));
            status.Items.Add(lblClock);

            Controls.Add(tabMain);
            Controls.Add(status);
        }

        private void SetupGrid()
        {
            dgvReportData.AutoGenerateColumns = false;
            dgvReportData.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReportData.ReadOnly = true;
            dgvReportData.AllowUserToAddRows = false;
            dgvReportData.RowHeadersVisible = false;
            dgvReportData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReportData.Columns.Clear();

            AddCol("Mã SKU", nameof(InventoryItem.Sku), 80);
            AddCol("Tên hàng", nameof(InventoryItem.Name), 170);
            AddCol("Danh mục", nameof(InventoryItem.Category), 110);
            AddCol("Kho", nameof(InventoryItem.Warehouse), 100);
            AddCol("Tồn kho", nameof(InventoryItem.Quantity), 70, "N0", true);
            AddCol("Đơn giá", nameof(InventoryItem.UnitPrice), 100, "N0", true);
            AddCol("Giá trị tồn", nameof(InventoryItem.TotalValue), 120, "N0", true);
            AddCol("Cập nhật cuối", nameof(InventoryItem.LastUpdated), 100, "dd/MM/yyyy");
        }

        private void AddCol(string header, string prop, float weight, string format = null, bool right = false)
        {
            var c = new DataGridViewTextBoxColumn { Name = "col" + prop, HeaderText = header, DataPropertyName = prop, FillWeight = weight };
            if (format != null) c.DefaultCellStyle.Format = format;
            if (right) c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvReportData.Columns.Add(c);
        }

        // =====================================================
        // 2. ASYNC / AWAIT
        // =====================================================
        private async Task AnalyzeAsync()
        {
            if (dtpFrom.Value.Date > dtpTo.Value.Date)
            {
                MessageBox.Show("\"Từ ngày\" không được lớn hơn \"Đến ngày\".", "Tiêu chí không hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Đọc tiêu chí ở UI thread trước khi đẩy sang luồng nền
            string warehouse = cboWarehouse.SelectedItem.ToString();
            DateTime from = dtpFrom.Value.Date, to = dtpTo.Value.Date;

            var cts = new CancellationTokenSource();
            _cts = cts;
            CancellationToken token = cts.Token;

            SetRunning(true);
            pgbStatus.Value = 0;
            lblPercent.Text = "0%";
            lblStatus.Text = "Đang quét và tổng hợp dữ liệu kho...";
            Log($"Bắt đầu thống kê: {warehouse}, {from:dd/MM/yyyy} - {to:dd/MM/yyyy}");

            // Progress<T> tạo ở UI thread -> callback tự chạy lại trên UI thread
            var progress = new Progress<int>(p =>
            {
                if (token.IsCancellationRequested || IsDisposed) return;   // bỏ qua báo cáo trễ sau khi hủy
                pgbStatus.Value = p;
                lblPercent.Text = p + "%";
            });

            var sw = Stopwatch.StartNew();
            List<InventoryItem> data = null;
            bool cancelled = false;
            Exception error = null;

            try
            {
                // Phần tính toán nặng chạy ở Background Thread, UI thread được giải phóng trong lúc await
                data = await Task.Run(() => BuildReport(warehouse, from, to, progress, token), token);
            }
            catch (OperationCanceledException) { cancelled = true; }
            catch (Exception ex) { error = ex; }
            finally
            {
                sw.Stop();
                cts.Dispose();
                if (ReferenceEquals(_cts, cts)) _cts = null;
            }

            if (IsDisposed) return;
            SetRunning(false);

            if (cancelled)
            {
                pgbStatus.Value = 0;
                lblPercent.Text = "0%";
                lblStatus.Text = "Tác vụ đã bị hủy.";
                Log("Đã hủy tác vụ thống kê.");
                MessageBox.Show("Tác vụ thống kê đã được hủy.", "Đã hủy", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (error != null)
            {
                pgbStatus.Value = 0;
                lblPercent.Text = "0%";
                lblStatus.Text = "Lỗi khi thống kê.";
                Log("Lỗi: " + error.Message);
                MessageBox.Show("Không thể thống kê:\n" + error.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                dgvReportData.DataSource = data;
                pgbStatus.Value = 100;
                lblPercent.Text = "100%";
                lblStatus.Text = $"Hoàn tất sau {sw.Elapsed.TotalSeconds:0.0} giây.";
                lblSummary.Text = $"Số mặt hàng: {data.Count:N0}   |   Tổng tồn: {data.Sum(x => x.Quantity):N0}   |   Tổng giá trị: {data.Sum(x => x.TotalValue):N0} VNĐ";
                Log($"Hoàn tất: {data.Count} mặt hàng, {sw.Elapsed.TotalSeconds:0.0} giây.");
            }
        }

        // Chạy ở luồng nền: KHÔNG được đụng tới control trên Form ở trong hàm này.
        private static List<InventoryItem> BuildReport(string warehouse, DateTime from, DateTime to,
                                                       IProgress<int> progress, CancellationToken token)
        {
            var rnd = new Random();
            const int steps = 100;
            int stepMs = rnd.Next(4000, 5001) / steps;      // tổng thời gian giả lập 4 - 5 giây
            string[] whs = warehouse == AllWarehouses ? Warehouses : new[] { warehouse };

            string[] categories = { "Điện tử", "Gia dụng", "Thực phẩm", "Văn phòng phẩm", "Thời trang" };
            string[][] names =
            {
                new[] { "Tai nghe", "Sạc dự phòng", "Chuột không dây", "Bàn phím cơ" },
                new[] { "Nồi cơm điện", "Quạt đứng", "Bình đun siêu tốc", "Máy xay sinh tố" },
                new[] { "Gạo ST25", "Dầu ăn", "Nước mắm", "Mì gói" },
                new[] { "Giấy A4", "Bút bi", "Sổ tay", "Kẹp tài liệu" },
                new[] { "Áo thun", "Quần jean", "Nón lưỡi trai", "Giày thể thao" }
            };

            int span = Math.Max(1, (to - from).Days + 1);
            var list = new List<InventoryItem>();
            int seq = 1;

            for (int step = 1; step <= steps; step++)
            {
                // Chờ stepMs nhưng thoát ngay khi có yêu cầu hủy
                if (token.WaitHandle.WaitOne(stepMs)) token.ThrowIfCancellationRequested();
                token.ThrowIfCancellationRequested();

                foreach (string wh in whs)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int c = rnd.Next(categories.Length);
                        list.Add(new InventoryItem
                        {
                            Sku = "SKU-" + (seq++).ToString("D4"),
                            Name = names[c][rnd.Next(names[c].Length)],
                            Category = categories[c],
                            Warehouse = wh,
                            Quantity = rnd.Next(0, 501),
                            UnitPrice = rnd.Next(10, 2001) * 1000m,
                            LastUpdated = from.AddDays(rnd.Next(span))
                        });
                    }
                }
                progress.Report(step);
            }
            return list.OrderByDescending(x => x.TotalValue).ToList();
        }

        // =====================================================
        // 3. HỦY TÁC VỤ
        // =====================================================
        private void CancelAnalysis()
        {
            if (_cts == null) return;
            btnCancel.Enabled = false;                 // tránh bấm lặp
            lblStatus.Text = "Đang hủy tác vụ...";
            _cts.Cancel();                             // báo cho luồng nền dừng lại
        }

        // =====================================================
        // 4. HỖ TRỢ
        // =====================================================
        private void SetRunning(bool running)
        {
            btnAnalyze.Enabled = !running;
            btnCancel.Enabled = running;
            cboWarehouse.Enabled = !running;
            dtpFrom.Enabled = !running;
            dtpTo.Enabled = !running;
        }

        private void Log(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
