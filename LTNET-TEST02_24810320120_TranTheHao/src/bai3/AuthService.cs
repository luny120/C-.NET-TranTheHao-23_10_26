using System;

namespace SchoolManagement
{
    // Tài khoản giả lập lưu trong bộ nhớ (đủ cho bài tập, không dùng CSDL).
    public static class AuthService
    {
        private static string _password = "123456";

        public const string UserName = "admin";
        public const string FullName = "Nguyễn Văn Quản Lý";

        public static bool Login(string user, string password)
        {
            return string.Equals((user ?? "").Trim(), UserName, StringComparison.OrdinalIgnoreCase)
                   && password == _password;
        }

        public static bool CheckPassword(string password) => password == _password;

        public static void ChangePassword(string newPassword) => _password = newPassword;
    }
}
