using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterProblemFilters;
using JoinRpg.Domain.ClaimProblemFilters;

namespace JoinRpg.Domain
{
  public static class ClaimProblemExtensions
  {
    private static IProblemFilter<Claim>[] Filters { get; }

    public static IEnumerable<ClaimProblem> GetProblems(this Claim claim, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
      return Filters.SelectMany(f => f.GetProblems(claim)).Where(p => p.Severity >= minimalSeverity);
    }

    static ClaimProblemExtensions()
    {
      Filters = new IProblemFilter<Claim>[]
      {
        new ResponsibleMasterProblemFilter(), new NotAnsweredClaim(), new BrokenClaimsAndCharacters(),
        new FinanceProblemsFilter(), new ClaimWorkStopped(), new FieldNotSetFilterClaim()
      };
    }
  }

  public static class CharacterProblemExtensions
  {
    private static IProblemFilter<Character>[] Filters { get; }

    public static IEnumerable<ClaimProblem> GetProblems(this Character claim, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
      return Filters.SelectMany(f => f.GetProblems(claim)).Where(p => p.Severity >= minimalSeverity);
    }

    static CharacterProblemExtensions()
    {
      Filters = new IProblemFilter<Character>[]
      {
        new FieldNotSetFilterCharacter()
      };
    }
  }


  public class ClaimProblem
  {
    public ClaimProblemType ProblemType { get; }

    public DateTime? ProblemTime { get; }
    public string ExtraInfo { get;  }

    public ProblemSeverity Severity { get;  }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, DateTime? problemTime = null, string extraInfo = null)
    {
      ProblemType = problemType;
      Severity = severity;
      ProblemTime = problemTime;
      ExtraInfo = extraInfo;
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
    ClaimDontHaveTarget,
    DeletedFieldHasValue,
    FieldIsEmpty
  }

  public enum ProblemSeverity
  {
    Hint,
    Warning,
    Error,
    Fatal
  }
}
