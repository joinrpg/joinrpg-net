using System;
using JetBrains.Annotations;

namespace JoinRpg.Domain
{
    public class ClaimProblem
    {
        public ClaimProblemType ProblemType { get; }

        public DateTime? ProblemTime { get; }
        [CanBeNull]
        public string? ExtraInfo { get; }

        public ProblemSeverity Severity { get; }

        public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, DateTime? problemTime = null, string? extraInfo = null)
        {
            ProblemType = problemType;
            Severity = severity;
            ProblemTime = problemTime;
            ExtraInfo = extraInfo;
        }
    }
}
