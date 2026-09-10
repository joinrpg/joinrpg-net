using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Helpers;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ClaimContactsMissingFilter : IProblemFilter<Claim>
{
    public IEnumerable<ClaimProblem> GetProblems(Claim claim, ProjectInfo projectInfo)
    {
        return new[]{
            CheckContact(claim.Player.Extra?.Telegram, projectInfo.ProfileRequirementSettings.RequireTelegram, ClaimProblemType.MissingTelegram),
            CheckContact(claim.Player.Extra?.VkVerified == true ? claim.Player.Extra.Vk : null, projectInfo.ProfileRequirementSettings.RequireVkontakte, ClaimProblemType.MissingVkontakte),
            CheckContact(claim.Player.Extra?.PhoneNumber, isMissing: value => !UserInfo.IsCorrectContact(value), projectInfo.ProfileRequirementSettings.RequirePhone, ClaimProblemType.MissingPhone),
            CheckContact(claim.Player.FullName, isMissing: value => !UserInfo.IsCorrectContact(value), projectInfo.ProfileRequirementSettings.RequireRealName, ClaimProblemType.MissingRealname),

        }
        .Union(CheckSensitiveDataAccess(claim, projectInfo))
        .WhereNotNull();
    }

    private static IEnumerable<ClaimProblem?> CheckSensitiveDataAccess(Claim claim, ProjectInfo projectInfo)
    {
        if (projectInfo.ProfileRequirementSettings.SensitiveDataRequired)
        {
            if (claim.PlayerAllowedSenstiveData)
            {
                yield return CheckContact(claim.Player.Extra?.PassportData, projectInfo.ProfileRequirementSettings.RequirePassport, ClaimProblemType.MissingPassport);
                yield return CheckContact(claim.Player.Extra?.RegistrationAddress, projectInfo.ProfileRequirementSettings.RequireRegistrationAddress, ClaimProblemType.MissingRegistrationAddress);

            }
            else
            {
                yield return new ProfileRelatedProblem(ClaimProblemType.SensitiveDataNotAllowed, ProblemSeverity.Warning);
            }
        }
    }

    private static ProfileRelatedProblem? CheckContact(string? contact, MandatoryStatus requirement, ClaimProblemType problemType)
        => CheckContact(contact, isMissing: string.IsNullOrWhiteSpace, requirement, problemType);

    private static ProfileRelatedProblem? CheckContact(string? contact, Func<string?, bool> isMissing, MandatoryStatus requirement, ClaimProblemType problemType)
    {
        if (isMissing(contact))
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
