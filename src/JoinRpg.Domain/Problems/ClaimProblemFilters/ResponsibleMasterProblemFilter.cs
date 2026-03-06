namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ResponsibleMasterProblemFilter : IProblemFilter<Claim>
{
    public IEnumerable<ClaimProblem> GetProblems(Claim claim, ProjectInfo projectInfo)
    {
        if (!projectInfo.HasMasterAccess(new(claim.ResponsibleMasterUserId)))
        {
            yield return new ClaimProblem(ClaimProblemType.InvalidResponsibleMaster, ProblemSeverity.Error);
        }
    }
}
