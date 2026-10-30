using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Читающий репозиторий приглашений к совместному проживанию поверх <see cref="MockedProject"/> —
/// общий для тестов сервиса и вью-слоя: стороны операции над приглашением и входящие/исходящие
/// приглашения заявки, как их отдал бы EF вместе с заявками сторон.
/// </summary>
public sealed class FakeAccommodationInviteRepository(MockedProject mock) : IAccommodationInviteRepository
{
    public Task<AccommodationInviteParticipants> GetInviteParticipants(AccommodationInviteIdentification inviteId)
    {
        var invite = mock.AccommodationInvites.SingleOrDefault(
                invite => invite.Id == inviteId.AccommodationInviteId
                    && invite.ProjectId == inviteId.ProjectId.Value)
            ?? throw new JoinRpgEntityNotFoundException(
                inviteId.AccommodationInviteId, nameof(AccommodationInvite));

        return Task.FromResult(new AccommodationInviteParticipants(
            new ClaimIdentification(inviteId.ProjectId, invite.FromClaimId),
            new ClaimIdentification(inviteId.ProjectId, invite.ToClaimId)));
    }

    public Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(int claimId)
        => Task.FromResult<IEnumerable<AccommodationInvite>>(
            [.. mock.AccommodationInvites.Where(invite => invite.ToClaimId == claimId)]);

    public Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(int claimId)
        => Task.FromResult<IEnumerable<AccommodationInvite>>(
            [.. mock.AccommodationInvites.Where(invite => invite.FromClaimId == claimId)]);
}
