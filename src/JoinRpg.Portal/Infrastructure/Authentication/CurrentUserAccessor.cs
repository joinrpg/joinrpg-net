using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.PrimitiveTypes;
#nullable enable

namespace JoinRpg.Web.Helpers;

/// <summary>
/// Adapter to extract user data from HttpContext principal
/// </summary>
public class CurrentUserAccessor : ICurrentUserAccessor
{
    private IHttpContextAccessor HttpContextAccessor { get; }

    private ClaimsPrincipal User => HttpContextAccessor.HttpContext?.User
        ?? throw new Exception("Should be inside http request");

    /// <summary>
    /// ctor
    /// </summary>
    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) => HttpContextAccessor = httpContextAccessor;

    int ICurrentUserAccessor.UserId => User.GetUserIdOrDefault() ?? throw new Exception("Authorization required here");

    int? ICurrentUserAccessor.UserIdOrDefault => User.GetUserIdOrDefault();

    string ICurrentUserAccessor.DisplayName => User.FindFirst(JoinClaimTypes.DisplayName)!.Value;

    string ICurrentUserAccessor.Email => User.FindFirst(ClaimTypes.Email)!.Value;

    bool ICurrentUserAccessor.IsAdmin => User.IsInRole(DataModel.Security.AdminRoleName);

    AvatarIdentification? ICurrentUserAccessor.Avatar
        => AvatarIdentification.FromOptional(
            int.TryParse(User.FindFirstValue(JoinClaimTypes.AvatarId), out var avatarId) ? avatarId : null
        );
}
