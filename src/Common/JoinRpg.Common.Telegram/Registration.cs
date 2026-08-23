using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace JoinRpg.Common.Telegram;

public static class Registration
{
    public static IServiceCollection AddJoinTelegram(this IServiceCollection services)
    {
        _ = services
            .AddTransient<TelegramNotificationServiceImpl>()
            .AddTransient<StubTelegramNotificationService>()
            .AddTransient<ITelegramNotificationService>(services =>
            {
                var options = services.GetRequiredService<IOptions<TelegramLoginOptions>>();
                if (!options.Value.Enabled)
                {
                    return services.GetRequiredService<StubTelegramNotificationService>();
                }
                return services.GetRequiredService<TelegramNotificationServiceImpl>();
            })
            .AddSingleton(services =>
            {
                var options = services.GetRequiredService<IOptions<TelegramLoginOptions>>();
                return new TelegramBotClient($"{options.Value.BotId}:{options.Value.BotSecret}");
            });


        _ = services
            .AddHealthChecks()
            .AddCheck<HealthCheckTelegram>("Telegram client");

        return services;
    }
}

internal class HealthCheckTelegram(ITelegramNotificationService service) : IHealthCheck
{

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var username = await service.GetMyUserName(cancellationToken);
        if (username is null)
        {
            return new HealthCheckResult(HealthStatus.Degraded, "Telegram выключен");
        }
        return new HealthCheckResult(HealthStatus.Healthy, description: "Подключен " + username);
    }
}
