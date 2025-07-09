using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;

namespace JoinRpg.Data.Interfaces;

public interface IUserRepository
{
    Task<User> GetById(int id);

    Task<User> WithProfile(int userId);
    Task<User> GetWithSubscribe(int currentUserId);
    Task<User?> GetByEmail(string email);
    Task<UserAvatar> LoadAvatar(AvatarIdentification userAvatarId);
}
