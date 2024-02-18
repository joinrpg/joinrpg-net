using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class FieldNotSetFilterClaim : FieldNotSetFilterBase, IProblemFilter<Claim>
{
    #region Implementation of IProblemFilter<in Claim>
    public IEnumerable<ClaimProblem> GetProblems(Claim claim, ProjectInfo projectInfo)
    {
        var projectFields = claim.GetFields(projectInfo);

        return CheckFields(projectFields.Where(pf => pf.Field.BoundTo == FieldBoundTo.Claim || claim.IsApproved).ToList(), claim.GetTarget());
    }
    #endregion
}
