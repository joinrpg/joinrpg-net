using System.Security.Claims;
using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using Microsoft.AspNetCore.Http;

namespace Joinrpg.Web.Identity;

/// <summary>
/// Adapter to extract user data from HttpContext principal
/// </summary>
public class CurrentUserAccessor : ICurrentUserAccessor, IImpersonateAccessor
{
    private class CurrentUserFromHttpContext : ICurrentUserAccessor
    {
        private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? throw new Exception("Should be inside http request");

        private readonly Lazy<UserDisplayName> DisplayName;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CurrentUserFromHttpContext(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
            DisplayName = new Lazy<UserDisplayName>(() => new UserDisplayName(User.FindFirst(JoinClaimTypes.DisplayName)!.Value, User.FindFirst(JoinClaimTypes.FullName)?.Value));
        }

        int? ICurrentUserAccessor.UserIdOrDefault => GetUserIdOrDefault(User);

        UserDisplayName ICurrentUserAccessor.DisplayName => DisplayName.Value;

        bool ICurrentUserAccessor.IsAdmin => User.IsInRole(Security.AdminRoleName);

        AvatarIdentification? ICurrentUserAccessor.Avatar
            => AvatarIdentification.FromOptional(
                int.TryParse(User.FindFirstValue(JoinClaimTypes.AvatarId), out var avatarId) ? avatarId : null
            );

        private static int? GetUserIdOrDefault(ClaimsPrincipal user)
        {
            if (user.Identity?.IsAuthenticated != true)
            {
                return null;
            }
            if (int.TryParse(user.FindFirstValue("uid"), out var uid))  // JWT tokens
            {
                return uid;
            }
            if (int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id))  // Cookie
            {
                return id;
            }
            return null;
        }
    }

    private class ImpersonatedUser(UserIdentification userId, UserDisplayName displayName) : ICurrentUserAccessor
    {
        public int? UserIdOrDefault { get; } = userId.Value;

        public UserDisplayName DisplayName { get; } = displayName;

        public bool IsAdmin { get; } = false;

        public AvatarIdentification? Avatar { get; } = null;
    }

    /// <summary>
    /// ctor
    /// </summary>
    public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        stack.Push(new CurrentUserFromHttpContext(httpContextAccessor));
    }

    private ICurrentUserAccessor Current => stack.Peek();
    private Stack<ICurrentUserAccessor> stack = new Stack<ICurrentUserAccessor>();

    public int? UserIdOrDefault => Current.UserIdOrDefault;

    public UserDisplayName DisplayName => Current.DisplayName;

    public bool IsAdmin => Current.IsAdmin;

    public AvatarIdentification? Avatar => Current.Avatar;
    public void StopImpersonate() => stack.Pop();
    void IImpersonateAccessor.StartImpersonate(UserIdentification userId, UserDisplayName displayName) => stack.Push(new ImpersonatedUser(userId, displayName));
}
