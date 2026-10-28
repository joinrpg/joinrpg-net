using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.Interfaces;

/// <summary>
/// Used to get information of current user.
/// </summary>
public interface ICurrentUserAccessor
{
    int? UserIdOrDefault { get; }
    UserDisplayName DisplayName { get; }
    bool IsAdmin { get; }
    /// <summary>
    /// Avatar of current user
    /// </summary>
    AvatarIdentification? Avatar { get; }

    UserIdentification UserIdentification => UserIdentificationOrDefault ?? throw new Exception("Authorization required here");

    UserIdentification? UserIdentificationOrDefault => UserIdentification.FromOptional(UserIdOrDefault);

    int UserId => UserIdOrDefault ?? throw new Exception("Authorization required here");

    UserInfoHeader ToUserInfoHeader() => new UserInfoHeader(UserIdentification, DisplayName);
}

public interface IImpersonateAccessor
{
    /// <param name="avatar">Аватарка пользователя, если она известна. <c>null</c> — для синтетических
    /// пользователей инструментов, у которых нет ни настоящего имени, ни аватарки.</param>
    void StartImpersonate(UserIdentification userId, UserDisplayName displayName, AvatarIdentification? avatar, bool IsAdmin);
    void StopImpersonate();
}
