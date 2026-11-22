using InternshipManagement.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace InternshipManagement.Web.Data;
public static class ParticipationMapping
{
    public static void ConfigureParticipation(this ModelBuilder b)
    {
        b.Entity<ParticipationPolicy>().ToTable("campaign_participation_policies", t => t.HasCheckConstraint("chk_participation_group_size", "`MaxGroupSize` BETWEEN 2 AND 20"));
        b.Entity<ParticipationPolicy>().HasOne<Campaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ProjectRegistrationGroup>(e => {
            e.HasAlternateKey(x => new { x.CampaignId, x.Id });
            e.HasOne<Campaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.LeaderId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<CampaignParticipant>(e => {
            e.HasKey(x => new { x.CampaignId, x.StudentId });
            e.HasOne<Campaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ProjectRegistrationGroup>().WithMany().HasForeignKey(x => new { x.CampaignId, x.GroupId })
                .HasPrincipalKey(x => new { x.CampaignId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ProjectGroupInvitation>(e => {
            e.HasKey(x => new { x.GroupId, x.StudentId });
            e.HasOne<ProjectRegistrationGroup>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<LecturerPriorityList>(e => {
            e.HasKey(x => new { x.TopicId, x.LecturerId });
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne<Topic>().WithMany().HasForeignKey(x => x.TopicId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.LecturerId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<LecturerStudentPriority>(e => {
            e.ToTable("lecturer_student_priorities", t => t.HasCheckConstraint("chk_lecturer_priority_rank", "`Rank` > 0"));
            e.HasKey(x => new { x.TopicId, x.LecturerId, x.StudentId });
            e.HasIndex(x => new { x.TopicId, x.LecturerId, x.Rank }).IsUnique();
            e.HasOne<LecturerPriorityList>().WithMany().HasForeignKey(x => new { x.TopicId, x.LecturerId }).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
