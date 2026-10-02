using JoinRpg.Data.Interfaces.Subscribe;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Подписки мастеров, заданные тестом напрямую: <see cref="Direct"/> — подписки на конкретные
/// заявки, <see cref="ForCharacters"/> — подписки по персонажам и дереву групп.
/// </summary>
internal sealed class FakeUserSubscribeRepository : IUserSubscribeRepository
{
    /// <summary>Подписки, которые вернутся на любой запрос по заявкам.</summary>
    public List<UserSubscribe> Direct { get; } = [];

    /// <summary>Подписки, которые вернутся на любой запрос по персонажам и группам.</summary>
    public List<UserSubscribe> ForCharacters { get; } = [];

    public Task<IReadOnlyCollection<UserSubscribe>> GetDirect(IReadOnlyCollection<ClaimIdentification> claimId)
        => Task.FromResult<IReadOnlyCollection<UserSubscribe>>(Direct);

    public Task<IReadOnlyCollection<UserSubscribe>> GetForCharAndGroups(
        IReadOnlyCollection<CharacterGroupIdentification> characterGroupIdentifications,
        IReadOnlyCollection<CharacterIdentification> characterId)
        => Task.FromResult<IReadOnlyCollection<UserSubscribe>>(ForCharacters);

    public Task<(User User, UserSubscriptionDto[] UserSubscriptions)> LoadSubscriptionsForProject(UserIdentification userId, ProjectIdentification projectId) => throw new NotSupportedException();
    public Task<UserSubscriptionDto> LoadSubscriptionById(ProjectIdentification projectId, int subscriptionId) => throw new NotSupportedException();
}
