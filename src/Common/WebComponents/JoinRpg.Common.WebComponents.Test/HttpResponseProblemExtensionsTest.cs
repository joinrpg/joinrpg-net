using System.Net;
using System.Text;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Какие отказы webapi острова могут показать пользователю как есть (<see cref="ServerProblemException"/>),
/// а какие остаются сбоем (<see cref="HttpRequestException"/>), — #5735.
/// </summary>
public class HttpResponseProblemExtensionsTest
{
    private static HttpResponseMessage Response(HttpStatusCode status, string? body = null, string mediaType = "text/plain")
        => new(status) { Content = body is null ? new ByteArrayContent([]) : new StringContent(body, Encoding.UTF8, mediaType) };

    private static async Task<string> RefusalMessage(HttpResponseMessage response)
        => (await Should.ThrowAsync<ServerProblemException>(response.EnsureSuccessOrThrowProblem())).Message;

    [Fact]
    public async Task Success_DoesNotThrow()
        => await Response(HttpStatusCode.OK, "ok").EnsureSuccessOrThrowProblem();

    [Theory]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ProblemDetails_TitleIsShown(HttpStatusCode status)
    {
        var response = Response(status, """{"status":409,"title":"Проект заблокирован"}""", "application/problem+json");

        (await RefusalMessage(response)).ShouldBe("Проект заблокирован");
    }

    /// <summary>
    /// Необработанное исключение на сервере — сбой, а не отказ: остров покажет свой текст и запишет ошибку в лог.
    /// </summary>
    [Fact]
    public async Task ServerError_ProblemDetails_IsFailure()
        => await Should.ThrowAsync<HttpRequestException>(
            Response(HttpStatusCode.InternalServerError, """{"status":500,"title":"Неизвестная ошибка на сервере"}""", "application/problem+json")
                .EnsureSuccessOrThrowProblem());

    [Fact]
    public async Task ValidationProblem_ErrorsAreShownInsteadOfEnglishTitle()
    {
        var response = Response(
            HttpStatusCode.BadRequest,
            """{"title":"One or more validation errors occurred.","status":400,"errors":{"Name":["Укажите название."],"Capacity":["Мест должно быть больше нуля."]}}""",
            "application/problem+json");

        (await RefusalMessage(response)).ShouldBe("Укажите название. Мест должно быть больше нуля.");
    }

    [Fact]
    public async Task ValidationProblem_BlankErrors_FallBackToTitle()
    {
        var response = Response(HttpStatusCode.BadRequest, """{"title":"Некорректный запрос","errors":{"Name":[" "]}}""", "application/problem+json");

        (await RefusalMessage(response)).ShouldBe("Некорректный запрос");
    }

    [Fact]
    public async Task Forbidden_ProblemWithoutTitle_SaysNoAccess()
        => (await RefusalMessage(Response(HttpStatusCode.Forbidden, """{"status":403}""", "application/problem+json")))
            .ShouldBe("Недостаточно прав для этого действия");

    [Theory]
    [InlineData("application/problem+json")]
    [InlineData("text/plain")]
    public async Task UnknownCharset_IsFailure(string mediaType)
    {
        var response = Response(HttpStatusCode.Conflict, """{"title":"Проект в архиве"}""", mediaType);
        response.Content.Headers.ContentType!.CharSet = "foo";

        await Should.ThrowAsync<HttpRequestException>(response.EnsureSuccessOrThrowProblem());
    }

    [Fact]
    public async Task MalformedProblemDetails_IsFailure()
        => await Should.ThrowAsync<HttpRequestException>(
            Response(HttpStatusCode.Conflict, "{", "application/problem+json").EnsureSuccessOrThrowProblem());

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ClientError_PlainText_IsShown(HttpStatusCode status)
        => (await RefusalMessage(Response(status, " В комнате не хватает мест\n"))).ShouldBe("В комнате не хватает мест");

    [Fact]
    public async Task ClientError_JsonString_IsShown()
    {
        var response = Response(HttpStatusCode.BadRequest, "\"Пользователь не найден\"", "application/json");

        (await RefusalMessage(response)).ShouldBe("Пользователь не найден");
    }

    [Fact]
    public async Task ClientError_JsonObject_IsFailure()
        => await Should.ThrowAsync<HttpRequestException>(
            Response(HttpStatusCode.BadRequest, """{"a":1}""", "application/json").EnsureSuccessOrThrowProblem());

    [Fact]
    public async Task ServerError_PlainText_IsNotShown()
        => await Should.ThrowAsync<HttpRequestException>(
            Response(HttpStatusCode.InternalServerError, "System.NullReferenceException at ...").EnsureSuccessOrThrowProblem());

    [Theory]
    [InlineData(null, "text/plain")]
    [InlineData("<html><body>Ошибка</body></html>", "text/html")]
    public async Task Unauthorized_WithoutReason_AsksToSignIn(string? body, string mediaType)
        => (await RefusalMessage(Response(HttpStatusCode.Unauthorized, body, mediaType)))
            .ShouldBe("Вы не вошли на сайт или сессия истекла — обновите страницу");

    [Fact]
    public async Task Forbidden_WithoutReason_SaysNoAccess()
        => (await RefusalMessage(Response(HttpStatusCode.Forbidden)))
            .ShouldBe("Недостаточно прав для этого действия");

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, null, "text/plain")]
    [InlineData(HttpStatusCode.BadRequest, "   ", "text/plain")]
    [InlineData(HttpStatusCode.NotFound, "<html></html>", "text/html")]
    public async Task ClientError_WithoutReason_IsFailure(HttpStatusCode status, string? body, string mediaType)
        => await Should.ThrowAsync<HttpRequestException>(Response(status, body, mediaType).EnsureSuccessOrThrowProblem());

    [Fact]
    public async Task ReadFromJson_ReturnsBody()
        => (await Response(HttpStatusCode.OK, """{"Name":"Шатёр"}""", "application/json").ReadFromJsonOrThrowProblem<Room>())
            .ShouldBe(new Room("Шатёр"));

    [Fact]
    public async Task ReadFromJson_Refusal_ThrowsProblem()
        => await Should.ThrowAsync<ServerProblemException>(
            Response(HttpStatusCode.Conflict, """{"title":"Проект в архиве"}""", "application/problem+json").ReadFromJsonOrThrowProblem<Room>());

    [Fact]
    public async Task ReadFromJson_NullBody_Throws()
    {
        var exception = await Should.ThrowAsync<Exception>(
            Response(HttpStatusCode.OK, "null", "application/json").ReadFromJsonOrThrowProblem<Room>());

        exception.ShouldNotBeOfType<ServerProblemException>();
    }

    private record Room(string Name);
}
