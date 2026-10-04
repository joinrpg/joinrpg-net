using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Репозиторий пользователей в тестах — общий для всех тестовых проектов.
/// </summary>
/// <remarks>
/// Тест настраивает ровно те источники данных, которые нужны проверяемой операции; всё
/// ненастроенное бросает <see cref="NotSupportedException"/> намеренно — чтобы поход за
/// незапланированными данными был виден в тесте, а не подменялся пустышкой. Если смысл фейка
/// именно в том, что обращения быть не должно, — <see cref="MustNotBeCalled(string)"/>: он
/// объяснит в сообщении, почему.
/// </remarks>
/// <param name="userInfos">Источник <see cref="UserInfo"/> по идентификатору; <c>null</c> для неизвестного пользователя.</param>
/// <param name="users">Источник EF-сущностей <see cref="User"/> для <see cref="GetById"/>.</param>
/// <param name="admins">Что отдавать на <see cref="GetAdminUserInfoHeaders"/>.</param>
/// <param name="mustNotBeCalledReason">Почему обращения быть не должно — попадёт в сообщение исключения.</param>
public sealed class FakeUserRepository(
    Func<UserIdentification, UserInfo?>? userInfos = null,
    Func<int, User?>? users = null,
    IReadOnlyCollection<UserInfoHeader>? admins = null,
    string? mustNotBeCalledReason = null) : IUserRepository
{
    /// <summary>
    /// Репозиторий поверх мока: знает ровно известных моку игрока, мастера и добавленных тестом
    /// мастеров. Остального (аватарок, поиска по соцсетям, списка админов) мок не знает, и
    /// обращение за ним падает.
    /// </summary>
    public FakeUserRepository(MockedProject mock) : this(mock.TryGetUserInfo, mock.TryGetUser)
    {
    }

    /// <summary>
    /// Фейк поверх фиксированного набора пользователей: знает ровно их, а на любого другого
    /// отвечает «пользователь не найден» (а не падает).
    /// </summary>
    public static FakeUserRepository WithUsers(params UserInfo[] userInfos)
    {
        var byId = userInfos.ToDictionary(u => u.UserId);
        return new FakeUserRepository(userId => byId.GetValueOrDefault(userId));
    }

    /// <summary>Фейк, который умеет только отдать список админов.</summary>
    public static FakeUserRepository WithAdmins(params UserInfoHeader[] admins) => new(admins: admins);

    /// <summary>Фейк, в который ходить нельзя: любое обращение падает с указанной причиной.</summary>
    public static FakeUserRepository MustNotBeCalled(string reason) => new(mustNotBeCalledReason: reason);

    /// <summary>
    /// Сколько раз сходили в репозиторий (любым методом) — именно запросов, а не пользователей в
    /// них. Нужно там, где тест проверяет, что сервис ходит в базу один раз на все значения, а не
    /// по разу на значение.
    /// </summary>
    public int CallCount { get; private set; }

    public Task<UserInfo?> GetUserInfo(UserIdentification userId)
    {
        CallCount++;
        return Task.FromResult(Resolve(userId));
    }

    public Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds)
    {
        CallCount++;
        return Task.FromResult<IReadOnlyCollection<UserInfo>>([.. userIds.Select(Resolve).OfType<UserInfo>()]);
    }

    public Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds)
    {
        CallCount++;
        return Task.FromResult<IReadOnlyCollection<UserInfoHeader>>(
            [.. userIds.Select(Resolve).OfType<UserInfo>().Select(user => new UserInfoHeader(user.UserId, user.DisplayName))]);
    }

    public Task<IReadOnlyDictionary<UserIdentification, PhoneNumber>> GetPhoneNumbers(
        IReadOnlyCollection<UserIdentification> userIds)
    {
        CallCount++;
        return Task.FromResult<IReadOnlyDictionary<UserIdentification, PhoneNumber>>(
            userIds.Select(Resolve).OfType<UserInfo>()
                .Select(user => (user.UserId, Phone: PhoneNumber.FromOptional(user.PhoneNumber)))
                .Where(x => x.Phone is not null)
                .ToDictionary(x => x.UserId, x => x.Phone!));
    }

    public Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders()
    {
        CallCount++;
        return Task.FromResult(admins ?? throw NotConfigured("список админов"));
    }

    public Task<User> GetById(int id)
    {
        CallCount++;
        var resolve = users ?? throw NotConfigured("EF-сущности пользователей");
        return Task.FromResult(resolve(id) ?? throw new NotSupportedException($"Тесту неизвестен пользователь {id}"));
    }

    public Task<User> WithProfile(int userId) => throw NotConfigured("профиль пользователя");
    public Task<User> GetWithSubscribe(int currentUserId) => throw NotConfigured("подписки пользователя");
    public Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId) => throw NotConfigured("аватарки");
    public Task<UserIdentification?> FindByVk(string vkId) => throw NotConfigured("поиск по ВК");
    public Task<UserIdentification?> FindByTelegram(string telegramUsername) => throw NotConfigured("поиск по телеграму");
    public Task<UserIdentification?> FindByEmail(string email) => throw NotConfigured("поиск по email");

    private UserInfo? Resolve(UserIdentification userId)
        => (userInfos ?? throw NotConfigured("данные пользователей"))(userId);

    private NotSupportedException NotConfigured(string what)
        => new(mustNotBeCalledReason is null
            ? $"Обращение к репозиторию пользователей за «{what}» не предусмотрено тестом. Если так и задумано — настрой источник в FakeUserRepository."
            : $"Обращения к репозиторию пользователей (за «{what}») быть не должно: {mustNotBeCalledReason}");
}
