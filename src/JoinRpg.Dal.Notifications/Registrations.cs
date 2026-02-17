using JoinRpg.Common.WebInfrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Dal.Notifications;

public static class Registrations
{
    public static IServiceCollection AddNotificationsDal(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        _ = services.AddJoinEfCoreDbContext<NotificationsDataDbContext>(configuration, environment, "Notifications");
        return services.AddTransient<INotificationRepository, NotificationsRepository>();
    }
}
