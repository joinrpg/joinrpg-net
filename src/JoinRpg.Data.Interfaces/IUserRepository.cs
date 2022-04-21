using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Data.Interfaces;

public interface IUserRepository
{
    Task<User> GetById(int id);

    Task<User> WithProfile(int userId);
    Task<User> GetWithSubscribe(int currentUserId);
    [ItemCanBeNull]
    Task<User> GetByEmail(string email);
    Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId);
}
