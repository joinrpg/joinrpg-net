using JoinRpg.Services.Notifications.Senders;
using JoinRpg.Services.Notifications.Senders.PostboxEmail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications;

public static class NotificationRegistration
{
    public static IServiceCollection AddJoinNotificationLayerServices(this IServiceCollection services)
    {
        return services
            .AddTransient<INotificationService, NotificationServiceImpl>()
            ;
    }

    public static IServiceCollection AddJoinNotificationJobs(this IServiceCollection services)
    {
        _ = services
            .AddSingleton<IValidateOptions<PostboxOptions>, PostboxOptionsValidator>()
            .AddOptionsWithValidateOnStart<PostboxOptions>();
        return services
            .AddSingleton<PostboxClientFactory>()
            .AddSenderJob<UiSenderJobService>()
            .AddSenderJob<PostboxSenderJobService>()
            .AddSenderJob<TelegramSenderJobService>()
            ;
    }

    internal static IServiceCollection AddSenderJob<TSenderJob>(this IServiceCollection serviceCollection)
        where TSenderJob : class, ISenderJob
    {
        return serviceCollection
            .AddScoped<TSenderJob>()
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
