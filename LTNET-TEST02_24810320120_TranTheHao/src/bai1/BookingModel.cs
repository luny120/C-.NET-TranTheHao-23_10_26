using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HotelBooking
{
    // Implement INotifyPropertyChanged để BindingList<T> tự báo cho Grid
    // refresh ngay khi một thuộc tính thay đổi (đồng bộ 2 chiều).
    public class BookingModel : INotifyPropertyChanged
    {
        private string _id;
        private string _customerName;
        private string _identityCard;
        private string _roomType;
        private DateTime _checkIn = DateTime.Today;
        private int _days = 1;
        private decimal _pricePerNight;

        public string Id { get => _id; set => Set(ref _id, value); }
        public string CustomerName { get => _customerName; set => Set(ref _customerName, value); }
        public string IdentityCard { get => _identityCard; set => Set(ref _identityCard, value); }
        public string RoomType { get => _roomType; set => Set(ref _roomType, value); }
        public DateTime CheckIn { get => _checkIn; set => Set(ref _checkIn, value); }
        public int Days { get => _days; set => Set(ref _days, value); }
        public decimal PricePerNight { get => _pricePerNight; set => Set(ref _pricePerNight, value); }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
