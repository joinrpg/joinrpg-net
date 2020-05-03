using System.Security.Claims;

namespace JoinRpg.Portal.Infrastructure.Authentication
{
    public static class IdentityUserExtensions
    {
        public static int? GetUserIdOrDefault(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated)
            {
                return null;
            }
            var userIdString = user.FindFirst(ClaimTypes.NameIdentifier).Value;
            return int.TryParse(userIdString, out var i) ? (int?)i : null;
        }
    }
}
