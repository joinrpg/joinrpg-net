using System.Diagnostics;
using System.Diagnostics.Metrics;
using JoinRpg.Markdown;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace JoinRpg.Common.Telegram;

internal class TelegramNotificationServiceImpl(TelegramBotClient client, ILogger<TelegramNotificationServiceImpl> logger) : ITelegramNotificationService
{
    private const int TelegramMaxMessageLength = 4096;

    private static readonly Meter meter = new("JoinRpg");
    private static readonly Histogram<double> sendDurationHistogram = meter.CreateHistogram<double>("telegram.send_duration_ms", "ms");
    private static readonly Counter<int> sendErrorsCounter = meter.CreateCounter<int>("telegram.send_errors");

    private static void CountError(string errorType) =>
        sendErrorsCounter.Add(1, new KeyValuePair<string, object?>("error_type", errorType));

    public async Task<string?> GetMyUserName(CancellationToken cancellationToken) => (await client.GetMe(cancellationToken)).Username;

    public async Task<SendingResult> SendTelegramNotification(TelegramChatId chatId, TelegramHtmlString contents)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            _ = await client.SendMessage(new ChatId(chatId.Value), contents.SanitizeHtml(TelegramMaxMessageLength), ParseMode.Html, linkPreviewOptions: new LinkPreviewOptions { IsDisabled = true });
            logger.LogInformation("Отправлено сообщение пользователю в телеграм {chatId}", chatId);
            return SendingResult.Success();
        }
        catch (ApiRequestException exception) when (GetPermanentErrorType(exception) is { } errorType)
        {
            CountError(errorType);
            logger.LogWarning("Телеграм-чат {chatId} недоступен для бота, повторять бессмысленно. Error code={telegramErrorCode}, Описание={telegramDescription}",
                chatId,
                exception.ErrorCode,
                exception.Message);
            return SendingResult.PermanentUserFailure();
        }
        catch (ApiRequestException exception)
        {
            CountError("api");
            // Описание отдельным полем, чтобы новый постоянный ответ Телеграма было видно в логах сразу, а не по семи ретраям
            logger.LogWarning(exception, "Ошибка при отправке сообщения в телеграм {chatId}. Error code={telegramErrorCode}, Описание={telegramDescription}, Параметры={telegramResponseParameters}",
                chatId,
                exception.ErrorCode,
                exception.Message,
                exception.Parameters);
            throw;
        }
        catch (Exception exception) when (HasTimeoutInChain(exception))
        {
            CountError("timeout");
            logger.LogWarning(exception, "Таймаут при отправке сообщения в телеграм {chatId}", chatId);
            return SendingResult.CommonFailure();
        }
        catch (Exception exception)
        {
            CountError("other");
            logger.LogWarning(exception, "Ошибка при отправке сообщения в телеграм {chatId}", chatId);
            throw;
        }
        finally
        {
            sw.Stop();
            sendDurationHistogram.Record(sw.Elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Ошибки, которые не исчезнут при повторе: боту закрыт доступ к этому чату. Возвращает тип ошибки для метрики или null.
    /// 403 у Телеграма всегда значит «сюда писать нельзя» (заблокировал бота, удалил аккаунт, выгнал из группы и т. п.),
    /// а среди 400 постоянна только «chat not found» — остальные 400 (например, кривая разметка) к получателю не относятся.
    /// </summary>
    private static string? GetPermanentErrorType(ApiRequestException exception) => exception.ErrorCode switch
    {
        403 when exception.Message.Contains("bot was blocked by the user", StringComparison.OrdinalIgnoreCase) => "blocked",
        403 => "forbidden",
        400 when exception.Message.Contains("chat not found", StringComparison.OrdinalIgnoreCase) => "chat_not_found",
        _ => null,
    };

    private static bool HasTimeoutInChain(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current is TimeoutException)
            {
                return true;
            }
        }
        return false;
    }
}

internal class StubTelegramNotificationService(ILogger<StubTelegramNotificationService> logger) : ITelegramNotificationService
{
    public Task<string?> GetMyUserName(CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    public Task<SendingResult> SendTelegramNotification(TelegramChatId chatId, TelegramHtmlString contents)
    {
        logger.LogInformation("Отправлено сообщение пользователю в телеграм {chatId}: {message}", chatId, contents);
        return Task.FromResult(SendingResult.Success());
    }
}

/// <summary>
/// Пока поддерживает только дефолтного бота приложения — реальный мультибот (свои боты
/// у рекламных каналов/мастеров) не реализован, см. ADR010 §5.
/// </summary>
internal class DefaultOnlyTelegramNotificationServiceFactory(ITelegramNotificationService defaultService) : ITelegramNotificationServiceFactory
{
    public ITelegramNotificationService GetService(string? botKey)
    {
        if (!string.IsNullOrEmpty(botKey))
        {
            throw new NotSupportedException($"Отправка от имени бота '{botKey}' пока не поддерживается (ADR010 §5).");
        }
        return defaultService;
    }
}
