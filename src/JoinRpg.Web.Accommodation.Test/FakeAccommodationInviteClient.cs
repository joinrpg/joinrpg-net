using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Web.Accommodation.Test;

internal sealed class FakeAccommodationInviteClient : IAccommodationInviteClient
{
    public AccommodationInviteTargetsViewModel NextTargets { get; set; } =
        new(SenderRequestId: null, RoomFreeSpace: 0, Targets: [], AccommodationInviteMode.Invite);

    public Dictionary<InviteDirection, IReadOnlyCollection<AccommodationInviteViewModel>> NextInvites { get; } = [];

    public List<(ClaimIdentification ClaimId, AccommodationGroupIdentification Target)> CreatedInvites { get; } = [];

    public List<(ClaimIdentification ClaimId, AccommodationGroupIdentification Target)> MergedGroups { get; } = [];

    public List<(string Action, AccommodationInviteIdentification InviteId)> Answers { get; } = [];

    /// <summary>
    /// Чем завершаются действия (пригласить, поселить вместе, ответить). <c>null</c> — успехом.
    /// Отказ сервера клиент передаёт как <see cref="JoinRpg.Common.WebComponents.ServerProblemException"/>.
    /// </summary>
    public Exception? Failure { get; set; }

    public Task<AccommodationInviteTargetsViewModel> GetInviteTargets(ClaimIdentification claimId)
        => Task.FromResult(NextTargets);

    public Task CreateInvite(ClaimIdentification claimId, AccommodationGroupIdentification target)
    {
        CreatedInvites.Add((claimId, target));
        return Complete();
    }

    public Task MergeGroups(ClaimIdentification claimId, AccommodationGroupIdentification target)
    {
        MergedGroups.Add((claimId, target));
        return Complete();
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
        return Complete();
    }

    private Task Complete() => Failure is null ? Task.CompletedTask : Task.FromException(Failure);
}
