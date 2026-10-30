using System.Diagnostics.Contracts;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain;

public static class ClaimExtensions
{

    /// <summary>
    /// Другие активные заявки того же игрока в этом проекте. Заявки «на паузе» не входят: конкурентом
    /// для <c>StrictlyOneCharacter</c> они не считаются (#5395).
    /// </summary>
    public static IEnumerable<Claim> OtherActiveClaimsForThisPlayer(this Claim claim)
        => claim.Player.Claims.Where(c => c.ClaimId != claim.ClaimId && c.ClaimStatus.IsActive() && c.ProjectId == claim.ProjectId);

    /// <summary>
    /// Returns true when approval is blocked by other claims for character of the current claim
    /// </summary>
    public static bool HasOtherClaimsForThisCharacter(this ClaimInCharacter claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        if (claim.Claim.IsApproved)
        {
            return false;
        }
        if (claim.Character.CharacterType == CharacterType.Slot)
        {
            return false;
        }
        return claim.Character.Claims.Any(c => c.PlayerId != claim.Claim.PlayerId && c.IsActive);
    }

    public static void EnsureStatus(this Claim claim, params ClaimStatus[] possibleStatus)
    {
        if (!possibleStatus.Contains(claim.ClaimStatus))
        {
            throw new ClaimWrongStatusException(claim.GetId(), claim.ClaimStatus, possibleStatus);
        }
    }

    /// <summary>
    /// Помечает персонажа изменённым, если заявка утверждена: правка утверждённой заявки — это правка
    /// персонажа.
    /// </summary>
    /// <param name="claim">Заявка, которую меняет операция.</param>
    /// <param name="fallbackCharacter">Персонаж операции — на случай, если у заявки персонаж не загружен.</param>
    /// <param name="now">Время операции.</param>
    /// <param name="userId">Кто меняет.</param>
    public static void MarkCharacterChangedIfApproved(this Claim claim, Character fallbackCharacter, DateTime now, int userId)
    {
        if (claim.ClaimStatus == ClaimStatus.Approved)
        {
            // Персонаж берётся из заявки, а не из хэндла: при утверждении заявки на слот заявка
            // к этому моменту уже переехала на только что созданного персонажа.
            EntityAudit.MarkChanged(claim.Character ?? fallbackCharacter, now, userId);
        }
    }

    [Pure]
    public static Claim? TrySelectSingleClaim(this IReadOnlyCollection<Claim> claims)
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
