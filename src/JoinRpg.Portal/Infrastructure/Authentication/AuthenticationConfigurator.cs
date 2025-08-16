using System.Text;
using Joinrpg.Web.Identity;
using JoinRpg.Portal.Infrastructure.Authentication.Telegram;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace JoinRpg.Portal.Infrastructure.Authentication;

internal static class AuthenticationConfigurator
{
    public static IServiceCollection AddJoinAuth(this IServiceCollection services,
        JwtSecretOptions jwtSecretOptions,
        IWebHostEnvironment environment,
        IConfigurationSection authSection)
    {

        _ = services.AddJoinIdentity();

        _ = services
            .AddTransient<TelegramLoginValidator>();

        _ = services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

        _ = services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromSeconds(3)); // Проверять, изменились ли роли мастера каждые 3 секунды

        _ = services.ConfigureApplicationCookie(SetCookieOptions());

        _ = services.AddAuthorization(o => o.DefaultPolicy = new AuthorizationPolicyBuilder(
            JwtBearerDefaults.AuthenticationScheme,
            IdentityConstants.ApplicationScheme
            )
            .RequireAuthenticatedUser()
          .Build())
            .AddTransient<IAuthorizationPolicyProvider, AuthPolicyProvider>()
            .AddAuthentication()
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = !environment.IsDevelopment();
                o.SaveToken = false;
                o.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretOptions.SecretKey));
                o.TokenValidationParameters.ValidAudience = "ApiUser";
                o.TokenValidationParameters.ValidIssuer = jwtSecretOptions.Issuer;
            });

        return services.AddJoinExternalLogins(authSection);
    }



    public static Action<CookieAuthenticationOptions> SetCookieOptions() => options =>
    {
        options.Events.OnRedirectToAccessDenied =
           options.Events.OnRedirectToLogin = OnCookieRedirect;
    };

    private static Task OnCookieRedirect(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.Value?.IsApiPath() == true)
        {
            context.Response.StatusCode = 401;
        }
        else if (context.HttpContext.Items.TryGetValue(DiscoverFilters.Constants.ProjectIdName, out var projectIdObj) && projectIdObj is int projectId)
        {
            context.Response.Redirect($"{context.RedirectUri}&projectId={projectId}");
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);

        }
        return Task.CompletedTask;
    }
}
