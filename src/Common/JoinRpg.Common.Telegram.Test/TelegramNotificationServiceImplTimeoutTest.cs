using System.Net;
using System.Text;
using JoinRpg.Common.PrimitiveTypes;
using Microsoft.Extensions.Logging.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;

namespace JoinRpg.Common.Telegram.Test;

public class TelegramNotificationServiceImplTimeoutTest
{
    [Fact]
    public async Task SendTelegramNotification_WithBotBlockedResponse_ReturnsPermanentUserFailure()
    {
        // Телеграм на заблокированного бота отвечает не транспортной ошибкой, а обычным HTTP-ответом с ok=false
        var handler = new RespondingHttpMessageHandler(
            HttpStatusCode.Forbidden,
            """{"ok":false,"error_code":403,"description":"Forbidden: bot was blocked by the user"}""");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var botClient = new TelegramBotClient("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", httpClient);
        var service = new TelegramNotificationServiceImpl(botClient, NullLogger<TelegramNotificationServiceImpl>.Instance);

        var result = await service.SendTelegramNotification(new TelegramChatId(1), new TelegramHtmlString("test"));

        // PermanentUserFailure отличается от RepeatableFailure значением Repeatable=false,
        // поэтому эта проверка действительно фиксирует «без ретраев» (см. #4997)
        result.ShouldBe(SendingResult.PermanentUserFailure());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, 400, "Bad Request: chat not found")] // #5301
    [InlineData(HttpStatusCode.Forbidden, 403, "Forbidden: user is deactivated")]
    [InlineData(HttpStatusCode.Forbidden, 403, "Forbidden: bot can't initiate conversation with a user")]
    public async Task SendTelegramNotification_WithChatUnavailableResponse_ReturnsPermanentUserFailure(HttpStatusCode statusCode, int errorCode, string description)
    {
        var service = CreateService(new RespondingHttpMessageHandler(
            statusCode,
            $$"""{"ok":false,"error_code":{{errorCode}},"description":"{{description}}"}"""));

        var result = await service.SendTelegramNotification(new TelegramChatId(1), new TelegramHtmlString("test"));

        result.ShouldBe(SendingResult.PermanentUserFailure());
    }

    [Fact]
    public async Task SendTelegramNotification_WithOtherBadRequest_Throws()
    {
        // Прочие 400 к получателю не относятся — их не глушим, они остаются повторяемыми
        var service = CreateService(new RespondingHttpMessageHandler(
            HttpStatusCode.BadRequest,
            """{"ok":false,"error_code":400,"description":"Bad Request: can't parse entities"}"""));

        _ = await Should.ThrowAsync<ApiRequestException>(
            () => service.SendTelegramNotification(new TelegramChatId(1), new TelegramHtmlString("test")));
    }

    private static TelegramNotificationServiceImpl CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var botClient = new TelegramBotClient("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", httpClient);
        return new TelegramNotificationServiceImpl(botClient, NullLogger<TelegramNotificationServiceImpl>.Instance);
    }

    [Fact]
    public async Task SendTelegramNotification_WithTimeoutException_ReturnsCommonFailure()
    {
        var handler = new ThrowingHttpMessageHandler(new TimeoutException("Request timed out"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var botClient = new TelegramBotClient("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", httpClient);
        var service = new TelegramNotificationServiceImpl(botClient, NullLogger<TelegramNotificationServiceImpl>.Instance);

        var result = await service.SendTelegramNotification(new TelegramChatId(1), new TelegramHtmlString("test"));

        result.ShouldBe(SendingResult.CommonFailure());
    }

    [Fact]
    public async Task SendTelegramNotification_WithInnerTimeoutException_ReturnsCommonFailure()
    {
        var inner = new TimeoutException("Request timed out");
        var outer = new HttpRequestException("Connection failed", inner);
        var handler = new ThrowingHttpMessageHandler(outer);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var botClient = new TelegramBotClient("123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11", httpClient);
        var service = new TelegramNotificationServiceImpl(botClient, NullLogger<TelegramNotificationServiceImpl>.Instance);

        var result = await service.SendTelegramNotification(new TelegramChatId(1), new TelegramHtmlString("test"));

        result.ShouldBe(SendingResult.CommonFailure());
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class RespondingHttpMessageHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }
}
