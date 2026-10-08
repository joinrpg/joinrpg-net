using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.WebPortal.Managers.Test.Accommodation;

/// <summary>
/// Читающий репозиторий приглашений поверх <see cref="MockedProject"/> — для вью-слоя: входящие и
/// исходящие приглашения заявки, как их отдал бы EF вместе с заявками сторон.
/// </summary>
/// <remarks>
/// Стороны операций над приглашением вью-слою не нужны: если сервис полезет за ними, тест обязан
/// упасть, а не получить что-то правдоподобное.
/// </remarks>
internal sealed class FakeAccommodationInviteRepository(MockedProject mock) : IAccommodationInviteRepository
{
    public Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(Claim claim)
        => GetIncomingInviteForClaim(claim.ClaimId);

    public Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(int claimId)
        => Task.FromResult<IEnumerable<AccommodationInvite>>(
            [.. mock.AccommodationInvites.Where(invite => invite.ToClaimId == claimId)]);

    public Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(Claim claim)
        => GetOutgoingInviteForClaim(claim.ClaimId);

    public Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(int claimId)
        => Task.FromResult<IEnumerable<AccommodationInvite>>(
            [.. mock.AccommodationInvites.Where(invite => invite.FromClaimId == claimId)]);

    public Task<AccommodationInviteParticipants> GetInviteParticipants(AccommodationInviteIdentification inviteId)
        => throw new NotSupportedException();
}
