using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain.ClaimProblemFilters;

namespace JoinRpg.Domain
{
  public static class ClaimProblemExtensions
  {
    private static IClaimProblemFilter[] Filters { get; }

    public static IEnumerable<ClaimProblem> GetProblems(this Claim claim, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
      return Filters.SelectMany(f => f.GetProblems(claim)).Where(p => p.Severity >= minimalSeverity);
    }

    static ClaimProblemExtensions()
    {
      Filters = new IClaimProblemFilter[]
      {
        new ResponsibleMasterProblemFilter(), new NotAnsweredClaim(), new BrokenClaimsAndCharacters(),
        new FinanceProblemsFilter(), new ClaimWorkStopped(),
      };
    }
  }


  public class ClaimProblem
  {
    public ClaimProblemType ProblemType { get; }

    public DateTime? ProblemTime { get; }

    public ProblemSeverity Severity { get;  }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, DateTime? problemTime = null)
    {
      ProblemType = problemType;
      Severity = severity;
      ProblemTime = problemTime;
    }
  }

  public enum ClaimProblemType
  {
    NoResponsibleMaster,
    InvalidResponsibleMaster,
    ClaimNeverAnswered,
    ClaimNoDecision,
    ClaimActiveButCharacterHasApprovedClaim,
    FinanceModerationRequired,
    TooManyMoney,
    ClaimDiscussionStopped,
    NoCharacterOnApprovedClaim,
    FeePaidPartially,
    UnApprovedClaimPayment,
    ClaimWorkStopped,
    ClaimDontHaveTarget
  }

  public enum ProblemSeverity
  {
    Hint,
    Warning,
    Error,
    Fatal
  }
}
