using System.Security.Cryptography;
using System.Text.Json;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Services.Matching;

public record MatchingStudent(string Id, string Name, string Code);
public record MatchingTopic(ulong Id, string Title, int? Capacity, string ApprovalStatus);
public record MatchingPreference(string StudentId, ulong TopicId, int Priority, DateTime CreatedAt);
public record MatchingAssignment(string StudentId, ulong? TopicId, MatchStatus Status);
public record MatchingInput(ulong CampaignId, string CampaignName, string CampaignStatus,
    MatchingOrder Order, NoPreferencePolicy Policy, int Seed,
    List<MatchingStudent> Students, List<MatchingTopic> Topics,
    List<MatchingPreference> Preferences, List<MatchingAssignment> Assignments)
{
    // Lists are loaded in a stable order. Include all inputs, not just the proposed result.
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this,
        new JsonSerializerOptions { IgnoreReadOnlyProperties = true })));
}

public record MatchingPreviewRow(MatchingStudent Student, List<MatchingPreference> Preferences,
    ulong? TopicId, string? TopicTitle, MatchStatus Status, int? MatchedPriority, bool Existing);

public record MatchingPreview(MatchingInput Input, List<MatchingPreviewRow> Rows, List<string> Errors)
{
    public int Total => Rows.Count;
    public int Matched => Rows.Count(r => r.Status == MatchStatus.Matched);
    public int PendingManual => Rows.Count(r => r.Status == MatchStatus.PendingManual);
    public int Unmatched => Rows.Count(r => r.Status == MatchStatus.Unmatched);
    public int Changes => Rows.Count(r => !Input.Assignments.Any(a =>
        a.StudentId == r.Student.Id && a.TopicId == r.TopicId && a.Status == r.Status));
    public int PriorityCount(int priority) => Rows.Count(r => r.Status == MatchStatus.Matched && r.MatchedPriority == priority);
    public decimal PriorityRate(int priority) => Total == 0 ? 0 : 100m * PriorityCount(priority) / Total;
    public bool CanConfirm => Errors.Count == 0 && Changes > 0 && Input.CampaignStatus != CampaignStatuses.Archived;
}

/// <summary>Pure planning shared by preview and execution; never writes assignments.</summary>
public static class MatchingPreviewPlanner
{
    public static MatchingPreview Build(MatchingInput input)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(input.Order) || !Enum.IsDefined(input.Policy))
            errors.Add("Cấu hình thứ tự hoặc chính sách ghép không hợp lệ.");
        if (input.Assignments.GroupBy(a => a.StudentId).Any(g => g.Count() > 1))
            errors.Add("Có sinh viên có nhiều kết quả trong cùng đợt. Cần kiểm tra trước khi xác nhận.");

        var topics = input.Topics.ToDictionary(t => t.Id);
        var assignments = input.Assignments.GroupBy(a => a.StudentId).ToDictionary(g => g.Key, g => g.First());
        var approved = input.Topics.Where(t => t.ApprovalStatus == TopicApprovalStatuses.Approved).ToList();
        var existingCounts = input.Assignments.Where(a => a.Status == MatchStatus.Matched && a.TopicId.HasValue)
            .GroupBy(a => a.TopicId!.Value).ToDictionary(g => g.Key, g => g.Count());
        foreach (var topic in input.Topics)
            if (topic.Capacity.HasValue && existingCounts.GetValueOrDefault(topic.Id) > topic.Capacity.Value)
                errors.Add($"Đề tài «{topic.Title}» đã vượt chỉ tiêu.");
        if (input.Assignments.Any(a => a.Status == MatchStatus.Matched &&
            (!a.TopicId.HasValue || !topics.TryGetValue(a.TopicId.Value, out var t) || t.ApprovalStatus != TopicApprovalStatuses.Approved)))
            errors.Add("Có kết quả đã ghép vào đề tài không còn hợp lệ.");
        var studentIds = input.Students.Select(s => s.Id).ToHashSet();
        if (input.Assignments.Any(a => !studentIds.Contains(a.StudentId)))
            errors.Add("Có kết quả của sinh viên ngoài danh sách xét ghép của ngành. Cần kiểm tra lại.");

        var preferences = input.Preferences.GroupBy(p => p.StudentId)
            .ToDictionary(g => g.Key, g => g.OrderBy(p => p.Priority).ThenBy(p => p.TopicId).ToList());
        var valid = input.Students.ToDictionary(s => s.Id, s => preferences.GetValueOrDefault(s.Id, [])
            .Where(p => topics.TryGetValue(p.TopicId, out var t) && t.ApprovalStatus == TopicApprovalStatuses.Approved)
            .Select(p => p.TopicId).Distinct().ToList());
        var random = new Random(input.Seed);
        var ordered = input.Students.OrderBy(s => s.Id, StringComparer.Ordinal).ToList();
        if (input.Order == MatchingOrder.Random)
        {
            for (var i = ordered.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (ordered[i], ordered[j]) = (ordered[j], ordered[i]);
            }
        }
        else
        {
            ordered = ordered.OrderBy(s => !preferences.ContainsKey(s.Id))
                .ThenBy(s => preferences.GetValueOrDefault(s.Id, []).Select(p => (DateTime?)p.CreatedAt).Min())
                .ThenBy(s => s.Id, StringComparer.Ordinal).ToList();
        }

        var pending = ordered.Where(s => !assignments.TryGetValue(s.Id, out var a) || a.Status != MatchStatus.Matched).ToList();
        var capacities = approved.ToDictionary(t => t.Id,
            t => t.Capacity.HasValue ? Math.Max(0, t.Capacity.Value - existingCounts.GetValueOrDefault(t.Id)) : pending.Count);
        var matching = GreedyMatcher.Match(pending.Where(s => valid[s.Id].Count > 0).Select(s => s.Id).ToList(), valid, capacities);
        var proposed = matching.Matches.ToDictionary(m => m.StudentId, m => m.TopicId);
        foreach (var topicId in proposed.Values) capacities[topicId]--;
        foreach (var student in pending.Where(s => valid[s.Id].Count == 0))
        {
            if (input.Policy != NoPreferencePolicy.RandomFill) continue;
            var available = approved.OrderBy(t => t.Id).ToList();
            while (available.Count > 0)
            {
                var index = random.Next(available.Count);
                var topic = available[index];
                available.RemoveAt(index);
                if (capacities[topic.Id] <= 0) continue;
                proposed[student.Id] = topic.Id;
                capacities[topic.Id]--;
                break;
            }
        }

        var rows = input.Students.Select(student =>
        {
            var existing = assignments.TryGetValue(student.Id, out var assignment) && assignment.Status == MatchStatus.Matched;
            ulong? topicId = existing ? assignment!.TopicId : proposed.TryGetValue(student.Id, out var proposedId) ? proposedId : null;
            var status = topicId.HasValue ? MatchStatus.Matched :
                valid[student.Id].Count == 0 && input.Policy == NoPreferencePolicy.ManualByAdmin ? MatchStatus.PendingManual : MatchStatus.Unmatched;
            var prefs = preferences.GetValueOrDefault(student.Id, []);
            int? rank = topicId.HasValue ? prefs.FirstOrDefault(p => p.TopicId == topicId)?.Priority : null;
            return new MatchingPreviewRow(student, prefs, topicId,
                topicId.HasValue ? topics.GetValueOrDefault(topicId.Value)?.Title : null, status, rank, existing);
        }).OrderBy(r => r.Student.Name, StringComparer.Ordinal).ThenBy(r => r.Student.Id, StringComparer.Ordinal).ToList();
        return new MatchingPreview(input, rows, errors);
    }
}
