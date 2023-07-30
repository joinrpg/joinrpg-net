using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class BrokenClaimsAndCharacters : IProblemFilter<Claim>
{
    public IEnumerable<ClaimProblem> GetProblems(Claim claim, ProjectInfo projectInfo)
    {
        if (claim.IsInDiscussion && claim.Character?.ApprovedClaim != null)
        {
            yield return new ClaimProblem(ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim, ProblemSeverity.Error);
        }
        if (claim.IsApproved && (claim.Character == null || !claim.Character.IsActive))
        {
            yield return new ClaimProblem(ClaimProblemType.NoCharacterOnApprovedClaim, ProblemSeverity.Fatal);
        }
        if (claim.Character == null && claim.Group == null)
        {
            yield return new ClaimProblem(ClaimProblemType.ClaimDontHaveTarget, ProblemSeverity.Fatal);
        }
    }
}
