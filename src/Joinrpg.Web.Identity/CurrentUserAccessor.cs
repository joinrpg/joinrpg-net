using System.Security.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using Microsoft.AspNetCore.Http;

namespace Joinrpg.Web.Identity;

/// <summary>
/// Adapter to extract user data from HttpContext principal
/// </summary>
public class CurrentUserAccessor : ICurrentUserAccessor, ICurrentUserSetAccessor
{
    private class CurrentUserFromHttpContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
    {
        private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? throw new Exception("Should be inside http request");

        int? ICurrentUserAccessor.UserIdOrDefault => GetUserIdOrDefault(User);

        string ICurrentUserAccessor.DisplayName => User.FindFirst(JoinClaimTypes.DisplayName)!.Value;

        string ICurrentUserAccessor.Email => User.FindFirst(ClaimTypes.Email)!.Value;

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

    private class CurrentUserFromDomainUser(User user) : ICurrentUserAccessor
    {
        public int? UserIdOrDefault { get; } = user.UserId;

        public string DisplayName { get; } = user.GetDisplayName();

        public string Email { get; } = user.Email;

        public bool IsAdmin { get; } = user.Auth.IsAdmin;

        public AvatarIdentification? Avatar { get; } = AvatarIdentification.FromOptional(user.SelectedAvatarId);
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

    public string DisplayName => Current.DisplayName;

    public string Email => Current.Email;

    public bool IsAdmin => Current.IsAdmin;

    public AvatarIdentification? Avatar => Current.Avatar;

    public void StartImpersonate(User user) => stack.Push(new CurrentUserFromDomainUser(user));
    public void StopImpersonate() => stack.Pop();
}

public interface ICurrentUserSetAccessor
{
    void StartImpersonate(User user);
    void StopImpersonate();
}
