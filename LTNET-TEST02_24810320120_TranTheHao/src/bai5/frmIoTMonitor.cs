using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IoTMonitor
{
    public class frmIoTMonitor : Form
    {
        private const int IntervalMs = 2000;   // 2 giây/lần
        private const int MaxLogRows = 500;

        // ====== Controls ======
        private Button btnStart, btnStop;
        private Label lblTemperature;
        private DataGridView dgvLogs;
        private ToolStripStatusLabel lblConnection, lblCycles;

        // ====== Dữ liệu ======
        private readonly BindingList<LogEntry> _logs = new BindingList<LogEntry>();
        private CancellationTokenSource _cts;
        private int _cycleCount;               // chỉ được đọc/ghi ở UI thread

        public frmIoTMonitor()
        {
            BuildUI();
            SetupGrid();
            dgvLogs.DataSource = _logs;
            SetRunning(false);

            // Đóng form -> dừng luồng nền, tránh Invoke vào form đã bị hủy
            FormClosing += (s, e) => _cts?.Cancel();
        }

        // =====================================================
        // 1. GIAO DIỆN
        // =====================================================
        private void BuildUI()
        {
            Text = "Giám sát cảnh báo IoT thời gian thực";
            Size = new Size(820, 580);
            StartPosition = FormStartPosition.CenterScreen;

            var pnlButtons = new Panel { Dock = DockStyle.Top, Height = 58 };
            btnStart = new Button { Name = "btnStart", Text = "Bắt đầu Giám sát", Left = 15, Top = 12, Width = 160, Height = 34 };
            btnStop = new Button { Name = "btnStop", Text = "Dừng Giám sát", Left = 185, Top = 12, Width = 160, Height = 34 };
            btnStart.Click += (s, e) => StartMonitoring();
            btnStop.Click += (s, e) => StopMonitoring();
            pnlButtons.Controls.Add(btnStart);
            pnlButtons.Controls.Add(btnStop);

            var pnlTemp = new Panel { Dock = DockStyle.Top, Height = 130, BackColor = Color.WhiteSmoke };
            pnlTemp.Controls.Add(new Label
            {
                Text = "NHIỆT ĐỘ CẢM BIẾN",
                Dock = DockStyle.Top, Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DimGray
            });
            lblTemperature = new Label
            {
                Name = "lblTemperature",
                Text = "-- °C",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 42f, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            pnlTemp.Controls.Add(lblTemperature);
            pnlTemp.Controls.SetChildIndex(lblTemperature, 0);   // Fill nằm dưới tiêu đề

            dgvLogs = new DataGridView { Name = "dgvLogs", Dock = DockStyle.Fill };

            var status = new StatusStrip();
            lblConnection = new ToolStripStatusLabel("Kết nối: Chưa kết nối") { BorderSides = ToolStripStatusLabelBorderSides.Right };
            lblCycles = new ToolStripStatusLabel("Số chu kỳ nhận tín hiệu: 0");
            status.Items.Add(lblConnection);
            status.Items.Add(lblCycles);

            Controls.Add(dgvLogs);      // Fill thêm trước
            Controls.Add(pnlTemp);
            Controls.Add(pnlButtons);
            Controls.Add(status);
        }

        private void SetupGrid()
        {
            dgvLogs.AutoGenerateColumns = false;
            dgvLogs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLogs.ReadOnly = true;
            dgvLogs.AllowUserToAddRows = false;
            dgvLogs.RowHeadersVisible = false;
            dgvLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvLogs.Columns.Clear();

            AddCol("STT", nameof(LogEntry.No), 40);
            AddCol("Thời gian", nameof(LogEntry.Time), 150, "dd/MM/yyyy HH:mm:ss");
            AddCol("Nhiệt độ (°C)", nameof(LogEntry.Temperature), 90, "0.0");
            AddCol("Trạng thái", nameof(LogEntry.Status), 100);

            // Dòng cảnh báo tô đỏ
            dgvLogs.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var item = dgvLogs.Rows[e.RowIndex].DataBoundItem as LogEntry;
                e.CellStyle.ForeColor = item != null && item.IsWarning ? Color.Firebrick : Color.Black;
            };
        }

        private void AddCol(string header, string prop, float weight, string format = null)
        {
            var c = new DataGridViewTextBoxColumn { Name = "col" + prop, HeaderText = header, DataPropertyName = prop, FillWeight = weight };
            if (format != null) c.DefaultCellStyle.Format = format;
            dgvLogs.Columns.Add(c);
        }

        private void SetRunning(bool running)
        {
            btnStart.Enabled = !running;
            btnStop.Enabled = running;
        }

        // =====================================================
        // 2. BẮT ĐẦU / DỪNG GIÁM SÁT (chạy ở UI thread)
        // =====================================================
        private void StartMonitoring()
        {
            _cycleCount = 0;
            lblCycles.Text = "Số chu kỳ nhận tín hiệu: 0";
            lblTemperature.Text = "-- °C";
            lblTemperature.ForeColor = Color.Gray;
            SetConnection("Đang kết nối...", Color.DarkOrange);
            SetRunning(true);

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;
            CancellationTokenSource cts = _cts;

            // Task.Run: vòng lặp vô hạn chạy ngầm ở thread của ThreadPool
            Task.Run(async () =>
            {
                try { await MonitorLoopAsync(token); }
                finally { cts.Dispose(); }
            });
        }

        private void StopMonitoring()
        {
            // Chỉ phát tín hiệu hủy, KHÔNG chờ luồng nền (chờ ở UI thread + Invoke sẽ gây deadlock)
            _cts?.Cancel();
            _cts = null;
            SetRunning(false);
            SetConnection("Đã ngắt kết nối", Color.Gray);
        }

        // =====================================================
        // 3. LUỒNG NỀN (KHÔNG được đụng tới control trực tiếp)
        // =====================================================
        private async Task MonitorLoopAsync(CancellationToken token)
        {
            var rnd = new Random();
            try
            {
                await Task.Delay(800, token);                         // giả lập thời gian bắt tay kết nối
                UpdateConnection("Đã kết nối", Color.ForestGreen, token);

                while (!token.IsCancellationRequested)
                {
                    double temp = Math.Round(30 + rnd.NextDouble() * 12, 1);   // 30.0 - 42.0 °C
                    UpdateTemperature(temp, token);                    // chuyển về UI thread bên trong hàm này
                    await Task.Delay(IntervalMs, token);               // định kỳ 2 giây
                }
            }
            catch (OperationCanceledException) { /* người dùng bấm Dừng */ }
            catch (ObjectDisposedException) { /* form đã đóng */ }
            catch (InvalidOperationException) when (token.IsCancellationRequested || IsDisposed) { /* form đang đóng */ }
        }

        // =====================================================
        // 4. THREAD SAFETY: các hàm cập nhật UI
        // =====================================================
        private void UpdateConnection(string text, Color color, CancellationToken token)
        {
            if (InvokeRequired)    // đang ở thread phụ -> chuyển về UI thread
            {
                Invoke(new Action(() => UpdateConnection(text, color, token)));
                return;
            }
            if (token.IsCancellationRequested) return;   // lệnh trễ của lần chạy đã dừng
            SetConnection(text, color);
        }

        private void UpdateTemperature(double temp, CancellationToken token)
        {
            if (InvokeRequired)    // đang ở thread phụ -> chuyển về UI thread
            {
                Invoke(new Action(() => UpdateTemperature(temp, token)));
                return;
            }

            // Từ đây trở xuống chắc chắn chạy trên UI thread -> được phép đụng control
            if (token.IsCancellationRequested) return;

            _cycleCount++;
            var entry = new LogEntry { No = _cycleCount, Time = DateTime.Now, Temperature = temp };

            lblTemperature.Text = temp.ToString("0.0") + " °C";
            lblTemperature.ForeColor = entry.IsWarning ? Color.Red : Color.ForestGreen;
            lblCycles.Text = "Số chu kỳ nhận tín hiệu: " + _cycleCount;

            _logs.Insert(0, entry);                       // dòng mới nhất nằm trên cùng
            if (_logs.Count > MaxLogRows) _logs.RemoveAt(_logs.Count - 1);
        }

        private void SetConnection(string text, Color color)
        {
            lblConnection.Text = "Kết nối: " + text;
            lblConnection.ForeColor = color;
        }
    }
}
