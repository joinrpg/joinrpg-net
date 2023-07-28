using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class FieldNotSetFilterClaim : FieldNotSetFilterBase, IProblemFilter<Claim>
{
    #region Implementation of IProblemFilter<in Claim>
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
        var projectFields = claim.GetFields();

        return CheckFields(projectFields.Where(pf => pf.Field.FieldBoundTo == FieldBoundTo.Claim || claim.IsApproved).ToList(), claim.GetTarget());
    }
    #endregion
}
