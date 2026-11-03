using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Seed Roles if missing
        var roleMap = new Dictionary<string, string>
        {
            { "role_admin", AppRoles.Admin },
            { "role_training_manager", AppRoles.TrainingManager },
            { "role_faculty_manager", AppRoles.FacultyManager },
            { "role_lecturer", AppRoles.Lecturer },
            { "role_student", AppRoles.Student },
            { "role_company", AppRoles.Company }
        };

        foreach (var (roleId, roleName) in roleMap)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                var newRole = new IdentityRole
                {
                    Id = roleId,
                    Name = roleName,
                    NormalizedName = roleName.ToUpper()
                };
                await roleManager.CreateAsync(newRole);
            }
        }

        // 2. Seed Default Faculties
        if (!await context.Faculties.AnyAsync())
        {
            var faculties = new List<Faculty>
            {
                new() { Code = "CNTT", Name = "Khoa Công nghệ Thông tin", IsActive = true },
                new() { Code = "KTKD", Name = "Khoa Kinh tế & Quản trị Kinh doanh", IsActive = true },
                new() { Code = "SPKT", Name = "Khoa Sư phạm Kỹ thuật", IsActive = true }
            };
            await context.Faculties.AddRangeAsync(faculties);
            await context.SaveChangesAsync();
        }

        // 3. Seed Default Majors
        if (!await context.Majors.AnyAsync())
        {
            var cntt = await context.Faculties.FirstAsync(f => f.Code == "CNTT");
            var ktkd = await context.Faculties.FirstAsync(f => f.Code == "KTKD");

            var majors = new List<Major>
            {
                new() { FacultyId = cntt.Id, Code = "7480201", Name = "Công nghệ Thông tin", IsActive = true },
                new() { FacultyId = cntt.Id, Code = "7480103", Name = "Kỹ thuật Phần mềm", IsActive = true },
                new() { FacultyId = cntt.Id, Code = "7480104", Name = "Hệ thống Thông tin", IsActive = true },
                new() { FacultyId = ktkd.Id, Code = "7340101", Name = "Quản trị Kinh doanh", IsActive = true }
            };
            await context.Majors.AddRangeAsync(majors);
            await context.SaveChangesAsync();
        }

        // 4. Seed Academic Years & Terms
        if (!await context.AcademicYears.AnyAsync())
        {
            var year2026 = new AcademicYear
            {
                Code = "2026-2027",
                StartDate = new DateTime(2026, 9, 1),
                EndDate = new DateTime(2027, 6, 30),
                Terms = new List<Term>
                {
                    new() { TermNo = 1, StartDate = new DateTime(2026, 9, 1), EndDate = new DateTime(2027, 1, 15) },
                    new() { TermNo = 2, StartDate = new DateTime(2027, 1, 16), EndDate = new DateTime(2027, 5, 30) },
                    new() { TermNo = 3, StartDate = new DateTime(2027, 6, 1), EndDate = new DateTime(2027, 8, 15) }
                }
            };

            var year2025 = new AcademicYear
            {
                Code = "2025-2026",
                StartDate = new DateTime(2025, 9, 1),
                EndDate = new DateTime(2026, 6, 30),
                Terms = new List<Term>
                {
                    new() { TermNo = 1, StartDate = new DateTime(2025, 9, 1), EndDate = new DateTime(2026, 1, 15) },
                    new() { TermNo = 2, StartDate = new DateTime(2026, 1, 16), EndDate = new DateTime(2026, 5, 30) }
                }
            };

            await context.AcademicYears.AddRangeAsync(year2026, year2025);
            await context.SaveChangesAsync();
        }

        // 5. Seed Initial Admin Account
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@vinhuni.edu.vn",
                FullName = "Quản trị viên Hệ thống",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, "Admin@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);

                // Add GLOBAL scope for Admin
                var adminRole = await roleManager.FindByNameAsync(AppRoles.Admin);
                if (adminRole != null)
                {
                    var hasScope = await context.UserRoleScopes.AnyAsync(s => s.UserId == adminUser.Id && s.RoleId == adminRole.Id);
                    if (!hasScope)
                    {
                        context.UserRoleScopes.Add(new UserRoleScope
                        {
                            UserId = adminUser.Id,
                            RoleId = adminRole.Id,
                            ScopeType = ScopeTypes.Global,
                            GrantedAt = DateTime.UtcNow,
                            GrantedByUserId = adminUser.Id
                        });
                        await context.SaveChangesAsync();
                    }
                }
            }
        }

        // 6. Seed Sample Student Account (for permission & test scenario 11)
        var studentUser = await userManager.FindByNameAsync("student");
        if (studentUser == null)
        {
            studentUser = new ApplicationUser
            {
                UserName = "student",
                Email = "student@vinhuni.edu.vn",
                FullName = "Nguyễn Văn Sinh Viên",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var createResult = await userManager.CreateAsync(studentUser, "Student@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(studentUser, AppRoles.Student);
            }
        }
    }
}
