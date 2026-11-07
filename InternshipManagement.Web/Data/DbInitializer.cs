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

        // 6. Seed Student Account
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

        // 7. Seed Lecturer Account (giangvien)
        var lecturerUser = await userManager.FindByNameAsync("giangvien");
        if (lecturerUser == null)
        {
            lecturerUser = new ApplicationUser
            {
                UserName = "giangvien",
                Email = "giangvien@vinhuni.edu.vn",
                FullName = "TS. Hoàng Minh Giảng Viên",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var createResult = await userManager.CreateAsync(lecturerUser, "Lecturer@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(lecturerUser, AppRoles.Lecturer);
            }
        }

        // Also check if giangvien_demo exists, ensure Lecturer role
        var gvDemo = await userManager.FindByNameAsync("giangvien_demo");
        if (gvDemo != null && !await userManager.IsInRoleAsync(gvDemo, AppRoles.Lecturer))
        {
            await userManager.AddToRoleAsync(gvDemo, AppRoles.Lecturer);
        }

        // 8. Seed Training Manager (cb_daotao) if missing
        var cbDaotao = await userManager.FindByNameAsync("cb_daotao");
        if (cbDaotao == null)
        {
            cbDaotao = new ApplicationUser
            {
                UserName = "cb_daotao",
                Email = "cb_daotao@vinhuni.edu.vn",
                FullName = "Trần Văn Cán Bộ",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var createResult = await userManager.CreateAsync(cbDaotao, "CanBo@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(cbDaotao, AppRoles.TrainingManager);
                var tmRole = await roleManager.FindByNameAsync(AppRoles.TrainingManager);
                if (tmRole != null && adminUser != null)
                {
                    context.UserRoleScopes.Add(new UserRoleScope
                    {
                        UserId = cbDaotao.Id,
                        RoleId = tmRole.Id,
                        ScopeType = ScopeTypes.Global,
                        GrantedAt = DateTime.UtcNow,
                        GrantedByUserId = adminUser.Id
                    });
                    await context.SaveChangesAsync();
                }
            }
        }

        // 9. Seed Faculty Manager (cb_cntt) for Faculty 1 (CNTT)
        var cbCntt = await userManager.FindByNameAsync("cb_cntt");
        if (cbCntt == null)
        {
            cbCntt = new ApplicationUser
            {
                UserName = "cb_cntt",
                Email = "cb_cntt@vinhuni.edu.vn",
                FullName = "Lê Thị Cán Bộ CNTT",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            var createResult = await userManager.CreateAsync(cbCntt, "CanBo@123");
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(cbCntt, AppRoles.FacultyManager);
                var fmRole = await roleManager.FindByNameAsync(AppRoles.FacultyManager);
                var cnttFaculty = await context.Faculties.FirstOrDefaultAsync(f => f.Code == "CNTT");
                if (fmRole != null && cnttFaculty != null && adminUser != null)
                {
                    context.UserRoleScopes.Add(new UserRoleScope
                    {
                        UserId = cbCntt.Id,
                        RoleId = fmRole.Id,
                        ScopeType = ScopeTypes.Faculty,
                        FacultyId = cnttFaculty.Id,
                        GrantedAt = DateTime.UtcNow,
                        GrantedByUserId = adminUser.Id
                    });
                    await context.SaveChangesAsync();
                }
            }
        }

        // 10. Seed Student Classes (student_classes)
        var cnttMajor = await context.Majors.FirstOrDefaultAsync(m => m.Code == "7480201");
        var ktpmMajor = await context.Majors.FirstOrDefaultAsync(m => m.Code == "7480103");
        if (cnttMajor != null && !await context.StudentClasses.AnyAsync())
        {
            var classes = new List<StudentClass>
            {
                new() { MajorId = ktpmMajor?.Id ?? cnttMajor.Id, Code = "63KTPM", Name = "Lớp Kỹ thuật Phần mềm K63", Cohort = "Khóa 63", IsActive = true },
                new() { MajorId = cnttMajor.Id, Code = "63CNTT", Name = "Lớp Công nghệ Thông tin K63", Cohort = "Khóa 63", IsActive = true }
            };
            await context.StudentClasses.AddRangeAsync(classes);
            await context.SaveChangesAsync();
        }

        // 11. Seed Student Profile for 'student'
        if (studentUser != null)
        {
            var studentProfile = await context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == studentUser.Id);
            if (studentProfile == null && cnttMajor != null)
            {
                var sampleClass = await context.StudentClasses.FirstOrDefaultAsync();
                studentProfile = new StudentProfile
                {
                    UserId = studentUser.Id,
                    StudentCode = "2011060001",
                    MajorId = cnttMajor.Id,
                    ClassId = sampleClass?.Id,
                    Skills = "C#, ASP.NET Core, React, SQL, Git",
                    Interests = "Phát triển ứng dụng Web, Cloud Computing, Trí tuệ nhân tạo",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.StudentProfiles.Add(studentProfile);
                await context.SaveChangesAsync();

                // Seed GPA records
                var terms = await context.Terms.OrderBy(t => t.StartDate).Take(2).ToListAsync();
                if (terms.Count > 0)
                {
                    context.StudentGpaRecords.Add(new StudentGpaRecord
                    {
                        StudentProfileId = studentProfile.Id,
                        TermId = terms[0].Id,
                        Gpa4 = 3.45m,
                        Gpa10 = 8.20m,
                        SourceType = GpaSourceTypes.Manual,
                        RecordedByUserId = adminUser?.Id,
                        RecordedAt = DateTime.UtcNow
                    });
                }
                if (terms.Count > 1)
                {
                    context.StudentGpaRecords.Add(new StudentGpaRecord
                    {
                        StudentProfileId = studentProfile.Id,
                        TermId = terms[1].Id,
                        Gpa4 = 3.65m,
                        Gpa10 = 8.55m,
                        SourceType = GpaSourceTypes.Manual,
                        RecordedByUserId = adminUser?.Id,
                        RecordedAt = DateTime.UtcNow
                    });
                }
                await context.SaveChangesAsync();
            }
        }

        // 12. Seed Lecturer Profile for 'giangvien' and 'giangvien_demo'
        var cnttFac = await context.Faculties.FirstOrDefaultAsync(f => f.Code == "CNTT");
        if (cnttFac != null)
        {
            var targetLecturers = new[] { lecturerUser, gvDemo };
            foreach (var lUser in targetLecturers)
            {
                if (lUser == null) continue;
                var lp = await context.LecturerProfiles.FirstOrDefaultAsync(p => p.UserId == lUser.Id);
                if (lp == null)
                {
                    lp = new LecturerProfile
                    {
                        UserId = lUser.Id,
                        LecturerCode = lUser.UserName == "giangvien" ? "GV_CNTT01" : "GV_DEMO02",
                        FacultyId = cnttFac.Id,
                        DepartmentName = "Bộ môn Kỹ thuật Phần mềm",
                        AcademicDegree = "Tiến sĩ",
                        AcademicTitle = "Phó Giáo sư",
                        Expertise = "Kỹ nghệ phần mềm, Hệ thống phân tán, Kiến trúc Microservices",
                        ResearchInterests = "Điện toán đám mây, Học máy ứng dụng, Tự động hóa kiểm thử",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.LecturerProfiles.Add(lp);
                    await context.SaveChangesAsync();
                }
            }
        }

        // 13. Ensure an OPEN Campaign exists for testing
        var openCampaign = await context.Campaigns.FirstOrDefaultAsync(c => c.Status == CampaignStatuses.Open);
        if (openCampaign == null)
        {
            var term1 = await context.Terms.OrderByDescending(t => t.StartDate).FirstOrDefaultAsync();
            if (term1 != null && cnttMajor != null && adminUser != null)
            {
                openCampaign = new Campaign
                {
                    Code = "DA_CNTT_2026_HK1",
                    Name = "Đồ án tốt nghiệp CNTT - Học kỳ 1 (2026-2027)",
                    TermId = term1.Id,
                    MajorId = cnttMajor.Id,
                    Kind = CampaignKinds.Project,
                    TrainingSystem = "CHINH_QUY",
                    StartDate = DateTime.UtcNow.Date,
                    EndDate = DateTime.UtcNow.Date.AddMonths(4),
                    Status = CampaignStatuses.Open,
                    CreatedByUserId = adminUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.Campaigns.Add(openCampaign);
                await context.SaveChangesAsync();
            }
        }

        // 14. Seed quota for lecturer in this campaign if missing
        if (openCampaign != null)
        {
            var lp = await context.LecturerProfiles.FirstOrDefaultAsync();
            if (lp != null)
            {
                var quota = await context.LecturerCampaignQuotas
                    .FirstOrDefaultAsync(q => q.LecturerProfileId == lp.Id && q.CampaignId == openCampaign.Id);
                if (quota == null)
                {
                    context.LecturerCampaignQuotas.Add(new LecturerCampaignQuota
                    {
                        LecturerProfileId = lp.Id,
                        CampaignId = openCampaign.Id,
                        MaxStudents = 5,
                        AssignedByUserId = adminUser?.Id,
                        UpdatedAt = DateTime.UtcNow
                    });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
