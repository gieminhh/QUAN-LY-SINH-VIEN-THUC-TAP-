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
    }
}
