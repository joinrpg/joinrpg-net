using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Joinrpg.Web.Identity;
public static class IdentityConfigurator
{
    public static IServiceCollection AddJoinIdentity(this IServiceCollection services)
    {

        _ = services
            .AddIdentity<JoinIdentityUser, string>(options =>
            {
                options.Password.ConfigureValidation();
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddDefaultTokenProviders()
            .AddUserStore<MyUserStore>()
            .AddRoleStore<MyUserStore>();

        services.AddOptions<JoinRpgHostNamesOptions>();

        return services
            .AddTransient<ICustomLoginStore, MyUserStore>()
            .AddTransient<IAccountEmailService<JoinIdentityUser>, AccountServiceEmailImpl>()
            .AddScoped<JoinUserManager>()

            .AddHttpContextAccessor()
            .AddScoped<ICurrentUserAccessor, CurrentUserAccessor>()
            .AddScoped<ICurrentUserSetAccessor, CurrentUserAccessor>()

            .AddScoped(typeof(PerRequestCache<,>))
            .AddSingleton(typeof(SingletonCache<,>))
            .AddTransient<IAvatarLoader, AvatarCachedLoader>();
    }

    private static void ConfigureValidation(this PasswordOptions password)
    {
        password.RequiredLength = 8;
        password.RequireLowercase = false;
        password.RequireUppercase = false;
        password.RequireNonAlphanumeric = false;
        password.RequireDigit = false;
    }

    public static IServiceCollection AddJoinExternalLogins(this IServiceCollection services, IConfigurationSection configSection)
    {
        var authBuilder = services.AddAuthentication();

        var vkConfig = configSection.GetSection("Vkontakte").Get<OAuthAuthenticationOptions>();

        if (vkConfig is not null)
        {
            _ = authBuilder.AddVkontakte(options =>
            {
                options.Scope.Add("email");

                SetCommonProperties(options, vkConfig);
            });
        }

        return services;

        static void SetCommonProperties(OAuthOptions options, OAuthAuthenticationOptions config)
        {
            options.SignInScheme = IdentityConstants.ExternalScheme;

            (options.ClientId, options.ClientSecret) = config;
        }
    }
}
