namespace InternshipManagement.Web.Models;

public enum NoPreferencePolicy
{
    LeaveUnassigned = 0,
    ManualByAdmin = 1,
    RandomFill = 2,
    SupplementaryRound = 3
}

public enum MatchStatus
{
    Matched = 0,
    Unmatched = 1,
    PendingManual = 2
}
