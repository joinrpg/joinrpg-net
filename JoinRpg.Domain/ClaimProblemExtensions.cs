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

    public static IEnumerable<ClaimProblem> GetProblems(this Claim claim)
    {
      return Filters.SelectMany(f => f.GetProblems(claim));
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

    public ClaimProblem(ClaimProblemType problemType, DateTime? problemTime = null)
    {
      ProblemType = problemType;
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
    ClaimWorkStopped
  }
}
