using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Users;

/// <summary>
/// Незаполненный элемент профиля пользователя в контексте требований конкретного проекта.
/// </summary>
public record UserProfileProjectProblem(UserProfileItemType ItemType, ProblemSeverity Severity);

/// <summary>
/// Сопоставляет незаполненные элементы профиля (<see cref="UserProfileItemsCalculator"/>)
/// с требованиями проекта (<see cref="ProjectProfileRequirementSettings"/>), выдавая единый
/// список проблем с severity. Дальше каждый потребитель сам решает, как их использовать —
/// маппит в <c>AddClaimForbideReason</c>, в <c>ClaimProblemType</c> и т.д., дополнительно
/// фильтруя по необходимости (например, только Required).
/// </summary>
public static class UserProfileProblemsCalculator
{
    public static IReadOnlyCollection<UserProfileProjectProblem> GetProblems(
        UserInfo userInfo,
        ProjectProfileRequirementSettings requirementSettings)
        => GetProblems(userInfo.GetMissingItems(), requirementSettings);

    public static IReadOnlyCollection<UserProfileProjectProblem> GetProblems(
        IReadOnlyCollection<UserProfileItemType> missingItems,
        ProjectProfileRequirementSettings requirementSettings)
    {
        List<UserProfileProjectProblem> problems = [];

        AddIfMissing(UserProfileItemType.Telegram, requirementSettings.RequireTelegram);
        AddIfMissing(UserProfileItemType.Vkontakte, requirementSettings.RequireVkontakte);
        AddIfMissing(UserProfileItemType.Phone, requirementSettings.RequirePhone);
        AddIfMissing(UserProfileItemType.RealName, requirementSettings.RequireRealName);

        return problems;

        void AddIfMissing(UserProfileItemType itemType, MandatoryStatus requirement)
        {
            if (!missingItems.Contains(itemType))
            {
                return;
            }

            if (ToSeverity(requirement) is { } severity)
            {
                problems.Add(new UserProfileProjectProblem(itemType, severity));
            }
        }
    }

    public static ProblemSeverity? ToSeverity(MandatoryStatus requirement) => requirement switch
    {
        MandatoryStatus.Optional => null,
        MandatoryStatus.Recommended => ProblemSeverity.Hint,
        MandatoryStatus.Required => ProblemSeverity.Warning,
        _ => throw new ArgumentOutOfRangeException(nameof(requirement)),
    };
}
