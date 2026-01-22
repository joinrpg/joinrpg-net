using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Email;

public static class NotificationRegistration
{
    public static IServiceCollection AddJoinNotificationServices(this IServiceCollection services)
    {
        return services
            .AddTransient<IEmailService, EmailServiceImpl>()
            .AddTransient<IAdminNotificationService, AdminNotificationServiceImpl>()
            ;
    }
}
