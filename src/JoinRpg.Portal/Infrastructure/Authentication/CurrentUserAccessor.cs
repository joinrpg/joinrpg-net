using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Helpers;

/// <summary>
/// Adapter to extract user data from HttpContext principal
/// </summary>
public class CurrentUserAccessor : ICurrentUserAccessor, ICurrentUserSetAccessor
{
    private class CurrentUserFromHttpContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
    {
        private ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ?? throw new Exception("Should be inside http request");

        int? ICurrentUserAccessor.UserIdOrDefault => User.GetUserIdOrDefault();

        string ICurrentUserAccessor.DisplayName => User.FindFirst(JoinClaimTypes.DisplayName)!.Value;

        string ICurrentUserAccessor.Email => User.FindFirst(ClaimTypes.Email)!.Value;

        bool ICurrentUserAccessor.IsAdmin => User.IsInRole(Security.AdminRoleName);

        AvatarIdentification? ICurrentUserAccessor.Avatar
            => AvatarIdentification.FromOptional(
                int.TryParse(User.FindFirstValue(JoinClaimTypes.AvatarId), out var avatarId) ? avatarId : null
            );
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

internal interface ICurrentUserSetAccessor
{
    public void StartImpersonate(User user);
    public void StopImpersonate();
}
