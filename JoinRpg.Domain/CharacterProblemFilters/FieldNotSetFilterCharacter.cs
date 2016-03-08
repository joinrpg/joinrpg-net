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
        //TODO change depending of optional/required status of field
        if (fieldWithValue.Field.IsActive && !fieldWithValue.HasValue())
        {
          yield return
            new ClaimProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Hint, null, fieldWithValue.Field.FieldName);
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
      projectFields.FillFrom(character);

      return CheckFields(projectFields.Where(pf => pf.Field.FieldBoundTo == FieldBoundTo.Character).ToList());
    }

    #endregion
  }

  internal class FieldNotSetFilterClaim : FieldNotSetFilterBase, IProblemFilter<Claim>
  {
    #region Implementation of IProblemFilter<in Character>

    public IEnumerable<ClaimProblem> GetProblems(Claim character)
    {
      var projectFields = character.Project.GetFields().ToList();
      projectFields.FillFrom(character);

      return CheckFields(projectFields.Where(pf => pf.Field.FieldBoundTo == FieldBoundTo.Claim).ToList());
    }

    #endregion
  }
}
