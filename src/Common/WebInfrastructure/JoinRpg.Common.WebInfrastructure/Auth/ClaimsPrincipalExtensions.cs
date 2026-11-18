using System.Security.Claims;
using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Common.WebInfrastructure.Auth;

public static class ClaimsPrincipalExtensions
{
    public static UserIdentification GetJoinrpgUserId(this ClaimsPrincipal user)
    {
        return user.TryGetJoinrpgUserId() ?? throw new InvalidOperationException("User has no valid JoinrpgUserId claim");
    }

    public static UserIdentification? TryGetJoinrpgUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return UserIdentification.TryParse(value, provider: null, out var userId) ? userId : null;
    }
}
