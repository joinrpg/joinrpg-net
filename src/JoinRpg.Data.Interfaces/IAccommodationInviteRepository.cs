using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Data.Interfaces;

public interface IAccommodationInviteRepository
{
    Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(Claim claim);
    Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(int claimId);

    Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(Claim claim);
    Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(int claimId);

    /// <summary>
    /// Стороны приглашения: кто пригласил и кого.
    /// </summary>
    /// <remarks>
    /// Нужны до начала мутации, чтобы выбрать её корень: операция над приглашением — это операция
    /// над заявкой действующей стороны (ADR014), а на входе у неё только идентификатор приглашения.
    /// Само приглашение затем перечитывается загрузчиком хэндла, поэтому решения о его состоянии
    /// принимаются уже внутри операции, на трекаемой сущности.
    /// </remarks>
    /// <exception cref="JoinRpgEntityNotFoundException">Приглашения в этом проекте нет.</exception>
    Task<AccommodationInviteParticipants> GetInviteParticipants(AccommodationInviteIdentification inviteId);
}

/// <summary>Стороны приглашения к совместному проживанию.</summary>
/// <param name="Sender">Заявка, которая пригласила.</param>
/// <param name="Receiver">Заявка, которую пригласили.</param>
public record AccommodationInviteParticipants(ClaimIdentification Sender, ClaimIdentification Receiver);
