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

    /// <summary>
    /// Отвечал ли мастер по заявке за последние <paramref name="days"/> дней.
    /// </summary>
    /// <remarks>
    /// Считается по последнему мастерскому комментарию, видимому игроку: «мастер отработал
    /// заявку» — это именно ответ игроку, а внутренние мастерские комментарии к этому не
    /// относятся. Сравнение идёт с локальным «сейчас» (<see cref="DateTimeOffset.Now"/>) —
    /// перенесено один в один из версии для EF-сущности.
    /// </remarks>
    public static bool HasMasterCommentsInLastXDays(this CharacterClaimInfo claim, int days)
    {
        ArgumentNullException.ThrowIfNull(claim);

        return claim.LastVisibleMasterCommentAt?.AddDays(days) >= DateTimeOffset.Now;
    }
}
