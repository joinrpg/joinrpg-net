using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Репозиторий пользователей поверх <see cref="MockedProject"/>: знает только игрока и мастера мока.
/// </summary>
/// <remarks>
/// Реализованы ровно те методы, которые нужны проверяемым операциям. Остальные бросают
/// <see cref="NotSupportedException"/> намеренно — чтобы поход за незапланированными данными был
/// виден в тесте, а не подменялся пустышкой.
/// </remarks>
internal sealed class FakeUserRepository(MockedProject mock) : IUserRepository
{
    public Task<UserInfo?> GetUserInfo(UserIdentification userId)
        => Task.FromResult(mock.TryGetUserInfo(userId));

    public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds)
        => Task.FromResult<IReadOnlyCollection<UserInfo>>(
            [.. userIds.Select(mock.TryGetUserInfo).OfType<UserInfo>()]);

    public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds)
        => Task.FromResult<IReadOnlyCollection<UserInfoHeader>>(
            [.. userIds.Select(mock.TryGetUserInfo).OfType<UserInfo>()
                .Select(user => new UserInfoHeader(user.UserId, user.DisplayName))]);

    public Task<User> GetById(int id) => throw new NotSupportedException();
    public Task<User> WithProfile(int userId) => throw new NotSupportedException();
    public Task<User> GetWithSubscribe(int currentUserId) => throw new NotSupportedException();
    public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw new NotSupportedException();
    public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders() => throw new NotSupportedException();
    public Task<UserIdentification?> FindByVk(string vkId) => throw new NotSupportedException();
    public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw new NotSupportedException();
    public Task<UserIdentification?> FindByEmail(string email) => throw new NotSupportedException();
}
