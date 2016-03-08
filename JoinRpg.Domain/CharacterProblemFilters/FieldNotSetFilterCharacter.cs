using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain.ClaimProblemFilters;

namespace JoinRpg.Domain.CharacterProblemFilters
{
  internal class FieldNotSetFilterBase
  {
    protected static IEnumerable<ClaimProblem> CheckFields(IReadOnlyCollection<FieldWithValue> fieldsToCheck)
    {
      foreach (var fieldWithValue in fieldsToCheck)
      {
        if (!fieldWithValue.Field.IsActive && fieldWithValue.HasValue())
        {
          yield return
            new ClaimProblem(ClaimProblemType.DeletedFieldHasValue, ProblemSeverity.Hint, null,
              fieldWithValue.Field.FieldName);
        }
      }

      foreach (var fieldWithValue in fieldsToCheck)
      {
        if (fieldWithValue.Field.IsActive && !fieldWithValue.HasValue())
        {
          switch (fieldWithValue.Field.MandatoryStatus)
          {
            case MandatoryStatus.Optional:
              break;
            case MandatoryStatus.Recommended:
              yield return new ClaimProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Hint, null, fieldWithValue.Field.FieldName);
              break;
            case MandatoryStatus.Required:
              yield return new ClaimProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Warning, null, fieldWithValue.Field.FieldName);
              break;
            default:
              throw new ArgumentOutOfRangeException();
          }
        }
      }
    }
  }

  internal class FieldNotSetFilterCharacter : FieldNotSetFilterBase, IProblemFilter<Character>
  {
    #region Implementation of IProblemFilter<in Character>
    public IEnumerable<ClaimProblem> GetProblems(Character character)
    {
      var projectFields = character.Project.GetFields().ToList();
      projectFields.FillFrom(character.ApprovedClaim);
      projectFields.FillFrom(character);

      return CheckFields(projectFields.Where(pf => pf.Field.FieldBoundTo == FieldBoundTo.Character || character.ApprovedClaim != null).ToList());
    }
    #endregion
  }

  internal class FieldNotSetFilterClaim : FieldNotSetFilterBase, IProblemFilter<Claim>
  {
    #region Implementation of IProblemFilter<in Character>
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      var projectFields = claim.Project.GetFields().ToList();
      projectFields.FillFrom(claim);
      projectFields.FillFrom(claim.Character);

      return CheckFields(projectFields.Where(pf => pf.Field.FieldBoundTo == FieldBoundTo.Claim || claim.IsApproved).ToList());
    }
    #endregion
  }
}
