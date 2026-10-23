using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EmployeeManagement
{
    // INotifyPropertyChanged: sửa thuộc tính -> BindingList báo ListChanged
    // -> Grid và StatusStrip tự cập nhật.
    public class EmployeeModel : INotifyPropertyChanged
    {
        private string _id;
        private string _fullName;
        private string _department;
        private string _position;
        private decimal _salary;
        private string _avatarPath;

        public string Id { get => _id; set => Set(ref _id, value); }
        public string FullName { get => _fullName; set => Set(ref _fullName, value); }
        public string Department { get => _department; set => Set(ref _department, value); }
        public string Position { get => _position; set => Set(ref _position, value); }
        public decimal Salary { get => _salary; set => Set(ref _salary, value); }
        public string AvatarPath { get => _avatarPath; set => Set(ref _avatarPath, value); }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
