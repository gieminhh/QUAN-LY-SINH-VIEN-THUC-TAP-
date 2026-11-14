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

    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<Major> Majors => Set<Major>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<UserRoleScope> UserRoleScopes => Set<UserRoleScope>();
    public DbSet<LecturerCampaignQuota> LecturerCampaignQuotas => Set<LecturerCampaignQuota>();
    public DbSet<LecturerCampaignAssignment> LecturerCampaignAssignments => Set<LecturerCampaignAssignment>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<TopicLecturer> TopicLecturers => Set<TopicLecturer>();
    public DbSet<TopicAttachment> TopicAttachments => Set<TopicAttachment>();
    public DbSet<TopicProposal> TopicProposals => Set<TopicProposal>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Map ASP.NET Core Identity tables
        builder.Entity<ApplicationUser>(b =>
        {
            b.ToTable("AspNetUsers");
            b.Property(u => u.FullName).HasMaxLength(180).IsRequired();
            b.Property(u => u.IsActive).HasDefaultValue(true);
            b.Property(u => u.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            b.Property(u => u.ClassName).HasColumnName("class_name");
            b.Property(u => u.MajorId).HasColumnName("major_id");
            b.Property(u => u.Skills).HasColumnName("skills");
            b.Property(u => u.Interests).HasColumnName("interests");
            b.Property(u => u.CvStoredFileName).HasColumnName("cv_stored_file_name");
            b.Property(u => u.LecturerSpecialty).HasColumnName("lecturer_specialty");
            b.HasOne(u => u.Major)
             .WithMany()
             .HasForeignKey(u => u.MajorId)
             .OnDelete(DeleteBehavior.SetNull);
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

        // Business Tables
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

        builder.Entity<LecturerCampaignQuota>(b =>
        {
            b.ToTable("lecturer_campaign_quotas");
            b.HasIndex(q => new { q.CampaignId, q.LecturerUserId }).IsUnique();
            b.HasOne(q => q.Campaign)
             .WithMany()
             .HasForeignKey(q => q.CampaignId)
             .OnDelete(DeleteBehavior.Cascade);
            b.HasOne(q => q.Lecturer)
             .WithMany(u => u.LecturerCampaignQuotas)
             .HasForeignKey(q => q.LecturerUserId)
             .OnDelete(DeleteBehavior.Cascade);
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
        // Topic
        builder.Entity<Topic>(b =>
        {
            b.ToTable("topics");

            b.HasIndex(t => new { t.CampaignId, t.Status });
            b.HasIndex(t => t.CreatedByUserId);
            b.HasIndex(t => t.SourceTopicId);

            b.HasOne(t => t.Campaign)
            .WithMany(c => c.Topics)
            .HasForeignKey(t => t.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(t => t.CreatedByUser)
            .WithMany(u => u.CreatedTopics)
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.SourceTopic)
            .WithMany(t => t.DerivedTopics)
            .HasForeignKey(t => t.SourceTopicId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        // Topic Lecturer
        builder.Entity<TopicLecturer>(b =>
        {
            b.ToTable("topic_lecturers");

            b.HasIndex(t => new { t.TopicId, t.LecturerUserId })
            .IsUnique();

            b.HasIndex(t => new { t.TopicId, t.IsPrimary });

            b.HasOne(t => t.Topic)
            .WithMany(t => t.Lecturers)
            .HasForeignKey(t => t.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(t => t.Lecturer)
            .WithMany(u => u.TopicLectures)
            .HasForeignKey(t => t.LecturerUserId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        // Topic Attachment
        builder.Entity<TopicAttachment>(b =>
        {
            b.ToTable("topic_attachments");

            b.HasIndex(a => a.TopicId);

            b.HasOne(a => a.Topic)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.UploadedByUser)
            .WithMany(u => u.UploadedTopicAttachments)
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TopicProposal>(b =>
        {
            b.ToTable("topic_proposals");
            b.HasIndex(p => new { p.LecturerUserId, p.Status });
            b.HasIndex(p => new { p.StudentUserId, p.CreatedAt });
            b.HasIndex(p => new { p.CampaignId, p.Status });

            b.HasOne(p => p.Campaign)
             .WithMany()
             .HasForeignKey(p => p.CampaignId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Student)
             .WithMany()
             .HasForeignKey(p => p.StudentUserId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(p => p.Lecturer)
             .WithMany()
             .HasForeignKey(p => p.LecturerUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
