using InternshipManagement.Web.Services.Matching;

namespace InternshipManagement.Tests;

public class GreedyMatcherTests
{
    [Fact]
    public void EmptyInputReturnsNoMatchesOrUnmatchedStudents()
    {
        var result = Match(
            [],
            new Dictionary<string, List<ulong>>(),
            new Dictionary<ulong, int>());

        Assert.Empty(result.Matches);
        Assert.Empty(result.UnmatchedStudentIds);
        AssertInvariants(result, new Dictionary<ulong, int>());
    }

    [Fact]
    public void StudentWithoutPreferencesIsUnmatched()
    {
        var result = Match(
            ["student-1"],
            new Dictionary<string, List<ulong>>(),
            new Dictionary<ulong, int> { [1] = 1 });

        Assert.Empty(result.Matches);
        Assert.Equal(["student-1"], result.UnmatchedStudentIds);
        AssertInvariants(result, new Dictionary<ulong, int> { [1] = 1 });
    }

    [Fact]
    public void TopicWithZeroCapacityIsNotAssigned()
    {
        var result = Match(
            ["student-1"],
            new Dictionary<string, List<ulong>> { ["student-1"] = [1] },
            new Dictionary<ulong, int> { [1] = 0 });

        Assert.Empty(result.Matches);
        Assert.Equal(["student-1"], result.UnmatchedStudentIds);
        AssertInvariants(result, new Dictionary<ulong, int> { [1] = 0 });
    }

    [Fact]
    public void FirstStudentGetsTheOnlyAvailablePlace()
    {
        var result = Match(
            ["student-1", "student-2"],
            new Dictionary<string, List<ulong>>
            {
                ["student-1"] = [1],
                ["student-2"] = [1]
            },
            new Dictionary<ulong, int> { [1] = 1 });

        Assert.Equal([("student-1", 1UL)], result.Matches);
        Assert.Equal(["student-2"], result.UnmatchedStudentIds);
        AssertInvariants(result, new Dictionary<ulong, int> { [1] = 1 });
    }

    [Fact]
    public void AllPreferencesAtFullTopicsLeaveStudentsUnmatched()
    {
        var result = Match(
            ["student-1", "student-2"],
            new Dictionary<string, List<ulong>>
            {
                ["student-1"] = [1],
                ["student-2"] = [1]
            },
            new Dictionary<ulong, int> { [1] = 0 });

        Assert.Empty(result.Matches);
        Assert.Equal(["student-1", "student-2"], result.UnmatchedStudentIds);
        AssertInvariants(result, new Dictionary<ulong, int> { [1] = 0 });
    }

    [Fact]
    public void DuplicatePreferenceIsTriedOnlyOnce()
    {
        var result = Match(
            ["student-1"],
            new Dictionary<string, List<ulong>> { ["student-1"] = [1, 1, 2] },
            new Dictionary<ulong, int> { [1] = 0, [2] = 1 });

        Assert.Equal([("student-1", 2UL)], result.Matches);
        Assert.Empty(result.UnmatchedStudentIds);
        AssertInvariants(result, new Dictionary<ulong, int> { [1] = 0, [2] = 1 });
    }

    [Fact]
    public void MissingTopicCapacityEntryIsSkipped()
    {
        var result = Match(
            ["student-1"],
            new Dictionary<string, List<ulong>> { ["student-1"] = [99, 1] },
            new Dictionary<ulong, int> { [1] = 1 });

        Assert.Equal([("student-1", 1UL)], result.Matches);
        Assert.Empty(result.UnmatchedStudentIds);
        Assert.DoesNotContain(result.Matches, match => match.TopicId == 99);
        AssertInvariants(result, new Dictionary<ulong, int> { [1] = 1 });
    }

    [Fact]
    public void MoreStudentsThanTotalCapacityLeavesTheExcessUnmatched()
    {
        var result = Match(
            ["student-1", "student-2", "student-3"],
            new Dictionary<string, List<ulong>>
            {
                ["student-1"] = [1, 2],
                ["student-2"] = [1, 2],
                ["student-3"] = [1, 2]
            },
            new Dictionary<ulong, int> { [1] = 1, [2] = 1 });

        Assert.Equal(2, result.Matches.Count);
        Assert.Single(result.UnmatchedStudentIds);
        AssertInvariants(result, new Dictionary<ulong, int> { [1] = 1, [2] = 1 });
    }

    [Fact]
    public void RepeatedRunsWithSameInputReturnSameResult()
    {
        var students = new[] { "student-1", "student-2", "student-3" };
        var preferences = new Dictionary<string, List<ulong>>
        {
            ["student-1"] = [1, 2],
            ["student-2"] = [1, 2],
            ["student-3"] = [2, 1]
        };
        var capacities = new Dictionary<ulong, int> { [1] = 1, [2] = 1 };

        var first = Match(students, preferences, capacities);
        var second = Match(students, preferences, capacities);

        Assert.Equal(first.Matches, second.Matches);
        Assert.Equal(first.UnmatchedStudentIds, second.UnmatchedStudentIds);
        AssertInvariants(first, capacities);
        AssertInvariants(second, capacities);
    }

    private static GreedyMatchResult Match(
        IReadOnlyList<string> studentIds,
        IReadOnlyDictionary<string, List<ulong>> preferences,
        IReadOnlyDictionary<ulong, int> capacities)
    {
        return GreedyMatcher.Match(studentIds, preferences, capacities);
    }

    private static void AssertInvariants(
        GreedyMatchResult result,
        IReadOnlyDictionary<ulong, int> capacities)
    {
        Assert.Equal(
            result.Matches.Count,
            result.Matches.Select(match => match.StudentId).Distinct().Count());

        foreach (var topicMatches in result.Matches.GroupBy(match => match.TopicId))
        {
            Assert.True(capacities.ContainsKey(topicMatches.Key));
            Assert.True(topicMatches.Count() <= capacities[topicMatches.Key]);
        }
    }
}
