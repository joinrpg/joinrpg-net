using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DomainTypes.Test.Users;

public class UserProfileProblemsCalculatorTest
{
    [Fact]
    public void ToSeverityIsDefinedForEveryMandatoryStatus()
    {
        foreach (var status in Enum.GetValues<MandatoryStatus>())
        {
            Should.NotThrow(() => UserProfileProblemsCalculator.ToSeverity(status));
        }
    }

    [Fact]
    public void GetProblemsCoversEveryMissingItem()
    {
        var allRequired = new ProjectProfileRequirementSettings(
            RequireRealName: MandatoryStatus.Required,
            RequireTelegram: MandatoryStatus.Required,
            RequireVkontakte: MandatoryStatus.Required,
            RequirePhone: MandatoryStatus.Required,
            RequirePassport: MandatoryStatus.Optional,
            RequireRegistrationAddress: MandatoryStatus.Optional);

        var missingItems = Enum.GetValues<UserProfileItemType>();

        var problems = Should.NotThrow(() => UserProfileProblemsCalculator.GetProblems(missingItems, allRequired));

        problems.Select(p => p.ItemType).ShouldBe(missingItems, ignoreOrder: true);
    }
}
