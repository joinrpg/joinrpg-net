using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes.Users;

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
}
