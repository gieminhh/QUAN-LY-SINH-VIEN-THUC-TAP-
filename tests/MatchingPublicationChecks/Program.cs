using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using InternshipManagement.Web.Controllers;
using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;

var connection = Environment.GetEnvironmentVariable("PUBLICATION_TEST_MYSQL") ?? throw new Exception("Set PUBLICATION_TEST_MYSQL.");
var settings = new MySqlConnectionStringBuilder(connection);
if (settings.Server != "127.0.0.1" || settings.Port != 33326 || settings.Database != "issue26_test" ||
    Environment.GetEnvironmentVariable("PUBLICATION_ALLOW_FIXTURES") != "1")
    throw new Exception("Only fresh disposable 127.0.0.1:33326/issue26_test is allowed.");
var clock = new FixtureClock(DateTimeOffset.UtcNow);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(MatchingPublicationsController).Assembly.GetName().Name });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseMySQL(connection));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddSingleton<TimeProvider>(clock);
builder.Services.AddScoped<IUserScopeService, UserScopeService>();
builder.Services.AddScoped<MatchingPublicationService>();
builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
builder.Services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "Fixture"; o.DefaultChallengeScheme = "Fixture"; o.DefaultForbidScheme = "Fixture"; })
    .AddScheme<AuthenticationSchemeOptions, FixtureAuthentication>("Fixture", _ => { });
builder.Services.AddAuthorization(o => o.AddPolicy("RequireCampaignManager", p => p.RequireRole(AppRoles.Admin, AppRoles.TrainingManager, AppRoles.FacultyManager)));
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(MatchingPublicationsController).Assembly);
await using var app = builder.Build();
app.UseAuthentication(); app.UseAuthorization();
app.MapControllerRoute("default", "{controller=StudentMatching}/{action=Index}/{id?}");

async Task<T> Db<T>(Func<ApplicationDbContext, Task<T>> action) { await using var scope = app.Services.CreateAsyncScope(); return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()); }
async Task<T> Service<T>(Func<MatchingPublicationService, Task<T>> action) { await using var scope = app.Services.CreateAsyncScope(); return await action(scope.ServiceProvider.GetRequiredService<MatchingPublicationService>()); }
var checks = 0;
void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }

await Db(async db =>
{
    if (await db.Database.CanConnectAsync()) throw new Exception("Fixture schema already exists; use a fresh disposable database.");
    await db.Database.EnsureCreatedAsync();
    // Exercise the actual additive migration against an existing schema, not just EnsureCreated.
    await db.Database.ExecuteSqlRawAsync("DROP TABLE matching_round_preferences");
    await db.Database.ExecuteSqlRawAsync("DROP TABLE matching_published_results");
    await db.Database.ExecuteSqlRawAsync("DROP TABLE matching_rounds");
    await db.Database.ExecuteSqlRawAsync("CREATE TABLE __EFMigrationsHistory (MigrationId varchar(150) NOT NULL PRIMARY KEY, ProductVersion varchar(32) NOT NULL)");
    var migration = db.GetService<IMigrationsAssembly>().Migrations.Keys.Single(k => k.EndsWith("_AddMatchingPublication"));
    var sql = db.GetService<IMigrator>().GenerateScript("20261127152057_AddDoanhNghiep", migration);
    await using var mysql = new MySqlConnection(connection); await mysql.OpenAsync();
    new MySqlScript(mysql, sql).Execute();
    Check(await db.Set<MatchingRound>().CountAsync() == 0, "additive migration creates publication tables on existing schema");

    var faculty = new Faculty { Code = "TEST26", Name = "Khoa thử" };
    var major = new Major { Faculty = faculty, Code = "TEST26", Name = "Ngành thử" };
    var otherMajor = new Major { Faculty = faculty, Code = "OTHER26", Name = "Ngành khác" };
    var year = new AcademicYear { Code = "2026-2027", StartDate = clock.GetUtcNow().Date.AddDays(-30), EndDate = clock.GetUtcNow().Date.AddDays(60) };
    var term = new Term { AcademicYear = year, TermNo = 1, StartDate = year.StartDate, EndDate = year.EndDate };
    db.AddRange(major, otherMajor, term);
    foreach (var role in AppRoles.AllRoles) db.Roles.Add(new IdentityRole { Id = role, Name = role, NormalizedName = role.ToUpperInvariant() });
    foreach (var (id, role) in new[] { ("admin", AppRoles.Admin), ("staff", AppRoles.FacultyManager), ("unscoped", AppRoles.FacultyManager), ("lecturer", AppRoles.Lecturer), ("s1", AppRoles.Student), ("s2", AppRoles.Student), ("s3", AppRoles.Student), ("other", AppRoles.Student) })
    {
        db.Users.Add(new ApplicationUser { Id = id, UserName = id, NormalizedUserName = id.ToUpperInvariant(), FullName = "TEST26_" + id, Major = id == "other" ? otherMajor : major });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = id, RoleId = role });
    }
    await db.SaveChangesAsync();
    var campaign = new Campaign { Code = "TEST26", Name = "Ghép thử", TermId = term.Id, MajorId = major.Id, CreatedByUserId = "admin", Status = CampaignStatuses.Closed, StartDate = year.StartDate, EndDate = year.EndDate };
    var otherCampaign = new Campaign { Code = "OTHER26", Name = "Đợt khác", TermId = term.Id, MajorId = otherMajor.Id, CreatedByUserId = "admin", Status = CampaignStatuses.Closed, StartDate = year.StartDate, EndDate = year.EndDate };
    db.AddRange(campaign, otherCampaign); await db.SaveChangesAsync();
    db.Add(new UserRoleScope { UserId = "staff", RoleId = AppRoles.FacultyManager, ScopeType = ScopeTypes.Campaign, CampaignId = campaign.Id });
    var topic = new Topic { CampaignId = campaign.Id, MajorId = major.Id, Title = "PRIVATE26_A", Description = "Fixture", CreatedByUserId = "lecturer", ApprovalStatus = TopicApprovalStatuses.Approved, MaxStudents = 1 };
    db.AddRange(topic,
        new Topic { CampaignId = campaign.Id, MajorId = major.Id, Title = "PRIVATE26_B", Description = "Fixture", CreatedByUserId = "lecturer", ApprovalStatus = TopicApprovalStatuses.Approved, MaxStudents = 1 },
        new Topic { CampaignId = campaign.Id, MajorId = major.Id, Title = "PRIVATE26_REJECTED", Description = "Fixture", CreatedByUserId = "lecturer", ApprovalStatus = TopicApprovalStatuses.Rejected },
        new Topic { CampaignId = otherCampaign.Id, MajorId = otherMajor.Id, Title = "PRIVATE26_OTHER", Description = "Fixture", CreatedByUserId = "lecturer", ApprovalStatus = TopicApprovalStatuses.Approved });
    await db.SaveChangesAsync();
    db.AddRange(new TopicStudentAssignment { CampaignId = campaign.Id, StudentUserId = "s1", TopicId = topic.Id, Status = MatchStatus.Matched },
        new TopicStudentAssignment { CampaignId = campaign.Id, StudentUserId = "s2", Status = MatchStatus.PendingManual });
    db.Add(new StudentTopicPreference { StudentId = "s2", TopicId = topic.Id, Priority = 1 });
    await db.SaveChangesAsync(); return true;
});
var campaignId = await Db(db => db.Campaigns.Where(c => c.Code == "TEST26").Select(c => c.Id).SingleAsync());
var topics = await Db(db => db.Topics.OrderBy(t => t.Id).ToListAsync());
var a = topics.Single(t => t.Title == "PRIVATE26_A").Id;
var b = topics.Single(t => t.Title == "PRIVATE26_B").Id;
var rejected = topics.Single(t => t.Title == "PRIVATE26_REJECTED").Id;
var foreign = topics.Single(t => t.Title == "PRIVATE26_OTHER").Id;
var review = (await Service(s => s.Review(campaignId)))!;
Check(review.Rows.Count == 3 && review.Rows.Count(r => r.Status == MatchStatus.Matched) == 1 && await Db(db => db.Set<MatchingRound>().CountAsync()) == 0, "review is read-only and includes unmatched students");
Check(await Service(s => s.Publish("unscoped", campaignId, review.Input.Fingerprint)) != null && await Service(s => s.Publish("s1", campaignId, review.Input.Fingerprint)) != null, "service blocks publication without manager role and scope");

await app.StartAsync();
try
{
    using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(app.Urls.Single()) };
    void Actor(string? actor) { client.DefaultRequestHeaders.Remove("X-Test-User"); if (actor is not null) client.DefaultRequestHeaders.Add("X-Test-User", actor); }
    async Task<HttpResponseMessage> Get(string? actor, string controller) { Actor(actor); return await client.GetAsync($"/{controller}/Index?campaignId={campaignId}"); }
    string Field(string html, string name) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"" + name + "\"[^>]*value=\"([^\"]*)\"").Groups[1].Value);
    async Task<HttpResponseMessage> Post(string controller, string action, Dictionary<string, string> fields) => await client.PostAsync($"/{controller}/{action}", new FormUrlEncodedContent(fields));
    Check((await Get(null, "MatchingPublications")).StatusCode == HttpStatusCode.Unauthorized, "anonymous cannot review publication");
    Check((await Get("s1", "MatchingPublications")).StatusCode == HttpStatusCode.Forbidden, "student cannot access staff publication page");
    Check((await Get("unscoped", "MatchingPublications")).StatusCode == HttpStatusCode.Forbidden, "unscoped manager cannot review campaign");
    var studentPage = await Get("s1", "StudentMatching");
    Check(studentPage.StatusCode == HttpStatusCode.OK && !(await studentPage.Content.ReadAsStringAsync()).Contains("PRIVATE26_A"), "student cannot see unpublished internal pair");
    var response = await Get("staff", "MatchingPublications"); var html = await response.Content.ReadAsStringAsync();
    Check(response.StatusCode == HttpStatusCode.OK && html.Contains("PRIVATE26_A"), "scoped staff can render publication review");
    var csrf = Field(html, "__RequestVerificationToken"); var token = Field(html, "token");
    Check(csrf.Length > 0 && token.Length > 0, "publication form has anti-forgery and protected approval tokens");
    Check((await Post("MatchingPublications", "Publish", new() { ["campaignId"] = campaignId.ToString(), ["token"] = token })).StatusCode == HttpStatusCode.BadRequest, "publication requires anti-forgery token");
    Check((await Post("MatchingPublications", "Publish", new() { ["campaignId"] = campaignId.ToString(), ["token"] = "forged", ["__RequestVerificationToken"] = csrf })).StatusCode == HttpStatusCode.BadRequest, "publication rejects forged approval token");
    await Db(async db => { db.UserRoleScopes.RemoveRange(db.UserRoleScopes.Where(s => s.UserId == "staff")); await db.SaveChangesAsync(); return true; });
    Check((await Post("MatchingPublications", "Publish", new() { ["campaignId"] = campaignId.ToString(), ["token"] = token, ["__RequestVerificationToken"] = csrf })).StatusCode == HttpStatusCode.Forbidden, "publication POST rechecks revoked scope");
    await Db(async db => { db.Add(new UserRoleScope { UserId = "staff", RoleId = AppRoles.FacultyManager, ScopeType = ScopeTypes.Campaign, CampaignId = campaignId }); var t = await db.Topics.FindAsync(a); t!.MaxStudents = 2; await db.SaveChangesAsync(); return true; });
    Check(await Service(s => s.Publish("staff", campaignId, review.Input.Fingerprint)) != null && await Db(db => db.Set<MatchingRound>().CountAsync()) == 0, "changed input rejects stale approval without writes");
    await Db(async db => { var t = await db.Topics.FindAsync(a); t!.MaxStudents = 1; await db.SaveChangesAsync(); await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_publication BEFORE INSERT ON matching_published_results FOR EACH ROW BEGIN IF NEW.StudentId = 's2' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Fixture forced failure'; END IF; END"); return true; });
    var failed = false;
    try { await Service(s => s.Publish("staff", campaignId, review.Input.Fingerprint)); } catch (DbUpdateException) { failed = true; }
    Check(failed && await Db(db => db.Set<MatchingRound>().CountAsync()) == 0 && await Db(db => db.Set<MatchingPublishedResult>().CountAsync()) == 0, "publication database error rolls back round and all results");
    await Db(async db => { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_publication"); return true; });
    Check((await Post("MatchingPublications", "Publish", new() { ["campaignId"] = campaignId.ToString(), ["token"] = token, ["__RequestVerificationToken"] = csrf })).StatusCode == HttpStatusCode.Redirect && await Db(db => db.Set<MatchingPublishedResult>().CountAsync()) == 3, "authorized HTTP approval publishes complete snapshot");
    var round1 = await Db(db => db.Set<MatchingRound>().SingleAsync());
    Check(round1.PublishedBy == "staff" && round1.PublishedAt.HasValue && await Service(s => s.Publish("staff", campaignId, review.Input.Fingerprint)) != null, "publication records approver and rejects repeated approval");
    Check(await Service(s => s.Confirm("s2", round1.Id, a)) != null && await Service(s => s.Confirm("other", round1.Id, a)) != null && await Service(s => s.Confirm("s1", round1.Id, b)) != null, "confirmation cannot claim another student's pair or change topic");
    response = await Get("s1", "StudentMatching"); html = await response.Content.ReadAsStringAsync(); csrf = Field(html, "__RequestVerificationToken");
    Check(html.Contains("PRIVATE26_A") && !html.Contains("TEST26_s2") && !html.Contains("PRIVATE26_B"), "student only sees their own published pair");
    Check((await Post("StudentMatching", "Confirm", new() { ["campaignId"] = campaignId.ToString(), ["roundId"] = round1.Id.ToString(), ["topicId"] = a.ToString(), ["studentId"] = "s2", ["__RequestVerificationToken"] = csrf })).StatusCode == HttpStatusCode.Redirect &&
        await Db(db => db.Set<MatchingPublishedResult>().AnyAsync(r => r.StudentId == "s1" && r.ConfirmedAt != null)) && !await Db(db => db.Set<MatchingPublishedResult>().AnyAsync(r => r.StudentId == "s2" && r.ConfirmedAt != null)), "HTTP confirmation ignores forged student identity and confirms authenticated student only");
    var confirmed = await Db(db => db.Set<MatchingPublishedResult>().Where(r => r.StudentId == "s1").Select(r => r.ConfirmedAt).SingleAsync());
    Check(await Service(s => s.Confirm("s1", round1.Id, a)) == null && confirmed == await Db(db => db.Set<MatchingPublishedResult>().Where(r => r.StudentId == "s1").Select(r => r.ConfirmedAt).SingleAsync()), "repeated student confirmation is idempotent");

    await Db(async db => { var assignment = await db.TopicStudentAssignments.SingleAsync(x => x.StudentUserId == "s1"); assignment.TopicId = b; await db.SaveChangesAsync(); return true; });
    html = await (await Get("s1", "StudentMatching")).Content.ReadAsStringAsync();
    Check(html.Contains("PRIVATE26_A") && !html.Contains("PRIVATE26_B"), "private assignment edits never alter published student snapshot");
    Check(await Service(s => s.OpenSupplementary("staff", campaignId, round1.Id, clock.GetUtcNow().UtcDateTime.AddHours(1))) != null, "changed published pair blocks supplementary opening");
    await Db(async db => { var assignment = await db.TopicStudentAssignments.SingleAsync(x => x.StudentUserId == "s1"); assignment.TopicId = a; await db.SaveChangesAsync(); return true; });
    Check(await Service(s => s.OpenSupplementary("staff", campaignId, round1.Id, clock.GetUtcNow().UtcDateTime.AddMinutes(-1))) != null, "supplementary deadline must be in the future");
    var openings = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Service(s => s.OpenSupplementary("staff", campaignId, round1.Id, clock.GetUtcNow().UtcDateTime.AddHours(1)))));
    Check(openings.Count(e => e == null) == 1 && await Db(db => db.Set<MatchingRound>().CountAsync()) == 2, "concurrent opening creates only one supplementary round");
    var round2 = await Db(db => db.Set<MatchingRound>().OrderByDescending(r => r.Number).FirstAsync());
    var emptyVersion = MatchingPublicationService.PreferenceVersion([]);
    Check(await Service(s => s.SavePreferences("s1", round2.Id, [b], emptyVersion)) != null, "already matched student cannot enter supplementary round");
    Check(await Service(s => s.SavePreferences("other", round2.Id, [b], emptyVersion)) != null, "supplementary registration enforces student's major");
    Check(await Service(s => s.SavePreferences("s2", round2.Id, [b, b], emptyVersion)) != null && await Service(s => s.SavePreferences("s2", round2.Id, [a], emptyVersion)) != null &&
        await Service(s => s.SavePreferences("s2", round2.Id, [rejected], emptyVersion)) != null && await Service(s => s.SavePreferences("s2", round2.Id, [foreign], emptyVersion)) != null, "supplementary rejects duplicate, full, unapproved and other-campaign topics");
    response = await Get("s2", "StudentMatching"); html = await response.Content.ReadAsStringAsync(); csrf = Field(html, "__RequestVerificationToken");
    Check(response.StatusCode == HttpStatusCode.OK && html.Contains("PRIVATE26_B") && !html.Contains("PRIVATE26_A"), "unmatched student sees available supplementary topics only");
    Check((await Post("StudentMatching", "SavePreferences", new() { ["campaignId"] = campaignId.ToString(), ["roundId"] = round2.Id.ToString(), ["topicIds"] = b.ToString(), ["version"] = emptyVersion, ["__RequestVerificationToken"] = csrf })).StatusCode == HttpStatusCode.Redirect &&
        await Db(db => db.Set<MatchingRoundPreference>().AnyAsync(p => p.StudentId == "s2" && p.TopicId == b)), "student submits supplementary preferences through HTTP");
    Check(await Service(s => s.SavePreferences("s2", round2.Id, [], emptyVersion)) != null, "stale preferences cannot overwrite a newer submission");
    var ownPreferences = await Db(db => db.Set<MatchingRoundPreference>().AsNoTracking().Where(p => p.StudentId == "s2").ToListAsync());
    Check(await Service(s => s.SavePreferences("s2", round2.Id, [b], MatchingPublicationService.PreferenceVersion(ownPreferences))) == null &&
        await Db(db => db.StudentTopicPreferences.AnyAsync(p => p.StudentId == "s2" && p.TopicId == a && p.Priority == 1)), "student can edit with current version without changing main-round preferences");
    clock.Now = clock.Now.AddMinutes(1);
    Check(await Service(s => s.SavePreferences("s3", round2.Id, [b], emptyVersion)) == null, "second unmatched student may compete for same remaining place");
    review = (await Service(s => s.Review(campaignId)))!;
    Check(await Service(s => s.Publish("staff", campaignId, review.Input.Fingerprint)) != null, "cannot publish while supplementary registration is open");
    clock.Now = clock.Now.AddHours(2);
    Check(await Service(s => s.SavePreferences("s3", round2.Id, [], emptyVersion)) != null, "deadline locks supplementary preference edits");
    await Db(async db => { await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_supplementary BEFORE INSERT ON matching_published_results FOR EACH ROW BEGIN IF NEW.StudentId = 's2' THEN SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Fixture forced failure'; END IF; END"); return true; });
    failed = false;
    try { await Service(s => s.Publish("staff", campaignId, review.Input.Fingerprint)); } catch (DbUpdateException) { failed = true; }
    Check(failed && await Db(db => db.TopicStudentAssignments.CountAsync(x => x.Status == MatchStatus.Matched)) == 1 &&
        await Db(db => db.Set<MatchingPublishedResult>().CountAsync()) == 3 && await Db(db => db.Set<MatchingRound>().AnyAsync(r => r.Id == round2.Id && r.State == MatchingRoundState.RegistrationOpen)), "failed supplementary publication rolls back new pairs and preserves previous public snapshot");
    await Db(async db => { await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_supplementary"); return true; });
    var publications = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Service(s => s.Publish("staff", campaignId, review.Input.Fingerprint))));
    Check(publications.Count(e => e == null) == 1, "concurrent supplementary approvals publish once");
    var results2 = await Db(db => db.Set<MatchingPublishedResult>().Where(r => r.RoundId == round2.Id).ToListAsync());
    Check(results2.Single(r => r.StudentId == "s1").TopicId == a && results2.Single(r => r.StudentId == "s1").ConfirmedAt == confirmed &&
        results2.Single(r => r.StudentId == "s2").TopicId == b && results2.Single(r => r.StudentId == "s3").TopicId == null, "supplementary fills spare capacity in order and preserves earlier confirmed pair");
    Check(await Db(db => db.TopicStudentAssignments.CountAsync(x => x.Status == MatchStatus.Matched)) == 2 && await Db(db => db.Set<MatchingPublishedResult>().CountAsync()) == 6, "supplementary writes assignments and retains previous publication history");
    Check(await Service(s => s.Confirm("s1", round1.Id, a)) != null && await Service(s => s.Confirm("s2", round2.Id, b)) == null, "confirmation accepts current round only");
    Check(await Service(s => s.OpenSupplementary("staff", campaignId, round2.Id, clock.GetUtcNow().UtcDateTime.AddHours(1))) != null, "no supplementary round can open without remaining capacity");
}
finally { await app.StopAsync(); }
Console.WriteLine($"All {checks} isolated migration/MySQL/HTTP checks passed.");

sealed class FixtureClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}

// Only the test host registers this authentication handler; production uses Identity cookies.
sealed class FixtureAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    UserManager<ApplicationUser> users) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = await users.FindByIdAsync(Request.Headers["X-Test-User"].ToString());
        if (user is null) return AuthenticateResult.NoResult();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Name, user.UserName!) };
        claims.AddRange((await users.GetRolesAsync(user)).Select(r => new Claim(ClaimTypes.Role, r)));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, IdentityConstants.ApplicationScheme)), Scheme.Name));
    }
}
