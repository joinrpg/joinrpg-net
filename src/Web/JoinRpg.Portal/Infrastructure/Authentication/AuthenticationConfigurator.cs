using System.Text;
using Joinrpg.Web.Identity;
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

        _ = services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

        _ = services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromSeconds(3)); // Проверять, изменились ли роли мастера каждые 3 секунды

        _ = services.ConfigureApplicationCookie(SetCookieOptions());

        _ = services.AddAuthorization(o =>
          {
              o.DefaultPolicy = new AuthorizationPolicyBuilder(
                  JwtBearerDefaults.AuthenticationScheme,
                  IdentityConstants.ApplicationScheme)
                  .RequireAuthenticatedUser()
                  .Build();
              o.AddPolicy("XApiUser", policy => policy
                  .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                  .RequireAuthenticatedUser());
          })
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
        options.Events.OnRedirectToLogin = context => OnCookieRedirect(context, StatusCodes.Status401Unauthorized);
        options.Events.OnRedirectToAccessDenied = context => OnCookieRedirect(context, StatusCodes.Status403Forbidden);
    };

    private static Task OnCookieRedirect(RedirectContext<CookieAuthenticationOptions> context, int apiStatusCode)
    {
        // Острова Blazor ходят в /webapi через fetch, а он молча идёт по редиректу и получает 200 со страницей входа
        // или «нет доступа» — отказ выглядел бы успехом. Поэтому API отвечаем кодом. Внешние API (x-api) — на JWT, не сюда.
        if (context.Request.Path.IsInternalApiPath())
        {
            context.Response.StatusCode = apiStatusCode;
            return Task.CompletedTask;
        }
        if (context.HttpContext.Items.TryGetValue(DiscoverFilters.Constants.ProjectIdName, out var projectIdObj) && projectIdObj is int projectId)
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
