using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.AddClaim;

/// <summary>
/// Каждому пункту профиля должна соответствовать причина запрета — иначе правило про контакты
/// упадёт на новом типе пункта.
/// </summary>
public class ToAddClaimForbideReasonTest
{
    [Fact]
    public void ToAddClaimForbideReasonIsDefinedForEveryUserProfileItemType()
    {
        foreach (var itemType in Enum.GetValues<UserProfileItemType>())
        {
            Should.NotThrow(() => ClaimValidator.ToAddClaimForbideReason(itemType));
        }
    }
}
