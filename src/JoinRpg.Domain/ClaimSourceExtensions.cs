namespace JoinRpg.Domain;

public static class ClaimSourceExtensions
{
    public static bool HasActiveClaims(this Character target) => target.Claims.Any(claim => claim.ClaimStatus.IsActive());
}
