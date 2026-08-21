using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Services.Interfaces;

public interface IAccommodationInviteService
{
    /// <summary>
    /// Пригласить к совместному проживанию. В зависимости от того, во что разворачивается
    /// <paramref name="target"/>, приглашается либо одна заявка, либо вся группа проживающих.
    /// </summary>
    /// <exception cref="AccommodationInviteNotAllowedException">
    /// Приглашение невозможно: кто-то уже расселён по комнатам, типы проживания не совпадают
    /// или в номере не хватает мест.
    /// </exception>
    Task CreateAccommodationInvite(
        ClaimIdentification senderClaimId,
        AccommodationRequestIdentification senderRequestId,
        AccommodationTargetIdentification target);

    Task<AccommodationInvite?> CancelOrDeclineAccommodationInvite(
        AccommodationInviteIdentification inviteId,
        InviteState newState);

    Task<AccommodationInvite?> AcceptAccommodationInvite(AccommodationInviteIdentification inviteId);

    Task DeclineAllClaimInvites(ClaimIdentification claimId);
}
