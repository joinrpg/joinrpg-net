using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.AddClaim;

public class ClaimAcceptOrMoveValidationExtensionsTest
{
    [Fact]
    public void ToAddClaimForbideReasonIsDefinedForEveryUserProfileItemType()
    {
        foreach (var itemType in Enum.GetValues<UserProfileItemType>())
        {
            Should.NotThrow(() => ClaimAcceptOrMoveValidationExtensions.ToAddClaimForbideReason(itemType));
        }
    }
}
