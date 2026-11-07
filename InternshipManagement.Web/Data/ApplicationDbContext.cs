using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Sprint 1 DbSets
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Major> Majors => Set<Major>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<UserRoleScope> UserRoleScopes => Set<UserRoleScope>();

    public DbSet<LecturerCampaignAssignment> LecturerCampaignAssignments => Set<LecturerCampaignAssignment>();

    // Sprint 2 DbSets
    public DbSet<StudentClass> StudentClasses => Set<StudentClass>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<StudentGpaRecord> StudentGpaRecords => Set<StudentGpaRecord>();
    public DbSet<LecturerProfile> LecturerProfiles => Set<LecturerProfile>();
    public DbSet<LecturerCampaignQuota> LecturerCampaignQuotas => Set<LecturerCampaignQuota>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TopicApprovalHistory> TopicApprovalHistories => Set<TopicApprovalHistory>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();

    public DbSet<CampaignWindow> CampaignWindows => Set<CampaignWindow>();
    public DbSet<CampaignEligibilityPolicy> CampaignEligibilityPolicies => Set<CampaignEligibilityPolicy>();
    public DbSet<CampaignException> CampaignExceptions => Set<CampaignException>();
    public DbSet<StudentEligibilityEvidence> StudentEligibilityEvidence => Set<StudentEligibilityEvidence>();
    public DbSet<CompanyRegistration> Companies => Set<CompanyRegistration>();
    public DbSet<CompanyMentor> CompanyMentors => Set<CompanyMentor>();
    public DbSet<StudentAssignment> StudentAssignments => Set<StudentAssignment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<CampaignWindow>().HasIndex(x=>new {x.CampaignId,x.Stage}).IsUnique();
        builder.Entity<CampaignEligibilityPolicy>().HasIndex(x=>x.CampaignId).IsUnique();
        builder.Entity<StudentEligibilityEvidence>().HasIndex(x=>new {x.CampaignId,x.StudentProfileId}).IsUnique();
        builder.Entity<CompanyRegistration>().HasIndex(x=>x.RegistrationCode).IsUnique();
        builder.Entity<CompanyMentor>().HasIndex(x=>new {x.CompanyId,x.UserId}).IsUnique();
        builder.Entity<StudentAssignment>().HasIndex(x=>new {x.CampaignId,x.StudentProfileId}).IsUnique();


        // Map ASP.NET Core Identity tables
        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("AspNetUsers");
            b.Property(u => u.FullName).HasMaxLength(180).IsRequired();
            b.Property(u => u.IsActive).HasDefaultValue(true);
            b.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });

        builder.Entity<IdentityRole>(b =>
        {
            b.ToTable("AspNetRoles");
        });

        builder.Entity<IdentityUserRole<string>>(b =>
        {
            b.ToTable("AspNetUserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(b =>
        {
            b.ToTable("AspNetUserClaims");
        });

        builder.Entity<IdentityRoleClaim<string>>(b =>
        {
            b.ToTable("AspNetRoleClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(b =>
        {
            b.ToTable("AspNetUserLogins");
        });

        builder.Entity<IdentityUserToken<string>>(b =>
        {
            b.ToTable("AspNetUserTokens");
        });

        // Sprint 1 Business Tables
        builder.Entity<Faculty>(b =>
        {
            b.ToTable("faculties");
            b.HasIndex(f => f.Code).IsUnique();
        });

        builder.Entity<Major>(b =>
        {
            b.ToTable("majors");
            b.HasIndex(m => m.Code).IsUnique();
            b.HasOne(m => m.Faculty)
             .WithMany(f => f.Majors)
             .HasForeignKey(m => m.FacultyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AcademicYear>(b =>
        {
            b.ToTable("academic_years");
            b.HasIndex(y => y.Code).IsUnique();
        });

        builder.Entity<Term>(b =>
        {
            b.ToTable("terms");
            b.HasIndex(t => new { t.AcademicYearId, t.TermNo }).IsUnique();
            b.HasOne(t => t.AcademicYear)
             .WithMany(y => y.Terms)
             .HasForeignKey(t => t.AcademicYearId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Campaign>(b =>
        {
            b.ToTable("campaigns");
            b.HasIndex(c => c.Code).IsUnique();
            b.HasOne(c => c.Term)
             .WithMany(t => t.Campaigns)
             .HasForeignKey(c => c.TermId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.Major)
             .WithMany(m => m.Campaigns)
             .HasForeignKey(c => c.MajorId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(c => c.CreatedByUser)
             .WithMany(u => u.CreatedCampaigns)
             .HasForeignKey(c => c.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<UserRoleScope>(b =>
        {
            b.ToTable("user_role_scopes");

            // Ignore generated/computed column ScopeTargetId from INSERT/UPDATE
            b.Property(e => e.ScopeTargetId)
             .ValueGeneratedOnAddOrUpdate();
            b.Property(e => e.ScopeTargetId)
             .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
            b.Property(e => e.ScopeTargetId)
             .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

            b.HasOne(s => s.User)
             .WithMany(u => u.RoleScopes)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(s => s.Role)
             .WithMany()
             .HasForeignKey(s => s.RoleId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(s => s.Faculty)
             .WithMany(f => f.RoleScopes)
             .HasForeignKey(s => s.FacultyId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(s => s.Major)
             .WithMany(m => m.RoleScopes)
             .HasForeignKey(s => s.MajorId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(s => s.Campaign)
             .WithMany(c => c.RoleScopes)
             .HasForeignKey(s => s.CampaignId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(s => s.GrantedByUser)
             .WithMany(u => u.GrantedRoleScopes)
             .HasForeignKey(s => s.GrantedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Sprint 2 Business Tables
        builder.Entity<StudentClass>(b =>
        {
            b.ToTable("student_classes");
            b.HasIndex(c => c.Code).IsUnique();
            b.HasOne(c => c.Major)
             .WithMany()
             .HasForeignKey(c => c.MajorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StudentProfile>(b =>
        {
            b.ToTable("student_profiles");
            b.HasIndex(p => p.UserId).IsUnique();
            b.HasIndex(p => p.StudentCode).IsUnique();

            b.HasOne(p => p.User)
             .WithOne(u => u.StudentProfile)
             .HasForeignKey<StudentProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.Major)
             .WithMany()
             .HasForeignKey(p => p.MajorId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Class)
             .WithMany(c => c.StudentProfiles)
             .HasForeignKey(p => p.ClassId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<StudentGpaRecord>(b =>
        {
            b.ToTable("student_gpa_records");
            b.HasIndex(g => new { g.StudentProfileId, g.TermId }).IsUnique();

            b.HasOne(g => g.StudentProfile)
             .WithMany(p => p.GpaRecords)
             .HasForeignKey(g => g.StudentProfileId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(g => g.Term)
             .WithMany()
             .HasForeignKey(g => g.TermId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(g => g.RecordedByUser)
             .WithMany()
             .HasForeignKey(g => g.RecordedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<LecturerProfile>(b =>
        {
            b.ToTable("lecturer_profiles");
            b.HasIndex(p => p.UserId).IsUnique();
            b.HasIndex(p => p.LecturerCode).IsUnique();

            b.HasOne(p => p.User)
             .WithOne(u => u.LecturerProfile)
             .HasForeignKey<LecturerProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(p => p.Faculty)
             .WithMany()
             .HasForeignKey(p => p.FacultyId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LecturerCampaignQuota>(b =>
        {
            b.ToTable("lecturer_campaign_quotas");
            b.HasIndex(q => new { q.LecturerProfileId, q.CampaignId }).IsUnique();

            b.HasOne(q => q.LecturerProfile)
             .WithMany(p => p.Quotas)
             .HasForeignKey(q => q.LecturerProfileId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(q => q.Campaign)
             .WithMany()
             .HasForeignKey(q => q.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(q => q.AssignedByUser)
             .WithMany()
             .HasForeignKey(q => q.AssignedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Topic>(b =>
        {
            b.ToTable("topics");
            b.HasIndex(t => new { t.CampaignId, t.Code }).IsUnique();

            b.HasOne(t => t.Campaign)
             .WithMany()
             .HasForeignKey(t => t.CampaignId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.LecturerProfile)
             .WithMany(p => p.Topics)
             .HasForeignKey(t => t.LecturerProfileId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicApprovalHistory>(b =>
        {
            b.ToTable("topic_approval_history");

            b.HasOne(h => h.Topic)
             .WithMany(t => t.ApprovalHistories)
             .HasForeignKey(h => h.TopicId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(h => h.ActorUser)
             .WithMany()
             .HasForeignKey(h => h.ActorUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<FileAttachment>(b =>
        {
            b.ToTable("file_attachments");

            b.HasOne(a => a.StudentProfile)
             .WithMany(p => p.Attachments)
             .HasForeignKey(a => a.StudentProfileId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.Topic)
             .WithMany(t => t.Attachments)
             .HasForeignKey(a => a.TopicId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.UploadedByUser)
             .WithMany()
             .HasForeignKey(a => a.UploadedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<LecturerCampaignAssignment>(b =>
        {
            b.ToTable("lecturer_campaign_assignments");
            b.HasIndex(a => new { a.CampaignId, a.StudentUserId, a.Status });
            b.HasIndex(a => new { a.CampaignId, a.LecturerUserId, a.Status });
            b.HasOne(a => a.Campaign)
             .WithMany()
             .HasForeignKey(a => a.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(a => a.Lecturer)
             .WithMany()
             .HasForeignKey(a => a.LecturerUserId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.Student)
             .WithMany()
             .HasForeignKey(a => a.StudentUserId)
             .OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.ReviewedByUser)
             .WithMany()
             .HasForeignKey(a => a.ReviewedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
