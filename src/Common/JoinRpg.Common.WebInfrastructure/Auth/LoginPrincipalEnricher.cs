using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure.Auth;

internal sealed class LoginPrincipalEnricher(
    UserInfoClient userInfoClient,
    JoinRpgOidcOptions options,
    ILogger<LoginPrincipalEnricher> logger)
{
    public async Task<ClaimsPrincipal> EnrichAsync(
        ClaimsPrincipal principal,
        AuthenticationProperties properties,
        CancellationToken cancellationToken)
    {
        if (!options.FetchUserInfo)
        {
            return principal;
        }

        var token = properties.GetTokenValue("access_token");
        if (token is null)
        {
            logger.LogWarning("OIDC userinfo requested, but no access_token in authentication properties");
            return principal;
        }

        try
        {
            var extra = await userInfoClient.GetUserInfoClaimsAsync(token, cancellationToken);
            if (principal.Identity is not ClaimsIdentity identity)
            {
                return principal;
            }

            var merged = new ClaimsIdentity(
                identity.Claims,
                identity.AuthenticationType,
                identity.NameClaimType,
                identity.RoleClaimType);
            merged.AddClaims(extra);
            return new ClaimsPrincipal(merged);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to fetch userinfo claims, using original principal");
            return principal;
        }
    }
}
