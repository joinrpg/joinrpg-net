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
            CheckContact(claim.Player.Extra?.PassportData, projectInfo.ProfileRequirementSettings.RequirePassport, ClaimProblemType.MissingPassport),
            CheckContact(claim.Player.Extra?.RegistrationAddress, projectInfo.ProfileRequirementSettings.RequireRegistrationAddress, ClaimProblemType.MissingRegistrationAddress),
            CheckContact(claim.Player.FullName, projectInfo.ProfileRequirementSettings.RequireRealName, ClaimProblemType.MissingRealname),
            CheckSensitiveDataAccess(claim, projectInfo),
        }.WhereNotNull();
    }

    private static ProfileRelatedProblem? CheckSensitiveDataAccess(Claim claim, ProjectInfo projectInfo)
    {
        return !claim.PlayerAllowedSenstiveData && projectInfo.ProfileRequirementSettings.SensitiveDataRequired
                    ? new ProfileRelatedProblem(ClaimProblemType.SensitiveDataNotAllowed, ProblemSeverity.Warning) : null;
    }

    private static ProfileRelatedProblem? CheckContact(string? contact, MandatoryStatus requirement, ClaimProblemType problemType)
    {
        if (string.IsNullOrWhiteSpace(contact))
        {
            return requirement switch
            {
                MandatoryStatus.Optional => null,
                MandatoryStatus.Recommended => new ProfileRelatedProblem(problemType, ProblemSeverity.Hint),
                MandatoryStatus.Required => new ProfileRelatedProblem(problemType, ProblemSeverity.Warning),
                _ => throw new ArgumentOutOfRangeException(nameof(requirement)),
            };
        }
        return null;
    }
}
