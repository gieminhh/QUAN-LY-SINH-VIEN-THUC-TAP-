using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using InternshipManagement.Web.Controllers;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;

// Never run against an application database. Use a disposable, separately initialized mysqld.
var connection = Environment.GetEnvironmentVariable("MATCHING_TEST_MYSQL") ?? throw new Exception("Set MATCHING_TEST_MYSQL.");
var settings = new MySqlConnectionStringBuilder(connection);
if (settings.Server != "127.0.0.1" || settings.Port != 33324 || settings.Database != "issue24_test" ||
    Environment.GetEnvironmentVariable("MATCHING_ALLOW_FIXTURES") != "1")
    throw new Exception("Only disposable 127.0.0.1:33324/issue24_test is supported.");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(MatchingPreviewController).Assembly.GetName().Name });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseMySQL(connection));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "Fixture"; o.DefaultChallengeScheme = "Fixture"; o.DefaultForbidScheme = "Fixture"; })
    .AddScheme<AuthenticationSchemeOptions, FixtureAuthentication>("Fixture", _ => { });
builder.Services.AddAuthorization(o => o.AddPolicy("RequireCampaignManager", p => p.RequireRole(AppRoles.Admin, AppRoles.TrainingManager, AppRoles.FacultyManager)));
builder.Services.AddScoped<IUserScopeService, UserScopeService>();
builder.Services.AddScoped<IMatchingPreviewService, MatchingPreviewService>();
builder.Services.AddScoped<ITopicAssignmentService, TopicAssignmentService>();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(MatchingPreviewController).Assembly);
await using var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=MatchingPreview}/{action=Index}/{id?}");

async Task<T> Db<T>(Func<ApplicationDbContext, Task<T>> action)
{
    await using var scope = app.Services.CreateAsyncScope();
    return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
}
async Task<T> Service<T>(Func<IMatchingPreviewService, Task<T>> action)
{
    await using var scope = app.Services.CreateAsyncScope();
    return await action(scope.ServiceProvider.GetRequiredService<IMatchingPreviewService>());
}
var checks = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }

await Db(async db =>
{
    if (await db.Database.CanConnectAsync()) throw new Exception("Fixture database already exists; use a fresh disposable server.");
    await db.Database.EnsureCreatedAsync();
    var faculty = new Faculty { Code = "TEST", Name = "Khoa thử" };
    var major = new Major { Faculty = faculty, Code = "TEST", Name = "Ngành thử" };
    var year = new AcademicYear { Code = "2026-2027", StartDate = new(2026, 1, 1), EndDate = new(2027, 12, 31) };
    var term = new Term { AcademicYear = year, TermNo = 1, StartDate = year.StartDate, EndDate = year.EndDate };
    db.AddRange(major, term);
    foreach (var role in AppRoles.AllRoles) db.Roles.Add(new IdentityRole { Id = role, Name = role, NormalizedName = role.ToUpperInvariant() });
    foreach (var (id, role) in new[] { ("admin", AppRoles.Admin), ("staff", AppRoles.FacultyManager), ("unscoped", AppRoles.FacultyManager), ("lecturer", AppRoles.Lecturer), ("s1", AppRoles.Student), ("s2", AppRoles.Student), ("s3", AppRoles.Student) })
    {
        db.Users.Add(new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(), FullName = id, Major = major });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = role });
    }
    await db.SaveChangesAsync();
    var campaign = new Campaign { Code = "TEST24", Name = "Ghép thử", TermId = term.Id, MajorId = major.Id, CreatedByUserId = "admin", Status = CampaignStatuses.Closed, RandomSeed = 12 };
    db.Add(campaign);
    await db.SaveChangesAsync();
    db.Add(new UserRoleScope { UserId = "staff", RoleId = AppRoles.FacultyManager, ScopeType = ScopeTypes.Campaign, CampaignId = campaign.Id });
    var topic = new Topic { CampaignId = campaign.Id, MajorId = major.Id, Title = "Đề tài thử", Description = "Test only", CreatedByUserId = "lecturer", ApprovalStatus = TopicApprovalStatuses.Approved, MaxStudents = 1 };
    db.Add(topic);
    await db.SaveChangesAsync();
    db.StudentTopicPreferences.AddRange(new[] { "s1", "s2" }.Select((s, i) => new StudentTopicPreference { StudentId = s, TopicId = topic.Id, Priority = 1, CreatedAt = new DateTime(2026, 11, 25, 8, i, 0) }));
    await db.SaveChangesAsync();
    return true;
});
var campaignId = await Db(db => db.Campaigns.Select(c => c.Id).SingleAsync());
var topicId = await Db(db => db.Topics.Select(t => t.Id).SingleAsync());
var preview = (await Service(s => s.PreviewAsync(campaignId)))!;
Check(preview.Matched == 1 && preview.Unmatched == 1 && preview.PendingManual == 1, "MySQL preview applies capacity and no-preference policy");
Check(await Db(db => db.TopicStudentAssignments.CountAsync()) == 0, "preview performs no assignment writes");

await app.StartAsync();
try
{
    using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(app.Urls.Single()) };
    async Task<HttpResponseMessage> Get(string? actor)
    {
        client.DefaultRequestHeaders.Remove("X-Test-User");
        if (actor is not null) client.DefaultRequestHeaders.Add("X-Test-User", actor);
        return await client.GetAsync($"/MatchingPreview/Index/{campaignId}");
    }
    Check((await Get(null)).StatusCode == HttpStatusCode.Unauthorized, "anonymous cannot read preview");
    Check((await Get("s1")).StatusCode == HttpStatusCode.Forbidden, "student cannot read preview");
    Check((await Get("lecturer")).StatusCode == HttpStatusCode.Forbidden, "lecturer cannot read preview");
    Check((await Get("unscoped")).StatusCode == HttpStatusCode.Forbidden, "manager outside campaign scope cannot read preview");
    var page = await Get("staff");
    var html = await page.Content.ReadAsStringAsync();
    if (page.StatusCode != HttpStatusCode.OK) Console.WriteLine($"Preview HTTP status: {page.StatusCode}\n{html}");
    Check(page.StatusCode == HttpStatusCode.OK && html.Contains("NV1") && (html.Contains("33.33") || html.Contains("33,33")), "scoped staff can render preview and NV statistics");
    string Field(string name) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"" + name + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
    var csrf = Field("__RequestVerificationToken");
    var token = Field("token");
    Check(csrf.Length > 0 && token.Length > 0, "confirmation form includes CSRF and protected preview tokens");
    async Task<HttpResponseMessage> Post(string value, bool includeCsrf = true) => await client.PostAsync($"/MatchingPreview/Confirm/{campaignId}",
        new FormUrlEncodedContent(includeCsrf ? new Dictionary<string, string> { ["token"] = value, ["__RequestVerificationToken"] = csrf } : new Dictionary<string, string> { ["token"] = value }));
    Check((await Post(token, false)).StatusCode == HttpStatusCode.BadRequest, "confirmation rejects missing anti-forgery token");
    Check((await Post("tampered")).StatusCode == HttpStatusCode.BadRequest, "confirmation rejects forged preview token");
    await Db(async db => { db.UserRoleScopes.RemoveRange(db.UserRoleScopes.Where(s => s.UserId == "staff")); await db.SaveChangesAsync(); return true; });
    Check((await Post(token)).StatusCode == HttpStatusCode.Forbidden, "direct POST rechecks revoked campaign scope");
    await Db(async db => { db.Add(new UserRoleScope { UserId = "staff", RoleId = AppRoles.FacultyManager, ScopeType = ScopeTypes.Campaign, CampaignId = campaignId }); await db.SaveChangesAsync(); return true; });

    await Db(async db => { var t = await db.Topics.SingleAsync(); t.MaxStudents = 2; await db.SaveChangesAsync(); return true; });
    var stale = await Service(s => s.ConfirmAsync(campaignId, preview.Input.Seed, preview.Input.Fingerprint));
    Check(!stale.Ok && await Db(db => db.TopicStudentAssignments.CountAsync()) == 0, "changed capacity rejects stale preview without partial writes");
    preview = (await Service(s => s.PreviewAsync(campaignId)))!;
    await Db(async db => { var p = await db.StudentTopicPreferences.FirstAsync(); p.Priority = 3; await db.SaveChangesAsync(); return true; });
    Check(!(await Service(s => s.ConfirmAsync(campaignId, preview.Input.Seed, preview.Input.Fingerprint))).Ok, "changed preferences invalidate reviewed preview");
    preview = (await Service(s => s.PreviewAsync(campaignId)))!;
    await Db(async db => { await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_test_assignment BEFORE INSERT ON topic_student_assignments FOR EACH ROW BEGIN IF NEW.student_user_id = 's2' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Fixture forced failure'; END IF; END"); return true; });
    var failed = false;
    try { await Service(s => s.ConfirmAsync(campaignId, preview.Input.Seed, preview.Input.Fingerprint)); }
    catch (DbUpdateException) { failed = true; }
    Check(failed && await Db(db => db.TopicStudentAssignments.CountAsync()) == 0, "database failure rolls back all results with no partial assignments");
    await Db(async db => { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_test_assignment"); return true; });
    var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Service(s => s.ConfirmAsync(campaignId, preview.Input.Seed, preview.Input.Fingerprint))));
    Check(results.Count(r => r.Ok) == 1, "only one concurrent confirmation succeeds");
    Check(await Db(db => db.TopicStudentAssignments.CountAsync()) == 3 && await Db(db => db.TopicStudentAssignments.CountAsync(a => a.Status == MatchStatus.Matched)) == 2, "confirmation atomically saves exact preview, including pending manual");
    Check(!(await Service(s => s.ConfirmAsync(campaignId, preview.Input.Seed, preview.Input.Fingerprint))).Ok, "replayed confirmation is rejected");
    Check(!(await Service(s => s.PreviewAsync(campaignId)))!.CanConfirm, "saved results are preserved with no changes to confirm");

    await Db(async db => { db.TopicStudentAssignments.RemoveRange(db.TopicStudentAssignments); await db.SaveChangesAsync(); return true; });
    page = await Get("staff"); html = await page.Content.ReadAsStringAsync(); csrf = Field("__RequestVerificationToken"); token = Field("token");
    Check((await Post(token)).StatusCode == HttpStatusCode.Redirect && await Db(db => db.TopicStudentAssignments.CountAsync()) == 3, "authorized HTTP confirmation saves results");

    await Db(async db => { db.TopicStudentAssignments.RemoveRange(db.TopicStudentAssignments); var c = await db.Campaigns.SingleAsync(); c.RandomSeed = null; c.MatchingOrder = MatchingOrder.Random; await db.SaveChangesAsync(); return true; });
    preview = (await Service(s => s.PreviewAsync(campaignId)))!;
    Check((await Service(s => s.ConfirmAsync(campaignId, preview.Input.Seed, preview.Input.Fingerprint))).Ok, "unseeded random preview keeps its generated seed through confirmation");
    var saved = await Db(db => db.TopicStudentAssignments.OrderBy(a => a.StudentUserId).Select(a => new { a.StudentUserId, a.TopicId, a.Status }).ToListAsync());
    Check(saved.All(a => preview.Rows.Any(r => r.Student.Id == a.StudentUserId && r.TopicId == a.TopicId && r.Status == a.Status)), "random persisted result exactly matches what staff reviewed");
}
finally { await app.StopAsync(); }
Console.WriteLine($"All {checks} isolated MySQL and HTTP checks passed.");

// Test host only; never registered in the production application.
class FixtureAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    UserManager<ApplicationUser> users) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var id = Request.Headers["X-Test-User"].ToString();
        var user = await users.FindByIdAsync(id);
        if (user is null) return AuthenticateResult.NoResult();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, user.UserName!) };
        claims.AddRange((await users.GetRolesAsync(user)).Select(r => new Claim(ClaimTypes.Role, r)));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme)), Scheme.Name));
    }
}
