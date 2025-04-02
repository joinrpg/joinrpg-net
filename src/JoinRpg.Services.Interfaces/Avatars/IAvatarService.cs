namespace JoinRpg.Services.Interfaces.Avatars;

/// <summary>
/// Operations with avatars
/// </summary>
public interface IAvatarService
{
    /// <summary>
    /// Ensures that is correct UserAvatar record for user
    /// </summary>
    Task AddGrAvatarIfRequired(int userId);
    /// <summary>
    /// Delete avatar
    /// </summary>
    Task DeleteAvatar(int userId, AvatarIdentification avatarIdentification);

    /// <summary>
    /// Recache avatar
    /// </summary>
    Task RecacheAvatar(UserIdentification userId, AvatarIdentification avatarIdentification);

    /// <summary>
    /// Select avatar for user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="avatarIdentification"></param>
    /// <returns></returns>
    Task SelectAvatar(int userId, AvatarIdentification avatarIdentification);
}
