using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.Common.WebInfrastructure.Auth;

public static class AuthRegistration
{
    /// <summary>
    /// Регистрирует cookie-схему аутентификации и OpenIddict-клиент для логина через id.joinrpg.ru.
    /// Состояние OpenIddict (authorization/token records) хранится в <typeparamref name="TContext"/> —
    /// вызывающая сторона должна вызвать <c>modelBuilder.UseOpenIddict()</c> в <c>OnModelCreating</c> этого контекста.
    /// Реализацию <see cref="IJoinUserLoginHandler"/> для приложение-специфичной части логина
    /// нужно зарегистрировать отдельно.
    /// </summary>
    public static void AddJoinRpgAuthentication<TContext>(this IServiceCollection services, IConfiguration configuration)
        where TContext : DbContext
    {
        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
            });

        var oidcOptions = configuration.GetSection("JoinRpgOidc").Get<JoinRpgOidcOptions>()
            ?? throw new InvalidOperationException("JoinRpgOidc configuration section is required");

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<TContext>();
            })
            .AddClient(options =>
            {
                options.AllowAuthorizationCodeFlow();

                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                    .EnableRedirectionEndpointPassthrough();

                options.UseSystemNetHttp();

                options.AddRegistration(new OpenIddict.Client.OpenIddictClientRegistration
                {
                    Issuer = oidcOptions.Issuer,
                    ClientId = oidcOptions.ClientId,
                    ClientSecret = oidcOptions.ClientSecret,
                    Scopes = { Scopes.OpenId, Scopes.Profile },
                    RedirectUri = new Uri("/signin-joinrpg", UriKind.Relative),
                });
            });

        services.AddAuthorization();
    }
}
