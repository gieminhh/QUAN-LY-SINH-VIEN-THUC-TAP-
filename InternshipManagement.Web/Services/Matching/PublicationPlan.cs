using System.Security.Cryptography;
using System.Text.Json;
using InternshipManagement.Web.Models;

namespace InternshipManagement.Web.Services.Matching;

public record PublicationStudent(string Id, string Name, string Code);
public record PublicationTopic(ulong Id, string Title, int? Capacity, string ApprovalStatus);
public record PublicationAssignment(string StudentId, ulong? TopicId, MatchStatus Status);
public record PublicationPreference(string StudentId, ulong TopicId, int Priority, DateTime SubmittedAt);
public record PublicationPrevious(string StudentId, ulong? TopicId, MatchStatus Status, DateTime? ConfirmedAt);
public record PublicationRow(string StudentId, string Name, string Code, ulong? TopicId, string? TopicTitle,
    MatchStatus Status, DateTime? ConfirmedAt);
public record PublicationInput(ulong CampaignId, string CampaignStatus, ulong? RoundId, int Number,
    DateTime? ClosesAt, MatchingOrder Order, int Seed,
    List<PublicationStudent> Students, List<PublicationTopic> Topics,
    List<PublicationAssignment> Assignments, List<PublicationPreference> Preferences, List<PublicationPrevious> Previous)
{
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(this,
        new JsonSerializerOptions { IgnoreReadOnlyProperties = true })));
}

public record PublicationPlan(PublicationInput Input, List<PublicationRow> Rows, List<string> Errors)
{
    public bool CanPublish(DateTime utcNow) => Errors.Count == 0 && Rows.Count > 0 &&
        Input.CampaignStatus == CampaignStatuses.Closed && (!Input.ClosesAt.HasValue || utcNow >= Input.ClosesAt);
}

public static class PublicationPlanner
{
    public static PublicationPlan Build(PublicationInput input)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(input.Order)) errors.Add("Thứ tự xét ghép không hợp lệ.");
        if (input.Assignments.GroupBy(a => a.StudentId).Any(g => g.Count() > 1))
            errors.Add("Có nhiều kết quả cho cùng sinh viên. Cần xử lý trước khi công bố.");
        var topics = input.Topics.ToDictionary(t => t.Id);
        var students = input.Students.ToDictionary(s => s.Id);
        var assignments = input.Assignments.GroupBy(a => a.StudentId).ToDictionary(g => g.Key, g => g.First());
        var matched = input.Assignments.Where(a => a.Status == MatchStatus.Matched).ToList();
        if (matched.Any(a => !students.ContainsKey(a.StudentId) || !a.TopicId.HasValue ||
            !topics.TryGetValue(a.TopicId.Value, out var t) || t.ApprovalStatus != TopicApprovalStatuses.Approved))
            errors.Add("Có cặp ghép sai sinh viên/ngành hoặc đề tài chưa được duyệt.");
        if (input.Assignments.Any(a => !Enum.IsDefined(a.Status) || (a.Status != MatchStatus.Matched && a.TopicId.HasValue)))
            errors.Add("Có kết quả không nhất quán giữa trạng thái và đề tài.");
        foreach (var group in matched.Where(a => a.TopicId.HasValue).GroupBy(a => a.TopicId!.Value))
            if (topics.TryGetValue(group.Key, out var t) && t.Capacity.HasValue && group.Count() > t.Capacity.Value)
                errors.Add($"Đề tài «{t.Title}» vượt chỉ tiêu.");
        foreach (var old in input.Previous.Where(p => p.Status == MatchStatus.Matched))
            if (!assignments.TryGetValue(old.StudentId, out var current) || current.Status != MatchStatus.Matched || current.TopicId != old.TopicId)
                errors.Add("Cặp đã công bố bị thay đổi. Cần khôi phục trước khi mở/công bố vòng bổ sung.");

        var pairs = matched.GroupBy(a => a.StudentId).ToDictionary(g => g.Key, g => g.First().TopicId);
        if (input.RoundId.HasValue)
        {
            var counts = matched.Where(a => a.TopicId.HasValue).GroupBy(a => a.TopicId!.Value).ToDictionary(g => g.Key, g => g.Count());
            var capacities = input.Topics.Where(t => t.ApprovalStatus == TopicApprovalStatuses.Approved).ToDictionary(t => t.Id,
                t => t.Capacity.HasValue ? Math.Max(0, t.Capacity.Value - counts.GetValueOrDefault(t.Id)) : input.Students.Count);
            var prefs = input.Preferences.GroupBy(p => p.StudentId).ToDictionary(g => g.Key,
                g => g.OrderBy(p => p.Priority).ThenBy(p => p.TopicId).Select(p => p.TopicId).ToList());
            var ordered = input.Students.Where(s => !pairs.ContainsKey(s.Id) && prefs.ContainsKey(s.Id))
                .OrderBy(s => input.Preferences.Where(p => p.StudentId == s.Id).Min(p => p.SubmittedAt))
                .ThenBy(s => s.Id, StringComparer.Ordinal).Select(s => s.Id).ToList();
            if (input.Order == MatchingOrder.Random)
            {
                ordered.Sort(StringComparer.Ordinal);
                var random = new Random(input.Seed);
                for (var i = ordered.Count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (ordered[i], ordered[j]) = (ordered[j], ordered[i]);
                }
            }
            foreach (var pair in GreedyMatcher.Match(ordered, prefs, capacities).Matches)
                pairs[pair.StudentId] = pair.TopicId;
        }
        var previous = input.Previous.ToDictionary(p => p.StudentId);
        var rows = input.Students.Select(s =>
        {
            var topicId = pairs.GetValueOrDefault(s.Id);
            var status = topicId.HasValue ? MatchStatus.Matched : MatchStatus.Unmatched;
            return new PublicationRow(s.Id, s.Name, s.Code, topicId,
                topicId.HasValue ? topics.GetValueOrDefault(topicId.Value)?.Title : null, status,
                previous.TryGetValue(s.Id, out var old) && old.TopicId == topicId ? old.ConfirmedAt : null);
        }).OrderBy(r => r.Name, StringComparer.Ordinal).ThenBy(r => r.StudentId, StringComparer.Ordinal).ToList();
        return new PublicationPlan(input, rows, errors.Distinct().ToList());
    }
}
