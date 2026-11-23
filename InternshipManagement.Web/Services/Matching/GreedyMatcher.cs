namespace InternshipManagement.Web.Services.Matching;

public sealed record GreedyMatchResult(
    List<(string StudentId, ulong TopicId)> Matches,
    List<string> UnmatchedStudentIds);

public static class GreedyMatcher
{
    public static GreedyMatchResult Match(
        IReadOnlyList<string> studentIds,
        IReadOnlyDictionary<string, List<ulong>> preferencesByStudent,
        IReadOnlyDictionary<ulong, int> topicCapacities)
    {
        var remainingCapacity = topicCapacities.ToDictionary(
            topic => topic.Key,
            topic => Math.Max(0, topic.Value));
        var matches = new List<(string StudentId, ulong TopicId)>();
        var unmatchedStudentIds = new List<string>();
        var seenStudents = new HashSet<string>();

        foreach (var studentId in studentIds)
        {
            if (!seenStudents.Add(studentId))
                continue;

            var assigned = false;
            var seenTopics = new HashSet<ulong>();
            if (preferencesByStudent.TryGetValue(studentId, out var preferences))
            {
                foreach (var topicId in preferences)
                {
                    if (!seenTopics.Add(topicId) ||
                        !remainingCapacity.TryGetValue(topicId, out var capacity) ||
                        capacity <= 0)
                    {
                        continue;
                    }

                    matches.Add((studentId, topicId));
                    remainingCapacity[topicId] = capacity - 1;
                    assigned = true;
                    break;
                }
            }

            if (!assigned)
                unmatchedStudentIds.Add(studentId);
        }

        return new GreedyMatchResult(matches, unmatchedStudentIds);
    }
}
