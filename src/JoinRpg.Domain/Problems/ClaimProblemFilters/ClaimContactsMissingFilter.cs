using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ClaimContactsMissingFilter : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimProblemContext context)
    {
        // Доступ к паспорту/адресу — согласие на уровне заявки
        // (CharacterClaimInfo.PlayerAllowedSensitiveData), а не факт о профиле, поэтому не входит
        // в UserProfileItemType и передаётся в калькулятор отдельно: пока доступа нет, он не
        // считает паспорт/адрес недостающими.
        var sensitiveDataAllowed = !context.ProjectInfo.ProfileRequirementSettings.SensitiveDataRequired
            || context.Claim.PlayerAllowedSensitiveData;

        var problems = UserProfileProblemsCalculator
            .GetProblems(context.Player, context.ProjectInfo.ProfileRequirementSettings, sensitiveDataAllowed)
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
