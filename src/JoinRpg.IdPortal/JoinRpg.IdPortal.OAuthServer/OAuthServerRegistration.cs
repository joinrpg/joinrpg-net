using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Joinrpg.Web.Identity;
using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.OAuthServer;

public static class OAuthServerRegistration
{
    private const string OpenIddictPolicy = "OpenIddictPolicy";

    public static WebApplicationBuilder AddJoinOAuthServer(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<OAuthServerOptions>();

        var oauthOptions = builder.Configuration.GetSection("OAuthServer").Get<OAuthServerOptions>();
        var certOptions = oauthOptions?.Certificates;

        builder.Services.AddJoinEfCoreDbContext<IdPortalDbContext>(builder.Configuration, builder.Environment, "IdPortal",
            options =>
        {
            // Register the entity sets needed by OpenIddict.
            options.UseOpenIddict();
        });

        builder.Services.AddOpenIddict()

            // Register the OpenIddict core components.
            .AddCore(options =>
            {
                // Configure OpenIddict to use the Entity Framework Core stores and models.
                options.UseEntityFrameworkCore()
                       .UseDbContext<IdPortalDbContext>();
            }

            );

        builder.Services.AddOpenIddict()

            // Register the OpenIddict server components.
            .AddServer(options =>
            {
                options
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token") // Внутри фреймворка
                    .SetUserInfoEndpointUris("connect/user_info")
                    ;


                options.AllowAuthorizationCodeFlow();

                options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Phone, Scopes.Profile);

                if (certOptions?.Signing?.Base64 is { } signingBase64)
                {
                    options.AddSigningCertificate(X509CertificateLoader.LoadPkcs12(
                        Convert.FromBase64String(signingBase64),
                        certOptions.Signing.Password));
                }
                else
                {
                    options.AddDevelopmentSigningCertificate();
                }

                if (certOptions?.Encryption?.Base64 is { } encryptionBase64)
                {
                    options.AddEncryptionCertificate(X509CertificateLoader.LoadPkcs12(
                        Convert.FromBase64String(encryptionBase64),
                        certOptions.Encryption.Password));
                }
                else
                {
                    options.AddDevelopmentEncryptionCertificate();
                }

                // Register the ASP.NET Core host and configure the ASP.NET Core options.
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough();
            })
                .AddValidation(options =>
                {
                    // Import the configuration from the local OpenIddict server instance.
                    options.UseLocalServer();

                    // Register the ASP.NET Core host.
                    options.UseAspNetCore();
                });

        builder.Services.AddHostedService<OAuthRegistrator>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(OpenIddictPolicy, policy =>
            {
                policy.AddAuthenticationSchemes(
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

                policy.RequireAuthenticatedUser();
            });
        });

        return builder;
    }

    public static WebApplication MapJoinOAuthServer(this WebApplication app)
    {
        app.MapMethods("connect/authorize", [HttpMethods.Get, HttpMethods.Post], AuthorizeMethod);
        app.MapGet("connect/user_info", GetUserInfoMethod)
            .RequireAuthorization(OpenIddictPolicy);
        return app;
    }

    internal async static Task<Results<ForbidHttpResult, Ok<Dictionary<string, object?>>>> GetUserInfoMethod(
        ClaimsPrincipal user,
        IUserRepository userRepository,
        IOptions<JoinRpgHostNamesOptions> hostNameOptions,
        IAvatarLoader avatarLoader
        )
    {
        if (!UserIdentification.TryParse(user.FindFirst(Claims.Subject)?.Value, provider: null, out var id))
        {
            return TypedResults.Forbid();
        }

        var userInfo = await userRepository.GetRequiredUserInfo(id);
        var claims = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [Claims.Subject] = id.ToString(),
            [Claims.PreferredUsername] = userInfo.DisplayName.DisplayName,
            [Claims.Profile] = $"https://{hostNameOptions.Value.IdHost}/user/{id.Value}",
        };

        if (user.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = userInfo.Email.Value;
            claims[Claims.EmailVerified] = userInfo.EmailConfirmed;
        }

        if (user.HasScope(Scopes.Phone))
        {
            claims[Claims.PhoneNumber] = userInfo.PhoneNumber;
            claims[Claims.PhoneNumberVerified] = userInfo.PhoneNumberConfirmed;
        }

        if (user.HasScope(Scopes.Profile))
        {
            claims[Claims.Name] = userInfo.UserFullName.FullName;
            claims[Claims.GivenName] = userInfo.UserFullName.BornName?.Value;
            claims[Claims.FamilyName] = userInfo.UserFullName.SurName?.Value;
            claims[Claims.MiddleName] = userInfo.UserFullName.FatherName?.Value;

            if (userInfo.SelectedAvatarId is not null)
            {
                var avatarInfo = await avatarLoader.GetAvatar(userInfo.SelectedAvatarId, 64);
                claims[Claims.Picture] = avatarInfo.Uri;
            }
        }

        return TypedResults.Ok(claims);
    }

    private async static Task<Results<ChallengeHttpResult, SignInHttpResult>> AuthorizeMethod(HttpContext context, IOpenIddictApplicationManager applicationManager, ICurrentUserAccessor currentUserAccessor)
    {

        var principal = (await context.AuthenticateAsync())?.Principal;
        if (principal is not { Identity.IsAuthenticated: true })
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = context.Request.GetEncodedUrl()
            };

            return TypedResults.Challenge(properties);
        }

        var request = context.GetOpenIddictServerRequest();
        if (request?.IsAuthorizationCodeFlow() == true && request.ClientId is not null)
        {
            // Note: the client credentials are automatically validated by OpenIddict:
            // if client_id or client_secret are invalid, this action won't be invoked.

            var application = await applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("The application cannot be found.");

            // Create a new ClaimsIdentity containing the claims that
            // will be used to create an id_token, a token or a code.
            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

            identity.SetClaim(Claims.Subject, currentUserAccessor.UserIdentification.ToString());

            identity.SetScopes(request.GetScopes());

            identity.SetDestinations(static claim => claim.Type switch
            {
                Claims.Subject
                    => [Destinations.AccessToken, Destinations.IdentityToken],
                Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                    => [Destinations.AccessToken, Destinations.IdentityToken],
                _ => [Destinations.AccessToken]
            });

            return TypedResults.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        else { throw new NotImplementedException("The specified grant is not implemented."); }
    }
}
