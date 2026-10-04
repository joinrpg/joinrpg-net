using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Читающий репозиторий приглашений поверх <see cref="MockedProject"/>.
/// </summary>
/// <remarks>
/// Методы, которые обслуживают только вью-слой, не реализованы намеренно: если сервис полезет в
/// них, тест обязан упасть, а не получить пустой список.
/// </remarks>
internal sealed class FakeAccommodationInviteRepository(MockedProject mock) : IAccommodationInviteRepository
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

    public Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(Claim claim)
        => throw new NotSupportedException();

    public Task<IEnumerable<AccommodationInvite>> GetIncomingInviteForClaim(int claimId)
        => throw new NotSupportedException();

    public Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(Claim claim)
        => throw new NotSupportedException();

    public Task<IEnumerable<AccommodationInvite>> GetOutgoingInviteForClaim(int claimId)
        => throw new NotSupportedException();
}
