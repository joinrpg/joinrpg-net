namespace Joinrpg.Web.Identity;

public static class JoinClaimTypes
{
    /// <summary>
    /// Display name that configured in profile
    /// </summary>
    public const string DisplayName = "joinrpg.DisplayName";

    public const string FullName = "joinrpg.FullName";
    /// <summary>
    /// Id of Avatar
    /// </summary>
    public const string AvatarId = "joinrpg.AvatarId";

    /// <summary>
    /// Claims, из которых <see cref="CurrentUserAccessor"/> собирает имя и аватарку текущего
    /// пользователя. Их кладут в cookie, JWT x-api и access-токены MCP (ADR026 §1).
    /// </summary>
    public static IReadOnlyList<string> UserHeaderClaimTypes { get; } = [DisplayName, FullName, AvatarId];
}
