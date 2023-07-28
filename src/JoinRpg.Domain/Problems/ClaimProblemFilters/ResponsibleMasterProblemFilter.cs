using JoinRpg.DataModel;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ResponsibleMasterProblemFilter : IProblemFilter<Claim>
{
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
        if (!claim.HasMasterAccess(claim.ResponsibleMasterUserId))
        {
            yield return new ClaimProblem(ClaimProblemType.InvalidResponsibleMaster, ProblemSeverity.Error);
        }
    }
}
