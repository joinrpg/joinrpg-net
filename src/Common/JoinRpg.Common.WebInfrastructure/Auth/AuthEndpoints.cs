using System.Security.Claims;
using JoinRpg.Common.PrimitiveTypes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using OpenIddict.Client.AspNetCore;

namespace JoinRpg.Common.WebInfrastructure.Auth;

/// <summary>
/// Общая часть логина через id.joinrpg.ru (OpenIddict): маппинг /login, /signin-joinrpg, /logout.
/// Приложение-специфичное сохранение пользователя и claims — в <see cref="IJoinUserLoginHandler"/>.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/login", (HttpContext context) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/signin-joinrpg",
            };
            return Results.Challenge(properties, [OpenIddictClientAspNetCoreDefaults.AuthenticationScheme]);
        });

        endpoints.MapGet("/signin-joinrpg", async (
            HttpContext context,
            IJoinUserLoginHandler loginHandler,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            var logger = loggerFactory.CreateLogger("Auth");

            var result = await context.AuthenticateAsync(OpenIddictClientAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                logger.LogWarning("OIDC authentication failed: {Error}", result.Failure?.Message);
                return Results.Problem(
                    detail: result.Failure?.Message ?? "Authentication callback failed",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var externalPrincipal = result.Principal!;

            var sub = externalPrincipal.FindFirstValue("sub");
            if (sub is null || !UserIdentification.TryParse(sub, provider: null, out var userId))
            {
                logger.LogWarning("Failed to extract user ID from claims. sub={Sub}, claims=[{Claims}]",
                    sub, string.Join(", ", externalPrincipal.Claims.Select(c => $"{c.Type}={c.Value}")));
                return Results.Problem(
                    detail: $"Could not extract numeric user ID from 'sub' claim (got: {sub})",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            };

            await loginHandler.HandleLoginAsync(userId, externalPrincipal, claims, cancellationToken);

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return Results.Redirect("/");
        });

        endpoints.MapGet("/logout", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });
    }
}
