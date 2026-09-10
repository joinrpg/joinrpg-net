using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Helpers;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ClaimContactsMissingFilter : IProblemFilter<Claim>
{
    public IEnumerable<ClaimProblem> GetProblems(Claim claim, ProjectInfo projectInfo)
    {
        var missingItems = UserProfileItemsCalculator.GetMissingItems(
            hasTelegram: !string.IsNullOrWhiteSpace(claim.Player.Extra?.Telegram),
            hasVerifiedVkontakte: claim.Player.Extra?.VkVerified == true && !string.IsNullOrWhiteSpace(claim.Player.Extra.Vk),
            claim.Player.Extra?.PhoneNumber,
            claim.Player.FullName);

        var contactProblems = UserProfileProblemsCalculator.GetProblems(missingItems, projectInfo.ProfileRequirementSettings)
            .Select(ToClaimProblem);

        return contactProblems
            .Union(CheckSensitiveDataAccess(claim, projectInfo))
            .WhereNotNull();
    }

    private static ProfileRelatedProblem ToClaimProblem(UserProfileProjectProblem problem)
        => new(problem.ItemType switch
        {
            UserProfileItemType.Telegram => ClaimProblemType.MissingTelegram,
            UserProfileItemType.Vkontakte => ClaimProblemType.MissingVkontakte,
            UserProfileItemType.Phone => ClaimProblemType.MissingPhone,
            UserProfileItemType.RealName => ClaimProblemType.MissingRealname,
            _ => throw new ArgumentOutOfRangeException(nameof(problem)),
        }, problem.Severity);

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
