using System.Security.Claims;

namespace JoinRpg.Portal.Infrastructure.Authentication
{
    public static class IdentityUserExtensions
    {
        public static int? GetUserIdOrDefault(this ClaimsPrincipal user)
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
}
