using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Helpers;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ClaimContactsMissingFilter : IProblemFilter<Claim>
{
    public IEnumerable<ClaimProblem> GetProblems(Claim claim, ProjectInfo projectInfo)
    {
        // TODO этот фильтр работает напрямую с EF-сущностью Claim, а не с UserInfo (его сборка
        // через claim.Player.GetUserInfo() неэффективна — тянет Claims/ProjectAcls). Когда
        // ProblemValidator/фильтры проблем заявки научатся работать с UserInfo, убрать этот
        // ручной вызов GetMissingItems и перейти на UserProfileProblemsCalculator.GetProblems(userInfo, ...).
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
        => new(ToClaimProblemType(problem.ItemType), problem.Severity);

    internal static ClaimProblemType ToClaimProblemType(UserProfileItemType itemType) => itemType switch
    {
        UserProfileItemType.Telegram => ClaimProblemType.MissingTelegram,
        UserProfileItemType.Vkontakte => ClaimProblemType.MissingVkontakte,
        UserProfileItemType.Phone => ClaimProblemType.MissingPhone,
        UserProfileItemType.RealName => ClaimProblemType.MissingRealname,
        _ => throw new ArgumentOutOfRangeException(nameof(itemType)),
    };

    // TODO(#4763) паспорт/адрес регистрации ещё не переведены на UserProfileItemType +
    // UserProfileProblemsCalculator — они не входят в UserInfo и дополнительно зависят от
    // claim.PlayerAllowedSenstiveData (факт про заявку, а не про профиль пользователя).
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
        if (!string.IsNullOrWhiteSpace(contact))
        {
            return null;
        }

        return UserProfileProblemsCalculator.ToSeverity(requirement) is { } severity
            ? new ProfileRelatedProblem(problemType, severity)
            : null;
    }
}
