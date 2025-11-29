using JoinRpg.Dal.CommonEfCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Dal.Notifications;
public static class Registrations
{
    public static void AddNotificationsDal(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddJoinEfCoreDbContext<NotificationsDataDbContext>(configuration, environment, "Notifications");
        services.AddTransient<INotificationRepository, NotificationsRepository>();
    }
}
