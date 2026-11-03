namespace InternshipManagement.Web.Models;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string TrainingManager = "TrainingManager";
    public const string FacultyManager = "FacultyManager";
    public const string Lecturer = "Lecturer";
    public const string Student = "Student";
    public const string Company = "Company";

    public static readonly string[] AllRoles = new[]
    {
        Admin,
        TrainingManager,
        FacultyManager,
        Lecturer,
        Student,
        Company
    };

    public static string GetDisplayName(string roleName)
    {
        return roleName switch
        {
            Admin => "Quản trị viên (Admin)",
            TrainingManager => "Cán bộ Quản lý Đào tạo",
            FacultyManager => "Cán bộ Khoa / Ngành",
            Lecturer => "Giảng viên",
            Student => "Sinh viên",
            Company => "Doanh nghiệp",
            _ => roleName
        };
    }
}
