using InternshipManagement.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Web.Data;

public static class MatchingPublicationMapping
{
    public static void ConfigureMatchingPublication(this ModelBuilder builder)
    {
        builder.Entity<MatchingRound>(e =>
        {
            e.HasIndex(r => new { r.CampaignId, r.Number }).IsUnique();
            e.HasOne<Campaign>().WithMany().HasForeignKey(r => r.CampaignId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.OpenedBy).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.PublishedBy).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<MatchingPublishedResult>(e =>
        {
            e.HasKey(r => new { r.RoundId, r.StudentId });
            e.HasOne<MatchingRound>().WithMany().HasForeignKey(r => r.RoundId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(r => r.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Topic>().WithMany().HasForeignKey(r => r.TopicId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<MatchingRoundPreference>(e =>
        {
            e.HasKey(p => new { p.RoundId, p.StudentId, p.TopicId });
            e.HasIndex(p => new { p.RoundId, p.StudentId, p.Priority }).IsUnique();
            e.HasOne<MatchingRound>().WithMany().HasForeignKey(p => p.RoundId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(p => p.StudentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Topic>().WithMany().HasForeignKey(p => p.TopicId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
