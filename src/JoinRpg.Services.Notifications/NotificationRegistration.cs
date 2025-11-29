using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Services.Notifications;
public static class NotificationRegistration
{
    public static IServiceCollection AddJoinNotificationLayerServices(this IServiceCollection services)
    {
        return services
            .AddTransient<INotificationService, NotificationServiceImpl>()
            .AddSenderJob<UiSenderJobService>()
            ;
    }

    internal static IServiceCollection AddSenderJob<TSenderJob>(this IServiceCollection serviceCollection)
        where TSenderJob : class, ISenderJob
    {
        return serviceCollection
            .AddScoped<TSenderJob>()
            //.AddScoped<IDailyJob, TJob>()
            .AddHostedService<SenderJobService<TSenderJob>>();
    }
}
