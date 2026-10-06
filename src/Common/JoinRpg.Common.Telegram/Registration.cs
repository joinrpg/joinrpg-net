using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace JoinRpg.Common.Telegram;

public static class Registration
{
    public static IServiceCollection AddJoinTelegram(this IServiceCollection services)
    {
        _ = services
            .AddTransient<TelegramLoginValidator>()
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
            .AddTransient<ITelegramNotificationServiceFactory, DefaultOnlyTelegramNotificationServiceFactory>()
            .AddSingleton(services =>
            {
                var options = services.GetRequiredService<IOptions<TelegramLoginOptions>>().Value;
                var httpClient = TelegramHttpClientFactory.Create(options.Proxy);
                return new TelegramBotClient($"{options.BotId}:{options.BotSecret}", httpClient);
            });


        _ = services
            .AddHealthChecks()
            .AddCheck<HealthCheckTelegram>("Telegram client");

        return services;
    }
}

internal class HealthCheckTelegram(
    IOptions<TelegramLoginOptions> options,
    IServiceProvider serviceProvider,
    ILogger<HealthCheckTelegram> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var telegramOptions = options.Value;
        if (!telegramOptions.Enabled)
        {
            return HealthCheckResult.Degraded("Telegram выключен");
        }
        if (telegramOptions.BotId is null || string.IsNullOrWhiteSpace(telegramOptions.BotSecret))
        {
            return HealthCheckResult.Degraded("Telegram не настроен: нет BotId или BotSecret");
        }

        try
        {
            // Сервис достаём здесь, а не через конструктор: TelegramBotClient падает уже при
            // создании, если токен кривой, и это тоже должно стать диагнозом чека.
            var service = serviceProvider.GetRequiredService<ITelegramNotificationService>();
            var username = await service.GetMyUserName(cancellationToken);
            return HealthCheckResult.Healthy("Подключен " + username);
        }
        catch (OperationCanceledException)
        {
            // Отмена — это сработавший таймаут чека или ушедший клиент. Пусть движок
            // health-чеков отчитается про таймаут, а не мы про «сломанный Telegram».
            throw;
        }
        catch (ApiRequestException exception)
        {
            logger.LogError(exception, "Telegram API вернул ошибку при проверке здоровья. Error code={telegramErrorCode}", exception.ErrorCode);
            return HealthCheckResult.Unhealthy($"Telegram API вернул ошибку {exception.ErrorCode}: {exception.Message}", exception);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Ошибка при обращении к Telegram API при проверке здоровья");
            return HealthCheckResult.Unhealthy("Ошибка при обращении к Telegram API: " + exception.Message, exception);
        }
    }
}
