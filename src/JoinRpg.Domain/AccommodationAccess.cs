using System.Diagnostics.CodeAnalysis;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain;

/// <summary>
/// Кто может менять проживание по заявке. Одно правило на всех: по нему проверяют доступ операции
/// с проживанием (<c>ClaimAccessRequirement.AccommodationChange</c>), и по нему же UI решает,
/// показывать ли кнопки, — иначе кнопка видна, а нажатие возвращает отказ.
/// </summary>
public static class AccommodationAccess
{
    /// <summary>
    /// Какие послабления сверх права <see cref="Permission.CanSetPlayersAccommodations"/> даёт
    /// заявка в статусе <paramref name="status"/>.
    /// </summary>
    /// <remarks>
    /// У утверждённой заявки поселение может менять сам игрок или ответственный мастер,
    /// у остальных — только обладатель права. Сравнение именно с <see cref="ClaimStatus.Approved"/>,
    /// а не с «уже утверждена», куда входит ещё и <see cref="ClaimStatus.CheckedIn"/>: после
    /// регистрации игрок своё проживание уже не меняет — это делает мастер с правом расселять.
    /// Так работало и до ADR014, и так решено оставить.
    /// </remarks>
    public static ExtraAccessReason GetExtraAccessReason(ClaimStatus status)
        => status == ClaimStatus.Approved ? ExtraAccessReason.PlayerOrResponsible : ExtraAccessReason.None;

    /// <summary>
    /// Может ли <paramref name="userId"/> менять проживание по заявке <paramref name="claim"/>.
    /// </summary>
    public static bool CanChangeAccommodation(this Claim claim, UserIdentification? userId)
        => claim.HasAccess(userId,
            Permission.CanSetPlayersAccommodations,
            GetExtraAccessReason(claim.ClaimStatus));

    /// <summary>
    /// Проверить, что <paramref name="userId"/> может менять проживание по заявке, иначе бросить отказ.
    /// </summary>
    /// <exception cref="NoAccessToProjectException">Менять проживание по этой заявке нельзя</exception>
    public static Claim RequestAccommodationChangeAccess(
        [NotNull] this Claim? claim,
        UserIdentification? userId)
    {
        ArgumentNullException.ThrowIfNull(claim);

        if (!claim.CanChangeAccommodation(userId))
        {
            throw new NoAccessToProjectException(claim, userId?.Value);
        }

        return claim;
    }
}
