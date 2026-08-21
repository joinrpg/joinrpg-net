using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Web.Accommodation.Test;

internal sealed class FakeAccommodationInviteClient : IAccommodationInviteClient
{
    public AccommodationInviteTargetsViewModel NextTargets { get; set; } =
        new(SenderRequestId: null, RoomFreeSpace: 0, Targets: []);

    public Dictionary<InviteDirection, IReadOnlyCollection<AccommodationInviteViewModel>> NextInvites { get; } = [];

    public List<(ClaimIdentification ClaimId, AccommodationTargetIdentification Target)> CreatedInvites { get; } = [];

    public List<(string Action, AccommodationInviteIdentification InviteId)> Answers { get; } = [];

    public Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId)
        => Task.FromResult(NextTargets);

    public Task CreateInvite(ClaimIdentification claimId, AccommodationTargetIdentification target)
    {
        CreatedInvites.Add((claimId, target));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<AccommodationInviteViewModel>> GetInvites(
        ClaimIdentification claimId,
        InviteDirection direction)
        => Task.FromResult(NextInvites.TryGetValue(direction, out var invites) ? invites : []);

    public Task AcceptInvite(AccommodationInviteIdentification inviteId) => Record(nameof(AcceptInvite), inviteId);

    public Task DeclineInvite(AccommodationInviteIdentification inviteId) => Record(nameof(DeclineInvite), inviteId);

    public Task CancelInvite(AccommodationInviteIdentification inviteId) => Record(nameof(CancelInvite), inviteId);

    private Task Record(string action, AccommodationInviteIdentification inviteId)
    {
        Answers.Add((action, inviteId));
        return Task.CompletedTask;
    }
}
