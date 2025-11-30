using JoinRpg.Services.Notifications.Senders;
using JoinRpg.Services.Notifications.Senders.PostboxEmail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications;
public static class NotificationRegistration
{
    public static IServiceCollection AddJoinNotificationLayerServices(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<PostboxOptions>();
        return services
            .AddTransient<INotificationService, NotificationServiceImpl>()
            .AddSingleton<PostboxClientFactory>()
            .AddSenderJob<UiSenderJobService>()
            .AddSenderJob<PostboxSenderJobService>()
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

    private class PostboxOptionsValidator : IValidateOptions<PostboxOptions>
    {
        ValidateOptionsResult IValidateOptions<PostboxOptions>.Validate(string? name, PostboxOptions options)
        {
            if (!options.Enabled)
            {
                return ValidateOptionsResult.Success;
            }

            return new DataAnnotationValidateOptions<PostboxOptions>(null).Validate(name, options);
        }
    }
}
