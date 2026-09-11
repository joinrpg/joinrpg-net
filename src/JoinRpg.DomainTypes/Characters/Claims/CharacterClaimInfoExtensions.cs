namespace JoinRpg.DomainTypes.Characters.Claims;

public static class CharacterClaimInfoExtensions
{
    /// <summary>
    /// Единственная заявка, которую разумно показать пользователю, если их несколько.
    /// </summary>
    /// <remarks>
    /// Зеркало <c>ClaimExtensions.TrySelectSingleClaim</c> для EF-сущностей (ADR013). Порядок
    /// предпочтений тот же: утверждённая, затем обсуждаемая, затем единственная какая есть.
    /// Если однозначного кандидата нет — <c>null</c>.
    /// </remarks>
    public static CharacterClaimInfo? TrySelectSingleClaim(this IReadOnlyCollection<CharacterClaimInfo> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);

        if (claims.Count(c => c.IsApproved) == 1)
        {
            return claims.Single(c => c.IsApproved);
        }
        if (claims.Count(c => c.IsInDiscussion) == 1)
        {
            return claims.Single(c => c.IsInDiscussion);
        }
        if (claims.Count == 1)
        {
            return claims.Single();
        }
        return null;
    }
}
