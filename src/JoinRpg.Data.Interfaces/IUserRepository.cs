using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Data.Interfaces;

public interface IUserRepository
{
    Task<User> GetById(int id);

    Task<UserNotificationInfoDto[]> GetUsersNotificationInfo(UserIdentification[] userIds);

    Task<User> WithProfile(int userId);
    Task<User> GetWithSubscribe(int currentUserId);
    Task<User?> GetByEmail(string email);
    Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId);

    Task<(UserIdentification, AvatarIdentification)[]> GetLegacyAvatarsList();
}
