using System;
using System.ComponentModel;

namespace SchoolManagement
{
    public class StudentModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string ClassName { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class CourseModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Credits { get; set; }
    }

    public class TeacherModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
    }

    public class ClassModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Homeroom { get; set; }
        public int Size { get; set; }
    }

    // Dữ liệu dùng chung: đóng rồi mở lại Form con vẫn giữ nguyên dữ liệu đã nhập.
    public static class DataStore
    {
        public static readonly BindingList<StudentModel> Students = new BindingList<StudentModel>
        {
            new StudentModel { Id = "SV001", FullName = "Nguyễn Minh Anh", ClassName = "CNTT1", BirthDate = new DateTime(2004, 3, 12) },
            new StudentModel { Id = "SV002", FullName = "Trần Quốc Bảo", ClassName = "CNTT2", BirthDate = new DateTime(2004, 7, 25) },
            new StudentModel { Id = "SV003", FullName = "Lê Thu Cúc", ClassName = "KT1", BirthDate = new DateTime(2003, 11, 2) },
        };

        public static readonly BindingList<CourseModel> Courses = new BindingList<CourseModel>
        {
            new CourseModel { Id = "MH001", Name = "Lập trình C#", Credits = 3 },
            new CourseModel { Id = "MH002", Name = "Cơ sở dữ liệu", Credits = 3 },
            new CourseModel { Id = "MH003", Name = "Toán rời rạc", Credits = 2 },
        };

        public static readonly BindingList<TeacherModel> Teachers = new BindingList<TeacherModel>
        {
            new TeacherModel { Id = "GV001", FullName = "TS. Phạm Văn Đức", Department = "Công nghệ thông tin", Email = "ducpv@school.edu.vn" },
            new TeacherModel { Id = "GV002", FullName = "ThS. Hoàng Thị Hà", Department = "Kinh tế", Email = "hahoang@school.edu.vn" },
            new TeacherModel { Id = "GV003", FullName = "ThS. Võ Minh Khoa", Department = "Công nghệ thông tin", Email = "khoavm@school.edu.vn" },
        };

        public static readonly BindingList<ClassModel> Classes = new BindingList<ClassModel>
        {
            new ClassModel { Id = "L001", Name = "CNTT1", Homeroom = "TS. Phạm Văn Đức", Size = 40 },
            new ClassModel { Id = "L002", Name = "CNTT2", Homeroom = "ThS. Võ Minh Khoa", Size = 38 },
            new ClassModel { Id = "L003", Name = "KT1", Homeroom = "ThS. Hoàng Thị Hà", Size = 45 },
        };
    }
}
