using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.DependencyInjection;

namespace Joinrpg.Web.Identity;
public static class IdentityConfigurator
{
    public static IServiceCollection AddJoinIdentity(this IServiceCollection services)
    {

        _ = services
            .AddIdentity<JoinIdentityUser, string>(options => options.Password.ConfigureValidation())
            .AddDefaultTokenProviders()
            .AddUserStore<MyUserStore>()
            .AddRoleStore<MyUserStore>();

        return services
            .AddTransient<ICustomLoginStore, MyUserStore>()
            .AddTransient<IAccountEmailService<JoinIdentityUser>, AccountServiceEmailImpl>()
            .AddScoped<JoinUserManager>()

            .AddHttpContextAccessor()
            .AddScoped<ICurrentUserAccessor, CurrentUserAccessor>()
            .AddScoped<ICurrentUserSetAccessor, CurrentUserAccessor>();
    }

    private static void ConfigureValidation(this PasswordOptions password)
    {
        password.RequiredLength = 8;
        password.RequireLowercase = false;
        password.RequireUppercase = false;
        password.RequireNonAlphanumeric = false;
        password.RequireDigit = false;
    }
}
