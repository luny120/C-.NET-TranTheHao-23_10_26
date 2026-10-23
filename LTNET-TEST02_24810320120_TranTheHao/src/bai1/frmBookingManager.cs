using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HotelBooking
{
    public class frmBookingManager : Form
    {
        // ====== Controls ======
        private TextBox txtBookingId, txtCustomerName, txtIdentityCard, txtPricePerNight;
        private ComboBox cboRoomType;
        private DateTimePicker dtpCheckIn;
        private NumericUpDown numDays;
        private Button btnAdd, btnUpdate, btnDelete, btnReset;
        private DataGridView dgvBookings;
        private ErrorProvider errorProvider;

        // ====== Data ======
        private readonly BindingList<BookingModel> _bookings = new BindingList<BookingModel>();
        private readonly BindingSource _bs = new BindingSource();
        private bool _loading; // chặn vòng lặp khi đang đổ dữ liệu lên ô nhập

        public frmBookingManager()
        {
            BuildUI();
            SetupGrid();

            _bs.DataSource = _bookings;          // BindingSource bọc BindingList<T>
            dgvBookings.DataSource = _bs;        // Grid bind vào BindingSource
            dgvBookings.SelectionChanged += (s, e) => ShowCurrentToInputs();

            SeedData();
            ClearInputs();
        }

        // =====================================================
        // 1. GIAO DIỆN
        // =====================================================
        private void BuildUI()
        {
            Text = "Quản lý đặt phòng khách sạn";
            Size = new Size(860, 600);
            StartPosition = FormStartPosition.CenterScreen;

            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            var grp = new GroupBox { Text = "Thông tin đặt phòng", Left = 10, Top = 10, Width = 820, Height = 190 };
            Controls.Add(grp);

            txtBookingId = new TextBox { ReadOnly = true, Name = "txtBookingId" };
            txtCustomerName = new TextBox { Name = "txtCustomerName" };
            txtIdentityCard = new TextBox { Name = "txtIdentityCard", MaxLength = 12 };
            cboRoomType = new ComboBox { Name = "cboRoomType", DropDownStyle = ComboBoxStyle.DropDownList };
            cboRoomType.Items.AddRange(new object[] { "Standard", "Deluxe", "VIP" });
            dtpCheckIn = new DateTimePicker { Name = "dtpCheckIn", Format = DateTimePickerFormat.Short };
            numDays = new NumericUpDown { Name = "numDays", Minimum = 1, Maximum = 365, Value = 1 };
            txtPricePerNight = new TextBox { Name = "txtPricePerNight" };

            AddRow(grp, "Mã đặt phòng:", txtBookingId, 0, 0);
            AddRow(grp, "Tên khách hàng:", txtCustomerName, 0, 1);
            AddRow(grp, "Số CCCD/CMND:", txtIdentityCard, 0, 2);
            AddRow(grp, "Loại phòng:", cboRoomType, 0, 3);
            AddRow(grp, "Ngày nhận phòng:", dtpCheckIn, 1, 0);
            AddRow(grp, "Số ngày ở:", numDays, 1, 1);
            AddRow(grp, "Giá phòng/đêm:", txtPricePerNight, 1, 2);

            // Xóa lỗi ngay khi người dùng sửa lại
            txtCustomerName.TextChanged += (s, e) => errorProvider.SetError(txtCustomerName, "");
            txtIdentityCard.TextChanged += (s, e) => errorProvider.SetError(txtIdentityCard, "");
            txtPricePerNight.TextChanged += (s, e) => errorProvider.SetError(txtPricePerNight, "");

            btnAdd = MakeButton("Thêm đặt phòng", 10, btnAdd_Click, 130);
            btnUpdate = MakeButton("Cập nhật", 150, btnUpdate_Click);
            btnDelete = MakeButton("Xóa", 250, btnDelete_Click);
            btnReset = MakeButton("Làm mới", 350, btnReset_Click);

            dgvBookings = new DataGridView { Name = "dgvBookings", Left = 10, Top = 255, Width = 820, Height = 295 };
            dgvBookings.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(dgvBookings);
        }

        private void AddRow(GroupBox parent, string caption, Control ctl, int col, int row)
        {
            int x = col == 0 ? 15 : 420;
            int y = 28 + row * 38;
            parent.Controls.Add(new Label { Text = caption, Left = x, Top = y + 3, Width = 110 });
            ctl.Left = x + 115;
            ctl.Top = y;
            ctl.Width = 250;
            parent.Controls.Add(ctl);
        }

        private Button MakeButton(string text, int left, EventHandler onClick, int width = 90)
        {
            var b = new Button { Text = text, Left = left, Top = 210, Width = width, Height = 32 };
            b.Click += onClick;
            Controls.Add(b);
            return b;
        }

        // =====================================================
        // 2. KHỞI TẠO GRID (hoàn toàn bằng code)
        // =====================================================
        private void SetupGrid()
        {
            dgvBookings.AutoGenerateColumns = false;
            dgvBookings.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // FullRowSelect
            dgvBookings.ReadOnly = true;
            dgvBookings.MultiSelect = false;
            dgvBookings.AllowUserToAddRows = false;
            dgvBookings.AllowUserToDeleteRows = false;
            dgvBookings.RowHeadersVisible = false;
            dgvBookings.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvBookings.Columns.Clear();
            dgvBookings.Columns.Add(MakeCol("colId", "Mã Đặt", nameof(BookingModel.Id)));
            dgvBookings.Columns.Add(MakeCol("colName", "Tên Khách", nameof(BookingModel.CustomerName), 150));
            dgvBookings.Columns.Add(MakeCol("colCCCD", "CCCD", nameof(BookingModel.IdentityCard)));
            dgvBookings.Columns.Add(MakeCol("colRoom", "Loại Phòng", nameof(BookingModel.RoomType)));
            dgvBookings.Columns.Add(MakeCol("colCheckIn", "Ngày Nhận", nameof(BookingModel.CheckIn), 100, "dd/MM/yyyy"));
            dgvBookings.Columns.Add(MakeCol("colDays", "Số Đêm", nameof(BookingModel.Days), 70));
            var price = MakeCol("colPrice", "Giá/Đêm", nameof(BookingModel.PricePerNight), 100, "N0");
            price.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvBookings.Columns.Add(price);
        }

        private DataGridViewTextBoxColumn MakeCol(string name, string header, string prop,
                                                  float weight = 100, string format = null)
        {
            var c = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = prop,   // khớp thuộc tính BookingModel
                FillWeight = weight
            };
            if (format != null) c.DefaultCellStyle.Format = format;
            return c;
        }

        // =====================================================
        // 3. HELPER
        // =====================================================
        private void SeedData()
        {
            _bookings.Add(new BookingModel { Id = "BK001", CustomerName = "Nguyễn Văn An", IdentityCard = "079123456789", RoomType = "Standard", CheckIn = DateTime.Today, Days = 2, PricePerNight = 500000 });
            _bookings.Add(new BookingModel { Id = "BK002", CustomerName = "Trần Thị Bình", IdentityCard = "123456789", RoomType = "VIP", CheckIn = DateTime.Today.AddDays(1), Days = 3, PricePerNight = 1500000 });
        }

        private string GenerateId()
        {
            int max = _bookings
                .Select(b => int.TryParse(b.Id.Substring(2), out int n) ? n : 0)
                .DefaultIfEmpty(0).Max();
            return "BK" + (max + 1).ToString("D3");
        }

        private BookingModel GetSelected()
        {
            return dgvBookings.SelectedRows.Count > 0 ? dgvBookings.SelectedRows[0].DataBoundItem as BookingModel : null;
        }

        private void ClearInputs()
        {
            _loading = true;
            txtBookingId.Text = GenerateId();
            txtCustomerName.Clear();
            txtIdentityCard.Clear();
            txtPricePerNight.Clear();
            cboRoomType.SelectedIndex = 0;
            dtpCheckIn.Value = DateTime.Today;
            numDays.Value = 1;
            ClearErrors();
            _loading = false;
        }

        private void ClearErrors()
        {
            errorProvider.SetError(txtCustomerName, "");
            errorProvider.SetError(txtIdentityCard, "");
            errorProvider.SetError(txtPricePerNight, "");
        }

        // Grid -> ô nhập liệu
        private void ShowCurrentToInputs()
        {
            if (_loading) return;
            if (dgvBookings.SelectedRows.Count == 0) return;
            if (!(dgvBookings.SelectedRows[0].DataBoundItem is BookingModel b)) return;

            _loading = true;
            txtBookingId.Text = b.Id;
            txtCustomerName.Text = b.CustomerName;
            txtIdentityCard.Text = b.IdentityCard;
            cboRoomType.SelectedItem = b.RoomType;
            dtpCheckIn.Value = b.CheckIn;
            numDays.Value = b.Days;
            txtPricePerNight.Text = b.PricePerNight.ToString("0.##", CultureInfo.CurrentCulture);
            ClearErrors();
            _loading = false;
        }

        // =====================================================
        // 4. VALIDATION (ErrorProvider)
        // =====================================================
        private bool ValidateInputs(out decimal price)
        {
            bool ok = true;
            price = 0;
            ClearErrors();

            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                errorProvider.SetError(txtCustomerName, "Tên khách hàng không được để trống.");
                ok = false;
            }

            if (!Regex.IsMatch(txtIdentityCard.Text.Trim(), @"^(\d{9}|\d{12})$"))
            {
                errorProvider.SetError(txtIdentityCard, "CCCD/CMND phải gồm đúng 9 hoặc 12 chữ số.");
                ok = false;
            }

            if (!decimal.TryParse(txtPricePerNight.Text.Trim(), NumberStyles.Number,
                                  CultureInfo.CurrentCulture, out price) || price <= 0)
            {
                errorProvider.SetError(txtPricePerNight, "Giá phòng phải là số lớn hơn 0.");
                ok = false;
            }
            return ok;
        }

        private void FillModel(BookingModel m, decimal price)
        {
            m.CustomerName = txtCustomerName.Text.Trim();
            m.IdentityCard = txtIdentityCard.Text.Trim();
            m.RoomType = cboRoomType.SelectedItem.ToString();
            m.CheckIn = dtpCheckIn.Value.Date;
            m.Days = (int)numDays.Value;
            m.PricePerNight = price;
        }

        // =====================================================
        // 5. SỰ KIỆN NÚT
        // =====================================================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out decimal price)) return;

            var m = new BookingModel { Id = GenerateId() };
            FillModel(m, price);
            _bookings.Add(m);                 // Grid tự cập nhật nhờ BindingList

            _bs.Position = _bs.Count - 1;     // chọn dòng vừa thêm
            MessageBox.Show("Thêm đặt phòng thành công!", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            btnReset_Click(sender, e);
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            var m = GetSelected();
            if (m == null)
            {
                MessageBox.Show("Vui lòng chọn một dòng cần cập nhật.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!ValidateInputs(out decimal price)) return;

            FillModel(m, price);              // INotifyPropertyChanged -> Grid refresh ngay
            MessageBox.Show("Cập nhật thành công!", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var m = GetSelected();
            if (m == null)
            {
                MessageBox.Show("Vui lòng chọn một dòng cần xóa.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rs = MessageBox.Show($"Bạn có chắc muốn xóa đặt phòng {m.Id} của {m.CustomerName}?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (rs == DialogResult.Yes)
            {
                _bookings.Remove(m);
                btnReset_Click(sender, e);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            dgvBookings.ClearSelection();
            ClearInputs();
            txtCustomerName.Focus();
        }
    }
}
