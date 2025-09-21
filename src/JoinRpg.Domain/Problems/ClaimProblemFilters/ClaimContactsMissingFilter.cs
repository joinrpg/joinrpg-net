using JoinRpg.Helpers;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;
internal class ClaimContactsMissingFilter : IProblemFilter<Claim>
{
    public IEnumerable<ClaimProblem> GetProblems(Claim claim, ProjectInfo projectInfo)
    {
        return new[]{
            CheckContact(claim.Player.Extra?.Telegram, projectInfo.ProfileRequirementSettings.RequireTelegram, ClaimProblemType.MissingTelegram),
            CheckContact(claim.Player.Extra?.Vk, projectInfo.ProfileRequirementSettings.RequireVkontakte, ClaimProblemType.MissingVkontakte),
            CheckContact(claim.Player.Extra?.PhoneNumber, projectInfo.ProfileRequirementSettings.RequirePhone, ClaimProblemType.MissingPhone),
            CheckContact(claim.Player.FullName, projectInfo.ProfileRequirementSettings.RequireRealName, ClaimProblemType.MissingRealname),
        }.WhereNotNull();
    }

    private static ClaimProblem? CheckContact(string? contact, MandatoryStatus requirement, ClaimProblemType problemType)
    {
        if (string.IsNullOrWhiteSpace(contact))
        {
            switch (requirement)
            {
                case MandatoryStatus.Optional:
                    return null;
                case MandatoryStatus.Recommended:
                    return new ClaimProblem(problemType, ProblemSeverity.Hint);
                case MandatoryStatus.Required:
                    return new ClaimProblem(problemType, ProblemSeverity.Warning);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return null;
    }
}
