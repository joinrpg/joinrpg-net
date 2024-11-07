using JoinRpg.DataModel;

namespace JoinRpg.Domain;

public static class ClaimSourceExtensions
{
    public static bool HasActiveClaims(this IClaimSource target) => target.Claims.Any(claim => claim.ClaimStatus.IsActive());
}
