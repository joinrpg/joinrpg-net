using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Interfaces;

/// <summary>
/// Used to get information of current user.
/// </summary>
public interface ICurrentUserAccessor
{
    int? UserIdOrDefault { get; }
    string DisplayName { get; }
    string Email { get; }
    bool IsAdmin { get; }
    /// <summary>
    /// Avatar of current user
    /// </summary>
    AvatarIdentification? Avatar { get; }

    UserIdentification UserIdentification => new UserIdentification(UserId);

    int UserId => UserIdOrDefault ?? throw new Exception("Authorization required here");
}
