using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternshipManagement.Web.Models;

[Table("student_topic_preference_lists")]
public sealed class TopicPreferenceList
{
    public ulong CampaignId { get; set; }
    public ulong StudentProfileId { get; set; }

    [MaxLength(180)]
    public string? ResearchDirection { get; set; }

    [MaxLength(32)]
    public string Version { get; set; } = "";

    public DateTime SavedAt { get; set; }
    public ICollection<TopicPreference> Choices { get; set; } = [];
}

[Table("student_topic_preferences")]
public sealed class TopicPreference
{
    public ulong CampaignId { get; set; }
    public ulong StudentProfileId { get; set; }
    public uint PreferenceRank { get; set; }
    public ulong TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}
