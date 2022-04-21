using JetBrains.Annotations;

namespace JoinRpg.Domain;

public class ClaimProblem
{
    public ClaimProblemType ProblemType { get; }

    public DateTime? ProblemTime { get; }
    [CanBeNull]
    public string? ExtraInfo { get; }

    public ProblemSeverity Severity { get; }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, DateTime problemTime, string? extraInfo = null)
    {
        ProblemType = problemType;
        Severity = severity;
        ProblemTime = problemTime;
        ExtraInfo = extraInfo;
    }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, string extraInfo)
    {
        ProblemType = problemType;
        Severity = severity;
        ProblemTime = null;
        ExtraInfo = extraInfo;
    }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity)
    {
        ProblemType = problemType;
        Severity = severity;
        ProblemTime = null;
        ExtraInfo = null;
    }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, DateTimeOffset problemTime, string? extraInfo = null)
    {
        ProblemType = problemType;
        Severity = severity;
        ProblemTime = problemTime.DateTime;
        ExtraInfo = extraInfo;
    }
}
