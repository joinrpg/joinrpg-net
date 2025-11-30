using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Interfaces;

/// <summary>
/// Used to get information of current user.
/// </summary>
public interface ICurrentUserAccessor
{
    int? UserIdOrDefault { get; }
    UserDisplayName DisplayName { get; }
    string Email { get; }
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
