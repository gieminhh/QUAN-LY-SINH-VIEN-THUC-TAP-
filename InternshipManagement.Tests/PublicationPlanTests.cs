using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services.Matching;

namespace InternshipManagement.Tests;

public class PublicationPlanTests
{
    private static readonly DateTime Now = new(2026, 11, 28, 8, 0, 0, DateTimeKind.Utc);
    private static PublicationInput Input() => new(1, CampaignStatuses.Closed, null, 1, null, MatchingOrder.RegistrationTime, 12,
        [new("a", "An", "SV01"), new("b", "Bình", "SV02"), new("c", "Chi", "SV03")],
        [new(1, "Đề tài A", 1, TopicApprovalStatuses.Approved), new(2, "Đề tài B", 1, TopicApprovalStatuses.Approved)],
        [new("a", 1, MatchStatus.Matched), new("b", null, MatchStatus.Unmatched)], [], []);

    [Fact]
    public void InitialPublicationIncludesUnassignedStudentsWithoutInventingPairs()
    {
        var result = PublicationPlanner.Build(Input());
        Assert.True(result.CanPublish(Now));
        Assert.Equal(3, result.Rows.Count);
        Assert.Single(result.Rows, r => r.Status == MatchStatus.Matched);
        Assert.Equal(2, result.Rows.Count(r => r.Status == MatchStatus.Unmatched));
    }

    [Theory]
    [InlineData(CampaignStatuses.Open)]
    [InlineData(CampaignStatuses.Draft)]
    [InlineData(CampaignStatuses.Archived)]
    public void CannotPublishBeforeClosingMainRegistrationOrAfterArchiving(string status)
    {
        Assert.False(PublicationPlanner.Build(Input() with { CampaignStatus = status }).CanPublish(Now));
    }

    [Fact]
    public void SupplementaryReservesExistingPlacesAndKeepsStudentConfirmation()
    {
        var input = Input() with { RoundId = 2, Number = 2, ClosesAt = Now,
            Previous = [new("a", 1, MatchStatus.Matched, Now.AddDays(-1))],
            Preferences = [new("b", 1, 1, Now.AddHours(-2)), new("b", 2, 2, Now.AddHours(-2)), new("c", 2, 1, Now.AddHours(-1))] };
        var result = PublicationPlanner.Build(input);
        Assert.Equal(1UL, result.Rows.Single(r => r.StudentId == "a").TopicId);
        Assert.Equal(Now.AddDays(-1), result.Rows.Single(r => r.StudentId == "a").ConfirmedAt);
        Assert.Equal(2UL, result.Rows.Single(r => r.StudentId == "b").TopicId);
        Assert.Null(result.Rows.Single(r => r.StudentId == "c").TopicId);
        Assert.True(result.CanPublish(Now));
        Assert.False(result.CanPublish(Now.AddTicks(-1)));
    }

    [Fact]
    public void ChangedPublishedPairBlocksFurtherPublication()
    {
        var input = Input() with { Previous = [new("a", 2, MatchStatus.Matched, null)] };
        Assert.False(PublicationPlanner.Build(input).CanPublish(Now));
    }

    [Fact]
    public void RejectsOverCapacityUnapprovedAndCrossCampaignTopics()
    {
        Assert.False(PublicationPlanner.Build(Input() with { Assignments = [new("a", 1, MatchStatus.Matched), new("b", 1, MatchStatus.Matched)] }).CanPublish(Now));
        Assert.False(PublicationPlanner.Build(Input() with { Assignments = [new("a", 99, MatchStatus.Matched)] }).CanPublish(Now));
        var input = Input();
        input.Topics[0] = input.Topics[0] with { ApprovalStatus = TopicApprovalStatuses.Rejected };
        Assert.False(PublicationPlanner.Build(input).CanPublish(Now));
    }

    [Fact]
    public void DuplicateStudentOrInvalidStatusBlocksPublication()
    {
        Assert.False(PublicationPlanner.Build(Input() with { Assignments = [new("a", 1, MatchStatus.Matched), new("a", 2, MatchStatus.Matched)] }).CanPublish(Now));
        Assert.False(PublicationPlanner.Build(Input() with { Assignments = [new("a", 1, MatchStatus.Unmatched)] }).CanPublish(Now));
        Assert.False(PublicationPlanner.Build(Input() with { Assignments = [new("a", null, (MatchStatus)99)] }).CanPublish(Now));
    }

    [Fact]
    public void StudentOutsideEligibleCohortBlocksPublication()
    {
        Assert.False(PublicationPlanner.Build(Input() with { Assignments = [new("outsider", 1, MatchStatus.Matched)] }).CanPublish(Now));
    }

    [Fact]
    public void RandomSupplementaryIsRepeatableAndDoesNotMutateSource()
    {
        var input = Input() with { RoundId = 2, Order = MatchingOrder.Random,
            Preferences = [new("b", 2, 1, Now), new("c", 2, 1, Now)] };
        var fingerprint = input.Fingerprint;
        var first = PublicationPlanner.Build(input);
        var second = PublicationPlanner.Build(input);
        Assert.Equal(first.Rows, second.Rows);
        Assert.Equal(fingerprint, input.Fingerprint);
        Assert.Single(input.Assignments, a => a.Status == MatchStatus.Matched);
        Assert.Single(first.Rows, r => r.TopicId == 2);
    }

    [Fact]
    public void NoSupplementaryPreferenceDoesNotTriggerAutomaticAssignment()
    {
        var result = PublicationPlanner.Build(Input() with { RoundId = 2 });
        Assert.Single(result.Rows, r => r.Status == MatchStatus.Matched);
    }

    [Fact]
    public void FingerprintIncludesCapacityResultsPreferencesAndConfirmations()
    {
        var input = Input();
        Assert.NotEqual(input.Fingerprint, (input with { Assignments = [] }).Fingerprint);
        Assert.NotEqual(input.Fingerprint, (input with { Topics = [] }).Fingerprint);
        Assert.NotEqual(input.Fingerprint, (input with { Preferences = [new("b", 2, 1, Now)] }).Fingerprint);
        Assert.NotEqual(input.Fingerprint, (input with { Previous = [new("a", 1, MatchStatus.Matched, Now)] }).Fingerprint);
    }

    [Fact]
    public void EmptyCohortCannotBePublished()
    {
        Assert.False(PublicationPlanner.Build(Input() with { Students = [], Assignments = [] }).CanPublish(Now));
    }
}
