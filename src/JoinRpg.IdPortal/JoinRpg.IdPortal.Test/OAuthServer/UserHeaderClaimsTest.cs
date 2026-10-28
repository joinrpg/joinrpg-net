using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.IdPortal.OAuthServer;

namespace JoinRpg.IdPortal.Test.OAuthServer;

/// <summary>
/// Имя и аватарка в access-токене MCP (ADR026 §1): без них в Portal под MCP-токеном падают
/// <c>ICurrentUserAccessor.DisplayName</c> и <c>ToUserInfoHeader()</c>.
/// </summary>
public class UserHeaderClaimsTest
{
    private static ClaimsPrincipal CookiePrincipal(params Claim[] extra) => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "3"),
            new Claim(ClaimTypes.Email, "user@example.com"),
            .. extra,
        ],
        authenticationType: "Identity.Application"));

    [Fact]
    public void CopiesHeaderClaimsFromCookie()
    {
        var identity = new ClaimsIdentity();

        OAuthServerRegistration.AddUserHeaderClaims(identity, CookiePrincipal(
            new Claim(JoinClaimTypes.DisplayName, "Ник"),
            new Claim(JoinClaimTypes.FullName, "Иван Иванов"),
            new Claim(JoinClaimTypes.AvatarId, "42")));

        identity.FindFirst(JoinClaimTypes.DisplayName)?.Value.ShouldBe("Ник");
        identity.FindFirst(JoinClaimTypes.FullName)?.Value.ShouldBe("Иван Иванов");
        identity.FindFirst(JoinClaimTypes.AvatarId)?.Value.ShouldBe("42");
    }

    [Fact]
    public void DoesNotCopyEmailOrMissingClaims()
    {
        var identity = new ClaimsIdentity();

        OAuthServerRegistration.AddUserHeaderClaims(identity, CookiePrincipal(
            new Claim(JoinClaimTypes.DisplayName, "Ник")));

        identity.Claims.Select(c => c.Type).ShouldBe([JoinClaimTypes.DisplayName]);
    }
}
