using System.Collections.Immutable;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Joinrpg.Web.Identity;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;
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


                options.AllowAuthorizationCodeFlow()
                       .RequireProofKeyForCodeExchange();

                options.AllowRefreshTokenFlow();
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(90));

                options.RegisterScopes(Scopes.OpenId, Scopes.Email, Scopes.Phone, Scopes.Profile, Scopes.OfflineAccess,
                    JoinRpgScopes.Read, JoinRpgScopes.CharactersWrite);

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

        builder.Services.AddScoped<IOAuthClientService, OAuthClientService>();

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

    private async static Task<Results<ChallengeHttpResult, RedirectHttpResult, SignInHttpResult>> AuthorizeMethod(
        HttpContext context,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        ICurrentUserAccessor currentUserAccessor)
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
        if (request?.IsAuthorizationCodeFlow() != true || request.ClientId is null)
        {
            throw new NotImplementedException("The specified grant is not implemented.");
        }

        // Note: the client credentials are automatically validated by OpenIddict:
        // if client_id or client_secret are invalid, this action won't be invoked.
        var application = await applicationManager.FindByClientIdAsync(request.ClientId) ??
            throw new InvalidOperationException("The application cannot be found.");
        var applicationId = await applicationManager.GetIdAsync(application) ??
            throw new InvalidOperationException("The application has no id.");

        var subject = currentUserAccessor.UserIdentification.ToString();
        var requestedScopes = request.GetScopes();

        // Only joinrpg.* (MCP) scopes go through explicit consent (ADR012 §4) — the site's own
        // OIDC login (openid/email/profile/...) keeps behaving exactly as before this change.
        var requiresConsent = requestedScopes.Any(JoinRpgScopes.IsJoinRpgScope);

        IReadOnlyList<int> grantedProjectIds = [];
        if (requiresConsent)
        {
            if (context.Request.Query[OAuthConsent.ConsentParameter] == OAuthConsent.Denied)
            {
                return TypedResults.Redirect(BuildAccessDeniedRedirectUri(request));
            }

            if (context.Request.Query[OAuthConsent.ConsentParameter] == OAuthConsent.Granted)
            {
                // The user just came back from the consent page: persist the decision as a
                // permanent authorization, so subsequent sign-ins skip the consent screen.
                grantedProjectIds = OAuthConsent.ParseProjectIds(context.Request.Query[OAuthConsent.ProjectsParameter]);

                var descriptor = new OpenIddictAuthorizationDescriptor
                {
                    ApplicationId = applicationId,
                    Subject = subject,
                    Type = AuthorizationTypes.Permanent,
                    Status = Statuses.Valid,
                };
                foreach (var scope in requestedScopes)
                {
                    descriptor.Scopes.Add(scope);
                }
                StoreGrantedProjects(descriptor.Properties, grantedProjectIds);

                _ = await authorizationManager.CreateAsync(descriptor);
            }
            else
            {
                var existingAuthorization = await FindMatchingAuthorizationAsync(
                    authorizationManager, subject, applicationId, requestedScopes);

                if (existingAuthorization is null)
                {
                    // No consent on file yet for this client/scope combination — ask the user.
                    return TypedResults.Redirect($"/oauth/consent{context.Request.QueryString}");
                }

                grantedProjectIds = ReadGrantedProjects(await authorizationManager.GetPropertiesAsync(existingAuthorization));
            }
        }

        // Create a new ClaimsIdentity containing the claims that
        // will be used to create an id_token, a token or a code.
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);

        identity.SetClaim(Claims.Subject, subject);
        identity.SetScopes(requestedScopes);

        if (grantedProjectIds.Count > 0)
        {
            identity.SetClaim(OAuthConsent.ProjectsClaimType, OAuthConsent.FormatProjectIds(grantedProjectIds));
        }

        identity.SetDestinations(static claim => claim.Type switch
        {
            Claims.Subject
                => [Destinations.AccessToken, Destinations.IdentityToken],
            Claims.Name when claim.Subject.HasScope(Scopes.Profile)
                => [Destinations.AccessToken, Destinations.IdentityToken],
            OAuthConsent.ProjectsClaimType
                => [Destinations.AccessToken],
            _ => [Destinations.AccessToken]
        });

        return TypedResults.SignIn(new ClaimsPrincipal(identity), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static string BuildAccessDeniedRedirectUri(OpenIddictRequest request)
    {
        var redirectUri = request.RedirectUri ?? throw new InvalidOperationException("The authorization request has no redirect_uri.");
        var parameters = new Dictionary<string, string?> { ["error"] = Errors.AccessDenied };
        if (request.State is { } state)
        {
            parameters["state"] = state;
        }
        return QueryHelpers.AddQueryString(redirectUri, parameters);
    }

    private async static Task<object?> FindMatchingAuthorizationAsync(
        IOpenIddictAuthorizationManager authorizationManager,
        string subject,
        string applicationId,
        ImmutableArray<string> requestedScopes)
    {
        await foreach (var authorization in authorizationManager.FindAsync(
            subject: subject,
            client: applicationId,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: null))
        {
            var grantedScopes = await authorizationManager.GetScopesAsync(authorization);
            if (requestedScopes.All(grantedScopes.Contains))
            {
                return authorization;
            }
        }

        return null;
    }

    private static void StoreGrantedProjects(IDictionary<string, JsonElement> properties, IReadOnlyList<int> projectIds)
    {
        if (projectIds.Count > 0)
        {
            properties[OAuthConsent.ProjectsClaimType] = JsonSerializer.SerializeToElement(projectIds);
        }
    }

    private static IReadOnlyList<int> ReadGrantedProjects(ImmutableDictionary<string, JsonElement> properties)
        => properties.TryGetValue(OAuthConsent.ProjectsClaimType, out var element)
            ? element.Deserialize<int[]>() ?? []
            : [];
}
