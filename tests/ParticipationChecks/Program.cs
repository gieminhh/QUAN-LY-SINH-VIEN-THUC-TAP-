using InternshipManagement.Web.Data;
using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using MySql.EntityFrameworkCore.Extensions;

var connection = Environment.GetEnvironmentVariable("PARTICIPATION_TEST_MYSQL") ?? throw new Exception("Set PARTICIPATION_TEST_MYSQL.");
var settings = new MySqlConnectionStringBuilder(connection);
if (settings.Server != "127.0.0.1" || settings.Port != 33320 || settings.Database != "issue2021_demo" ||
    Environment.GetEnvironmentVariable("PARTICIPATION_ALLOW_FIXTURES") != "1") throw new Exception("Only the explicitly authorized disposable database at 127.0.0.1:33320/issue2021_demo is supported.");
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseMySQL(connection));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddScoped<IUserScopeService, UserScopeService>();
builder.Services.AddScoped<ParticipationService>();
await using var app = builder.Build();
async Task<T> Db<T>(Func<ApplicationDbContext, Task<T>> action) { await using var scope = app.Services.CreateAsyncScope(); return await action(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()); }
async Task<string?> Call(Func<ParticipationService, Task<string?>> action) { await using var scope = app.Services.CreateAsyncScope(); return await action(scope.ServiceProvider.GetRequiredService<ParticipationService>()); }
void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
var now = ParticipationService.VietnamNow;
if (args.Contains("--prepare")) {
    await Db(async db => { await db.Database.EnsureCreatedAsync(); return true; });
    await DbInitializer.SeedAsync(app.Services);
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var major = await db.Majors.OrderBy(m => m.Id).FirstAsync();
    var other = await db.Majors.Where(m => m.Id != major.Id).FirstAsync();
    foreach (var name in new[] { "s1", "s2", "s3", "s4", "other", "l1", "l2", "staff" }) {
        var user = new ApplicationUser { Id = "test20-"+name, UserName = "test20-"+name, Email = name+"@isolated.example", FullName = "Test "+name,
            MajorId = name == "other" ? other.Id : major.Id, IsActive = true, EmailConfirmed = true };
        var created = await users.CreateAsync(user, "Test20@123");
        if (!created.Succeeded) throw new Exception(string.Join(",", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, name.StartsWith("l") ? AppRoles.Lecturer : name == "staff" ? AppRoles.FacultyManager : AppRoles.Student);
    }
    var admin = await users.FindByNameAsync("admin") ?? throw new Exception("Seed admin missing");
    var term = await db.Terms.OrderBy(t => t.Id).FirstAsync();
    var campaigns = new[] {
        new Campaign { Code = "TEST20_PROJECT", Kind = CampaignKinds.Project },
        new Campaign { Code = "TEST20_INTERNSHIP", Kind = CampaignKinds.Internship }
    };
    foreach (var c in campaigns) { c.Name = c.Code; c.MajorId = major.Id; c.TermId = term.Id; c.StartDate = now.Date.AddDays(-2); c.EndDate = now.Date.AddDays(3); c.Status = CampaignStatuses.Open; c.CreatedByUserId = admin.Id; db.Add(c); }
    await db.SaveChangesAsync();
    foreach (var c in campaigns) db.Add(new ParticipationPolicy { CampaignId = c.Id, GroupsAllowed = c.Kind == CampaignKinds.Project, MaxGroupSize = 3,
        RegistrationOpensAt = now.AddDays(-1), RegistrationClosesAt = now.AddDays(1), LecturerRankingEnabled = true, RankingOpensAt = now.AddDays(-1), RankingClosesAt = now.AddDays(1) });
    var topic = new Topic { CampaignId = campaigns[0].Id, MajorId = major.Id, Title = "TEST21_TOPIC", Description = "Isolated acceptance fixture", CreatedByUserId = "test20-l1", ApprovalStatus = TopicApprovalStatuses.Approved, Status = TopicStatuses.Published };
    db.Add(topic); await db.SaveChangesAsync();
    db.AddRange(new[] { "s1", "s2", "other" }.Select(s => new StudentTopicPreference { StudentId = "test20-"+s, TopicId = topic.Id, Priority = 1 }));
    await db.SaveChangesAsync();
    Console.WriteLine("Prepared isolated fixtures. Apply the migration SQL to verify deployment before --check.");
    return;
}
var cp = await Db(db => db.Campaigns.Where(c => c.Code == "TEST20_PROJECT").Select(c => c.Id).SingleAsync());
var ip = await Db(db => db.Campaigns.Where(c => c.Code == "TEST20_INTERNSHIP").Select(c => c.Id).SingleAsync());
var topicId = await Db(db => db.Topics.Where(t => t.Title == "TEST21_TOPIC").Select(t => t.Id).SingleAsync());
var adminId = await Db(db => db.Users.Where(u => u.UserName == "admin").Select(u => u.Id).SingleAsync());
Check(await Call(s => s.CreateGroup("test20-s1", ip, "Invalid internship")) != null, "internship refuses group registration");
Check(await Call(s => s.RegisterIndividual("test20-s1", ip)) == null, "internship registers each student individually");
Check(await Call(s => s.CreateGroup("test20-other", cp, "Outside")) != null, "outside-major student cannot create a group");
Check(await Call(s => s.CreateGroup("test20-s1", cp, "Group One")) == null, "project group creation confirms leader");
Check(await Call(s => s.CreateGroup("test20-s2", cp, "Group Two")) == null, "second group registration");
var g1 = await Db(db => db.Set<ProjectRegistrationGroup>().Where(g => g.Name == "Group One").Select(g => g.Id).SingleAsync());
var g2 = await Db(db => db.Set<ProjectRegistrationGroup>().Where(g => g.Name == "Group Two").Select(g => g.Id).SingleAsync());
Check(await Call(s => s.Invite("test20-s2", g1, "test20-s3")) != null, "only group leader may invite");
Check(await Call(s => s.Invite("test20-s1", g1, "test20-other")) != null, "invitation rejects student outside campaign scope");
Check(await Call(s => s.Invite("test20-s1", g1, "test20-s3")) == null && await Call(s => s.Invite("test20-s2", g2, "test20-s3")) == null, "invites do not enroll a student automatically");
Check(!await Db(db => db.Set<CampaignParticipant>().AnyAsync(p => p.CampaignId == cp && p.StudentId == "test20-s3")), "pending invitations require member confirmation");
var responses = await Task.WhenAll(Call(s => s.Respond("test20-s3", g1, true)), Call(s => s.Respond("test20-s3", g2, true)));
Check(responses.Count(e => e == null) == 1 && await Db(db => db.Set<CampaignParticipant>().CountAsync(p => p.CampaignId == cp && p.StudentId == "test20-s3")) == 1, "concurrent confirmations cannot join two groups");
Check(await Call(s => s.RegisterIndividual("test20-s3", cp)) != null, "cannot enroll individually while already in a group");
Check(await Call(s => s.Leave("test20-s3", cp)) == null, "member may leave before deadline");
Check(await Call(s => s.Invite("test20-s1", g1, "test20-s4")) == null, "leader invites another student");
Check(await Call(s => s.Respond("test20-s4", g1, false)) == null && !await Db(db => db.Set<CampaignParticipant>().AnyAsync(p => p.CampaignId == cp && p.StudentId == "test20-s4")), "declined invitation never creates membership");
var policy = await Db(db => db.Set<ParticipationPolicy>().AsNoTracking().SingleAsync(p => p.CampaignId == cp));
policy.MaxGroupSize = 2; await Call(s => s.SavePolicy(adminId, policy));
var pendingG1 = await Db(db => db.Set<ProjectGroupInvitation>().AnyAsync(i => i.GroupId == g1 && i.StudentId == "test20-s3" && i.Status == "PENDING"));
Check((pendingG1 || await Call(s => s.Invite("test20-s1", g1, "test20-s3")) == null) && await Call(s => s.Respond("test20-s3", g1, true)) == null, "member may rejoin using a valid invitation after leaving");
Check(await Call(s => s.Invite("test20-s1", g1, "test20-s4")) == null && await Call(s => s.Respond("test20-s4", g1, true)) != null, "acceptance enforces group size including leader, even after invitation");
await Call(s => s.Leave("test20-s3", cp)); policy.MaxGroupSize = 3;

policy.GroupsAllowed = false;
Check(await Call(s => s.SavePolicy(adminId, policy)) != null, "cannot disable groups while registrations exist");
policy.GroupsAllowed = true;
Check(await Call(s => s.SavePolicy("test20-staff", policy)) != null, "manager without campaign scope cannot change policy");
policy.RegistrationClosesAt = now.AddMinutes(-1);
Check(await Call(s => s.SavePolicy(adminId, policy)) == null && await Call(s => s.Leave("test20-s1", cp)) != null && await Call(s => s.Invite("test20-s1", g1, "test20-s3")) != null, "registration deadline locks membership and invitations");
policy.RegistrationClosesAt = now.AddDays(1);
Check(await Call(s => s.SavePolicy(adminId, policy)) == null, "authorized manager can reopen registration");
Check(await Call(s => s.SaveRanking("test20-l2", topicId, ["test20-s1"], null, "Expertise")) != null, "lecturer cannot rank another lecturer's topic");
Check(await Call(s => s.SaveRanking("test20-l1", topicId, ["test20-other"], null, "Expertise")) != null && await Call(s => s.SaveRanking("test20-l1", topicId, ["test20-s3"], null, "Expertise")) != null, "ranking rejects wrong-major and students without a valid preference");
Check(await Call(s => s.SaveRanking("test20-l1", topicId, ["test20-s1", "test20-s1"], null, "Expertise")) != null, "ranking rejects duplicate students");
Check(await Call(s => s.SaveRanking("test20-l1", topicId, ["test20-s2", "test20-s1"], null, "")) != null, "ranking requires explicit criteria");
Check(await Call(s => s.SaveRanking("test20-l1", topicId, ["test20-s2", "test20-s1"], null, "Project interest and interview; GPA is not mandatory")) == null, "lecturer saves a manual non-GPA priority order");
Check((await Db(db => db.Set<LecturerStudentPriority>().Where(p => p.TopicId == topicId).OrderBy(p => p.Rank).Select(p => p.StudentId).ToListAsync())).SequenceEqual(new[] { "test20-s2", "test20-s1" }), "priority rank order persists exactly");
var version = await Db(db => db.Set<LecturerPriorityList>().Where(l => l.TopicId == topicId).Select(l => l.Version).SingleAsync());
var ranked = await Task.WhenAll(Call(s => s.SaveRanking("test20-l1", topicId, ["test20-s1"], version, "Interview")), Call(s => s.SaveRanking("test20-l1", topicId, ["test20-s2"], version, "Interest")));
Check(ranked.Count(e => e == null) == 1, "concurrent ranking saves reject stale forms");
version = await Db(db => db.Set<LecturerPriorityList>().Where(l => l.TopicId == topicId).Select(l => l.Version).SingleAsync());
policy.RankingClosesAt = now.AddMinutes(-1); await Call(s => s.SavePolicy(adminId, policy));
Check(await Call(s => s.SaveRanking("test20-l1", topicId, [], version, "Clear list")) != null && await Db(db => db.Set<LecturerStudentPriority>().AnyAsync(p => p.TopicId == topicId)), "ranking deadline preserves saved priorities");
policy.RankingClosesAt = now.AddDays(1); policy.LecturerRankingEnabled = false; await Call(s => s.SavePolicy(adminId, policy));
Check(await Call(s => s.SaveRanking("test20-l1", topicId, [], version, "Clear list")) != null, "ranking is optional per campaign and disabled by default");
policy.LecturerRankingEnabled = true; await Call(s => s.SavePolicy(adminId, policy));
Check(await Call(s => s.SaveRanking("test20-l1", topicId, [], version, "No preference currently")) == null, "lecturer can intentionally leave an empty priority list");
Check(await Call(s => s.Leave("test20-s1", cp)) == null && !await Db(db => db.Set<ProjectRegistrationGroup>().AnyAsync(g => g.Id == g1)), "leader can disband group before deadline");
Check(await Call(s => s.Respond("test20-s4", g1, true)) != null, "dissolved group cannot accept old invitations");
Check(await Call(s => s.RegisterIndividual("test20-s1", cp)) == null, "project permits individual choice alongside groups");
Console.WriteLine("All participation and priority checks passed.");
