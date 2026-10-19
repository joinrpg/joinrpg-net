using System.Text.Json;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Test.Infrastructure;

public class WebApiExceptionHandlerTest
{
    private static readonly ProjectIdentification ProjectId = new(1611);

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        return new DefaultHttpContext
        {
            RequestServices = services,
            Request = { Path = path },
            Response = { Body = new MemoryStream() },
        };
    }

    private static async Task<(bool Handled, JsonElement Problem)> Handle(HttpContext httpContext, Exception exception)
    {
        var handler = new WebApiExceptionHandler(httpContext.RequestServices.GetRequiredService<IProblemDetailsService>());
        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);
        httpContext.Response.Body.Position = 0;
        var problem = handled ? JsonDocument.Parse(httpContext.Response.Body).RootElement : default;
        return (handled, problem);
    }

    [Fact]
    public async Task BlockedProject_InWebApi_ReturnsConflictWithReason()
    {
        var httpContext = CreateHttpContext("/webapi/1611/project/SaveContactSettings");

        var (handled, problem) = await Handle(httpContext, new ProjectBlockedException(ProjectId));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status409Conflict);
        httpContext.Response.ContentType.ShouldBe("application/problem+json");
        problem.GetProperty("title").GetString().ShouldBe("Проект заблокирован на время восстановления данных, изменения в нём сейчас невозможны");
    }

    [Fact]
    public async Task UnknownException_InWebApi_ReturnsServerErrorProblem()
    {
        var httpContext = CreateHttpContext("/webapi/1611/project/SaveContactSettings");

        var (handled, problem) = await Handle(httpContext, new InvalidOperationException("секрет"));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        problem.GetProperty("title").GetString().ShouldBe("Неизвестная ошибка на сервере");
        problem.GetRawText().ShouldNotContain("секрет");
    }

    [Fact]
    public async Task OutsideWebApi_NotHandled()
    {
        var httpContext = CreateHttpContext("/1611/project/settings");

        var (handled, _) = await Handle(httpContext, new ProjectBlockedException(ProjectId));

        handled.ShouldBeFalse();
    }

    [Fact]
    public void UnknownException_IsNotExpectedRefusal()
    {
        WebApiExceptionHandler.IsExpectedRefusal(new InvalidOperationException()).ShouldBeFalse();
        WebApiExceptionHandler.IsExpectedRefusal(new ProjectDeactivatedException(ProjectId)).ShouldBeTrue();
    }
}
