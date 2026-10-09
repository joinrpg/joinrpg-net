using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Data.Interfaces;

public interface IUserRepository
{
    Task<User> GetById(int id);

    Task<User> WithProfile(int userId);
    Task<User> GetWithSubscribe(int currentUserId);
    Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId);

    Task<UserInfo?> GetUserInfo(UserIdentification userId);

    Task<IReadOnlyCollection<UserInfo>> GetUserInfos(IReadOnlyCollection<UserIdentification> userIds);

    Task<IReadOnlyCollection<UserInfoHeader>> GetUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds);

    /// <summary>
    /// Телефоны пользователей пачкой. Пользователи без телефона в словарь не попадают.
    /// </summary>
    /// <remarks>
    /// Отдельно от <see cref="GetUserInfos"/>: тому нужен весь профиль, и он тянет вложенными
    /// проекциями заявки, доступы к проектам и внешние логины. Печать конвертов открывается на
    /// весь проект — это до тысячи игроков, — а из профиля ей нужен ровно телефон.
    /// </remarks>
    Task<IReadOnlyDictionary<UserIdentification, PhoneNumber>> GetPhoneNumbers(
        IReadOnlyCollection<UserIdentification> userIds);

    async Task<UserInfo> GetRequiredUserInfo(UserIdentification userId)
    {
        return await GetUserInfo(userId) ?? throw new JoinRpgEntityNotFoundException(userId, "user");
    }
    async Task<IReadOnlyCollection<UserInfo>> GetRequiredUserInfos(IReadOnlyCollection<UserIdentification> userIds)
    {
        var result = await GetUserInfos(userIds);
        if (result.Count != userIds.Count)
        {
            throw new JoinRpgEntityNotFoundException(userIds.Except(result.Select(x => x.UserId)).First(), "user");
        }
        return result;
    }


    async Task<IReadOnlyCollection<UserInfoHeader>> GetRequiredUserInfoHeaders(IReadOnlyCollection<UserIdentification> userIds)
    {
        var result = await GetUserInfoHeaders(userIds);
        if (result.Count != userIds.Count)
        {
            throw new JoinRpgEntityNotFoundException(userIds.Except(result.Select(x => x.UserId)).First(), "user");
        }
        return result;
    }

    async Task<UserInfoHeader> GetRequiredUserInfoHeader(UserIdentification userId)
    {
        return (await GetRequiredUserInfoHeaders([userId])).Single();
    }

    /// <summary>
    /// Отметки «создано/изменено» для доменного агрегата, который хранит только идентификаторы
    /// авторов: заголовки обоих пользователей приезжают одним запросом.
    /// </summary>
    async Task<CreateUpdateMarksInfo> LoadCreateUpdateMarks(
        DateTime createdAt,
        UserIdentification createdById,
        DateTime updatedAt,
        UserIdentification updatedById)
    {
        var users = (await GetRequiredUserInfoHeaders([.. new[] { createdById, updatedById }.Distinct()]))
            .ToDictionary(user => user.UserId);
        return new CreateUpdateMarksInfo(createdAt, users[createdById], updatedAt, users[updatedById]);
    }

    Task<IReadOnlyCollection<UserInfoHeader>> GetAdminUserInfoHeaders();

    Task<UserIdentification?> FindByVk(string vkId);
    Task<UserIdentification?> FindByTelegram(string telegramUsername);
    Task<UserIdentification?> FindByEmail(string email);
}
