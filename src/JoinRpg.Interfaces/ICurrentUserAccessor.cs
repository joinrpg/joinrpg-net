using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Interfaces;

/// <summary>
/// Used to get information of current user.
/// </summary>
public interface ICurrentUserAccessor
{
    int? UserIdOrDefault { get; }
    int UserId { get; }
    string DisplayName { get; }
    string Email { get; }
    bool IsAdmin { get; }
    /// <summary>
    /// Avatar of current user
    /// </summary>
    AvatarIdentification? Avatar { get; }
}
