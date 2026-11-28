using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services.Matching;

namespace InternshipManagement.Web.ViewModels.Assignments;

public sealed class PublicationReviewPage
{
    public List<Campaign> Campaigns { get; set; } = [];
    public Campaign? Campaign { get; set; }
    public List<MatchingRound> Rounds { get; set; } = [];
    public PublicationPlan? Plan { get; set; }
    public string Token { get; set; } = "";
    public bool CanPublish { get; set; }
    public DateTime UtcNow { get; set; }
}

public sealed class StudentMatchingPage
{
    public List<Campaign> Campaigns { get; set; } = [];
    public Campaign? Campaign { get; set; }
    public MatchingRound? PublishedRound { get; set; }
    public MatchingPublishedResult? Result { get; set; }
    public MatchingRound? SupplementaryRound { get; set; }
    public bool CanRegister { get; set; }
    public List<SupplementaryTopic> Topics { get; set; } = [];
    public List<MatchingRoundPreference> Preferences { get; set; } = [];
    public string PreferenceVersion { get; set; } = "";
}

public record SupplementaryTopic(ulong Id, string Title, int? Remaining);
