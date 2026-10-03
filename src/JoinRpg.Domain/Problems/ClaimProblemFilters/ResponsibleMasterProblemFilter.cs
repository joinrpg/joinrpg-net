using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ResponsibleMasterProblemFilter : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimProblemContext context)
    {
        if (!context.ProjectInfo.HasMasterAccess(context.Claim.ResponsibleMasterId))
        {
            yield return new ClaimProblem(ClaimProblemType.InvalidResponsibleMaster, ProblemSeverity.Error);
        }
    }
}
