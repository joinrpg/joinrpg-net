using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;

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
            claim.Player.FullName,
            claim.Player.Extra?.PassportData,
            claim.Player.Extra?.RegistrationAddress);

        // Доступ к паспорту/адресу — согласие на уровне заявки (claim.PlayerAllowedSenstiveData),
        // а не факт о профиле, поэтому не входит в UserProfileItemType и передаётся в калькулятор
        // отдельно: пока доступа нет, он не считает паспорт/адрес недостающими.
        var sensitiveDataAllowed = !projectInfo.ProfileRequirementSettings.SensitiveDataRequired
            || claim.PlayerAllowedSenstiveData;

        var problems = UserProfileProblemsCalculator
            .GetProblems(missingItems, projectInfo.ProfileRequirementSettings, sensitiveDataAllowed)
            .Select(ToClaimProblem);

        if (!sensitiveDataAllowed)
        {
            // ClaimProblemType.SensitiveDataNotAllowed не привязан к UserProfileItemType (это не
            // поле профиля, а отказ в доступе), поэтому калькулятор его не производит — добавляем
            // здесь, единственном месте, знающем про ClaimProblemType.
            problems = problems.Append(new ProfileRelatedProblem(ClaimProblemType.SensitiveDataNotAllowed, ProblemSeverity.Warning));
        }

        return problems;
    }

    private static ProfileRelatedProblem ToClaimProblem(UserProfileProjectProblem problem)
        => new(ToClaimProblemType(problem.ItemType), problem.Severity);

    internal static ClaimProblemType ToClaimProblemType(UserProfileItemType itemType) => itemType switch
    {
        UserProfileItemType.Telegram => ClaimProblemType.MissingTelegram,
        UserProfileItemType.Vkontakte => ClaimProblemType.MissingVkontakte,
        UserProfileItemType.Phone => ClaimProblemType.MissingPhone,
        UserProfileItemType.RealName => ClaimProblemType.MissingRealname,
        UserProfileItemType.Passport => ClaimProblemType.MissingPassport,
        UserProfileItemType.RegistrationAddress => ClaimProblemType.MissingRegistrationAddress,
        _ => throw new ArgumentOutOfRangeException(nameof(itemType)),
    };
}
