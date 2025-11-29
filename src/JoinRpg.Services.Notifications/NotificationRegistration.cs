using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Notifications;
public static class NotificationRegistration
{
    public static IServiceCollection AddJoinNotificationLayerServices(this IServiceCollection services)
    {
        return services
            .AddTransient<INotificationService, NotificationServiceImpl>()
            ;
    }
}
