using InternshipManagement.Web.Models;
using InternshipManagement.Web.Services.Matching;

namespace InternshipManagement.Tests;

public class MatchingPreviewTests
{
    private static readonly DateTime Time = new(2026, 11, 25, 8, 0, 0, DateTimeKind.Utc);
    private static MatchingInput Input() => new(1, "Đợt thử", CampaignStatuses.Closed,
        MatchingOrder.RegistrationTime, NoPreferencePolicy.ManualByAdmin, 42,
        [new("a", "An", "SV01"), new("b", "Bình", "SV02"), new("c", "Chi", "SV03"), new("d", "Dung", "SV04")],
        [new(1, "Đề tài A", 1, TopicApprovalStatuses.Approved), new(2, "Đề tài B", 1, TopicApprovalStatuses.Approved), new(3, "Đề tài C", 1, TopicApprovalStatuses.Approved)],
        [new("a", 1, 1, Time), new("b", 1, 1, Time.AddMinutes(1)), new("b", 2, 2, Time.AddMinutes(1)),
         new("c", 1, 1, Time.AddMinutes(2)), new("c", 2, 2, Time.AddMinutes(2)), new("c", 3, 3, Time.AddMinutes(2))], []);

    [Fact]
    public void PreviewShowsRanksAndUsesAllStudentsAsStatisticsDenominator()
    {
        var result = MatchingPreviewPlanner.Build(Input());
        Assert.Equal(4, result.Total);
        Assert.Equal(3, result.Matched);
        Assert.Equal(1, result.PendingManual);
        Assert.Equal(new int?[] { 1, 2, 3, null }, result.Rows.Select(r => r.MatchedPriority));
        Assert.All(new[] { 1, 2, 3 }, p => Assert.Equal(25m, result.PriorityRate(p)));
        Assert.True(result.CanConfirm);
    }

    [Fact]
    public void RegistrationTimeWinsOverStudentIdAndInputOrder()
    {
        var input = Input() with { Preferences = [new("b", 1, 1, Time), new("a", 1, 1, Time.AddMinutes(1))] };
        var result = MatchingPreviewPlanner.Build(input);
        Assert.Equal(1UL, result.Rows.Single(r => r.Student.Id == "b").TopicId);
        Assert.Equal(MatchStatus.Unmatched, result.Rows.Single(r => r.Student.Id == "a").Status);
    }

    [Fact]
    public void ExistingMatchesArePreservedAndReserveCapacity()
    {
        var input = Input() with { Assignments = [new("d", 1, MatchStatus.Matched)] };
        var result = MatchingPreviewPlanner.Build(input);
        Assert.True(result.Rows.Single(r => r.Student.Id == "d").Existing);
        Assert.Equal(1UL, result.Rows.Single(r => r.Student.Id == "d").TopicId);
        Assert.Equal(MatchStatus.Unmatched, result.Rows.Single(r => r.Student.Id == "a").Status);
        Assert.Equal(0, result.PriorityCount(1)); // Existing match outside preferences is not NV1.
        Assert.Single(result.Rows, r => r.TopicId == 1);
    }

    [Fact]
    public void UnapprovedPreferencesRemainVisibleButAreNotUsedForMatching()
    {
        var input = Input();
        input.Topics[0] = input.Topics[0] with { ApprovalStatus = TopicApprovalStatuses.Rejected };
        var result = MatchingPreviewPlanner.Build(input);
        Assert.DoesNotContain(result.Rows, r => r.TopicId == 1);
        Assert.Single(result.Rows.Single(r => r.Student.Id == "a").Preferences);
        Assert.Equal(MatchStatus.PendingManual, result.Rows.Single(r => r.Student.Id == "a").Status);
    }

    [Theory]
    [InlineData(NoPreferencePolicy.LeaveUnassigned, MatchStatus.Unmatched)]
    [InlineData(NoPreferencePolicy.ManualByAdmin, MatchStatus.PendingManual)]
    [InlineData(NoPreferencePolicy.SupplementaryRound, MatchStatus.Unmatched)]
    [InlineData(NoPreferencePolicy.RandomFill, MatchStatus.Matched)]
    public void AppliesNoPreferencePolicy(NoPreferencePolicy policy, MatchStatus expected)
    {
        var input = Input() with { Preferences = [], Policy = policy };
        var result = MatchingPreviewPlanner.Build(input);
        Assert.Equal(expected, result.Rows[0].Status);
        Assert.Equal(0, result.PriorityCount(1));
        if (policy == NoPreferencePolicy.RandomFill)
        {
            Assert.Equal(3, result.Matched);
            Assert.Equal(1, result.Unmatched);
            Assert.All(result.Rows.Where(r => r.TopicId.HasValue).GroupBy(r => r.TopicId), g => Assert.Single(g));
        }
    }

    [Fact]
    public void PreviewIsRepeatableWithoutMutatingInputOrConsumingRandomState()
    {
        var input = Input() with { Order = MatchingOrder.Random, Policy = NoPreferencePolicy.RandomFill };
        var fingerprint = input.Fingerprint;
        var first = MatchingPreviewPlanner.Build(input);
        var second = MatchingPreviewPlanner.Build(input);
        Assert.Equal(first.Rows.Select(r => (r.Student.Id, r.TopicId)), second.Rows.Select(r => (r.Student.Id, r.TopicId)));
        Assert.Equal(fingerprint, input.Fingerprint);
        Assert.Empty(input.Assignments);
    }

    [Fact]
    public void EmptyCohortHasZeroRatesAndCannotBeConfirmed()
    {
        var result = MatchingPreviewPlanner.Build(Input() with { Students = [], Preferences = [] });
        Assert.Equal(0, result.Total);
        Assert.Equal(0m, result.PriorityRate(1));
        Assert.False(result.CanConfirm);
    }

    [Fact]
    public void UnlimitedCapacityCanHoldAllStudents()
    {
        var input = Input() with { Topics = [new(1, "Không giới hạn", null, TopicApprovalStatuses.Approved)], Preferences = [], Policy = NoPreferencePolicy.RandomFill };
        Assert.Equal(4, MatchingPreviewPlanner.Build(input).Matched);
    }

    [Fact]
    public void InvalidExistingResultsBlockConfirmation()
    {
        var input = Input() with { Assignments = [new("a", 1, MatchStatus.Matched), new("b", 1, MatchStatus.Matched)] };
        var result = MatchingPreviewPlanner.Build(input);
        Assert.NotEmpty(result.Errors);
        Assert.False(result.CanConfirm);
        Assert.False(MatchingPreviewPlanner.Build(Input() with { Assignments = [new("a", 99, MatchStatus.Matched)] }).CanConfirm);
        Assert.False(MatchingPreviewPlanner.Build(Input() with { Assignments = [new("outsider", 1, MatchStatus.Matched)] }).CanConfirm);
    }

    [Fact]
    public void ArchivedOrAlreadySavedPlanCannotBeConfirmed()
    {
        var input = Input();
        var result = MatchingPreviewPlanner.Build(input);
        Assert.False(MatchingPreviewPlanner.Build(input with { CampaignStatus = CampaignStatuses.Archived }).CanConfirm);
        var saved = input with { Assignments = result.Rows.Select(r => new MatchingAssignment(r.Student.Id, r.TopicId, r.Status)).ToList() };
        Assert.Equal(0, MatchingPreviewPlanner.Build(saved).Changes);
        Assert.False(MatchingPreviewPlanner.Build(saved).CanConfirm);
    }

    [Fact]
    public void FingerprintDetectsChangedPreferencesCapacitiesAndConfiguration()
    {
        var input = Input();
        Assert.NotEqual(input.Fingerprint, (input with { Seed = 43 }).Fingerprint);
        Assert.NotEqual(input.Fingerprint, (input with { Policy = NoPreferencePolicy.RandomFill }).Fingerprint);
        Assert.NotEqual(input.Fingerprint, (input with { Preferences = [] }).Fingerprint);
        Assert.NotEqual(input.Fingerprint, (input with { Topics = [] }).Fingerprint);
        Assert.NotEqual(input.Fingerprint, (input with { Assignments = [new("a", 1, MatchStatus.Matched)] }).Fingerprint);
    }
}
