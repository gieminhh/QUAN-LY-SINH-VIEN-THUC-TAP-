using Microsoft.AspNetCore.Authentication;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Support clean UTF-8 rendering for Vietnamese text across Razor views
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.All));

// Add Database Context with Oracle MySQL provider for EF Core 10
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySQL(connectionString);
});

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = ".VinhUni.Internship.Auth";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Events.OnValidatePrincipal = async context => {
        var users=context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user=context.Principal==null?null:await users.GetUserAsync(context.Principal);
        var claim=context.Principal?.FindFirst(users.Options.ClaimsIdentity.SecurityStampClaimType)?.Value;
        if(user==null||!user.IsActive||await users.IsLockedOutAsync(user)||claim!=user.SecurityStamp) {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        var signIn=context.HttpContext.RequestServices.GetRequiredService<SignInManager<ApplicationUser>>();
        context.ReplacePrincipal(await signIn.CreateUserPrincipalAsync(user));
    };
});

// Register Domain Services (Sprint 1 & Sprint 2)
builder.Services.AddScoped<IUserScopeService, UserScopeService>();
builder.Services.AddScoped<PasswordRecoveryMailer>();
builder.Services.AddScoped<ProfileAccessService>();
builder.Services.AddScoped<CampaignRulesService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IStudentProfileService, StudentProfileService>();
builder.Services.AddScoped<ILecturerProfileService, LecturerProfileService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<ITopicApprovalService, TopicApprovalService>();

// Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole(AppRoles.Admin));
    options.AddPolicy("RequireCampaignManager", policy => 
        policy.RequireRole(AppRoles.Admin, AppRoles.TrainingManager, AppRoles.FacultyManager));
    options.AddPolicy("RequireLecturer", policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.Lecturer));
    options.AddPolicy("RequireStudent", policy =>
        policy.RequireRole(AppRoles.Student));
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed initial roles, data, and default accounts
try
{
    await DbInitializer.SeedAsync(app.Services);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo dữ liệu ban đầu.");
}

// Configure HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
