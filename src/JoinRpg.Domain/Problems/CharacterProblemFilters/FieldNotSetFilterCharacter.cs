using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems.CharacterProblemFilters;

internal class FieldNotSetFilterCharacter : FieldNotSetFilterBase, IProblemFilter<Character>
{
    #region Implementation of IProblemFilter<in Character>

    public IEnumerable<ClaimProblem> GetProblems(Character character, ProjectInfo projectInfo)
    {
        return
          CheckFields(
            character.GetFields(projectInfo)
              .Where(pf => pf.Field.FieldBoundTo == FieldBoundTo.Character || character.ApprovedClaim != null)
              .ToList(), character);
    }

    #endregion
}
