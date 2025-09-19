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
                if (string.IsNullOrWhiteSpace(options.Value.BotName))
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

internal class HealthCheckTelegram(TelegramBotClient client) : IHealthCheck
{

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var x = await client.GetMe(cancellationToken);
        return new HealthCheckResult(HealthStatus.Healthy, description: "Подключен " + x.Username ?? "нет имени");
    }
}
