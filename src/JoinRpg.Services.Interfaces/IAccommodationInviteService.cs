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
        AccommodationGroupIdentification target);

    /// <summary>
    /// Принять приглашение: приглашённый вместе со своими соседями переезжает в группу
    /// приглашающего.
    /// </summary>
    /// <exception cref="AccommodationInviteNotAllowedException">
    /// Принять нельзя: на приглашение уже ответили, стороны уже живут вместе, у приглашающего нет
    /// типа проживания или в номере не хватает мест.
    /// </exception>
    Task AcceptAccommodationInvite(AccommodationInviteIdentification inviteId);

    /// <summary>
    /// Отказаться от приглашения. Делает это приглашённый, поэтому и доступ требуется к его заявке.
    /// </summary>
    /// <exception cref="AccommodationInviteNotAllowedException">На приглашение уже ответили.</exception>
    Task DeclineAccommodationInvite(AccommodationInviteIdentification inviteId);

    /// <summary>
    /// Отозвать отправленное приглашение. Делает это приглашающий — отсюда и другой требуемый
    /// доступ, и другое состояние, в которое уходит приглашение.
    /// </summary>
    /// <exception cref="AccommodationInviteNotAllowedException">На приглашение уже ответили.</exception>
    Task CancelAccommodationInvite(AccommodationInviteIdentification inviteId);
}
