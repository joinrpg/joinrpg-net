using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain.CharacterProblemFilters;
using JoinRpg.Domain.ClaimProblemFilters;

namespace JoinRpg.Domain
{
  public static class ClaimProblemExtensions
  {
    private static IProblemFilter<Claim>[] Filters { get; }

    public static IEnumerable<ClaimProblem> GetProblems([NotNull] this Claim claim, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      return Filters.SelectMany(f => f.GetProblems(claim)).Where(p => p.Severity >= minimalSeverity);
    }

    static ClaimProblemExtensions()
    {
      Filters = new IProblemFilter<Claim>[]
      {
        new ResponsibleMasterProblemFilter(), new NotAnsweredClaim(), new BrokenClaimsAndCharacters(),
        new FinanceProblemsFilter(), new ClaimWorkStopped(), new FieldNotSetFilterClaim(),
      };
    }

    public static bool HasProblemsForFields([NotNull] this Claim claim, [NotNull, ItemNotNull] IEnumerable<ProjectField> fields)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      if (fields == null) throw new ArgumentNullException(nameof(fields));
      return claim.GetProblems().OfType<FieldRelatedProblem>().Any(fp => fields.Select(f => f.ProjectFieldId).Contains(fp.Field.ProjectFieldId));
    }
  }

  public static class CharacterProblemExtensions
  {
    private static IProblemFilter<Character>[] Filters { get; }

    public static IEnumerable<ClaimProblem> GetProblems([NotNull] this Character claim, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      return Filters.SelectMany(f => f.GetProblems(claim)).Where(p => p.Severity >= minimalSeverity);
    }

    static CharacterProblemExtensions()
    {
      Filters = new IProblemFilter<Character>[]
      {
        new FieldNotSetFilterCharacter(),
      };
    }

    public static bool HasProblemsForField([NotNull] this Character character, [NotNull] ProjectField field)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      if (field == null) throw new ArgumentNullException(nameof(field));
      return character.GetProblems().OfType<FieldRelatedProblem>().Any(fp => fp.Field == field);
    }
  }


  public class ClaimProblem
  {
    public ClaimProblemType ProblemType { get; }

    public DateTime? ProblemTime { get; }
    [CanBeNull]
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

  public class FieldRelatedProblem : ClaimProblem
  {
    [NotNull]
    public ProjectField Field { get; }

    public FieldRelatedProblem(ClaimProblemType problemType, ProblemSeverity severity, [NotNull] ProjectField field)
      : base(problemType, severity, null, field.FieldName)
    {
      if (field == null) throw new ArgumentNullException(nameof(field));
      Field = field;
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
    [Obsolete, UsedImplicitly]
    DeletedFieldHasValue,
    FieldIsEmpty,
    FieldShouldNotHaveValue,
    NoParentGroup,
    GroupIsBroken,
  }

  public enum ProblemSeverity
  {
    Hint,
    Warning,
    Error,
    Fatal,
  }
}
