using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.IdPortal.OAuthServer.Test;

public class OAuthProtocolExceptionHandlerTest
{
    /// <param name="originalPath">
    /// Исходный путь, если <c>UseExceptionHandler("/Error")</c> уже переписал путь запроса на <c>/Error</c> — так
    /// обработчик вызывается на проде.
    /// </param>
    private static async Task<(bool Handled, HttpContext HttpContext, string Body)> Handle(string path, Exception exception, string? originalPath = null)
    {
        var httpContext = new DefaultHttpContext
        {
            Request = { Path = path },
            Response = { Body = new MemoryStream() },
        };
        if (originalPath is not null)
        {
            httpContext.Features.Set<IExceptionHandlerPathFeature>(new ExceptionHandlerFeature { Path = originalPath, Error = exception });
        }
        var handled = await new OAuthProtocolExceptionHandler().TryHandleAsync(httpContext, exception, CancellationToken.None);
        httpContext.Response.Body.Position = 0;
        var body = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();
        return (handled, httpContext, body);
    }

    [Theory]
    [InlineData("/connect/token")]
    [InlineData("/connect/introspect")]
    [InlineData("/connect/user_info")]
    [InlineData("/.well-known/openid-configuration")]
    public async Task ProtocolPath_ReturnsOAuthServerErrorJson(string path)
    {
        var (handled, httpContext, body) = await Handle(path, new InvalidOperationException("секрет"));

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        httpContext.Response.ContentType!.ShouldStartWith("application/json");
        JsonDocument.Parse(body).RootElement.GetProperty("error").GetString().ShouldBe("server_error");
        body.ShouldNotContain("секрет");
    }

    [Fact]
    public async Task ProtocolPathRewrittenToErrorPage_StillHandled()
    {
        var (handled, httpContext, body) = await Handle("/Error", new InvalidOperationException(), originalPath: "/connect/token");

        handled.ShouldBeTrue();
        httpContext.Response.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        JsonDocument.Parse(body).RootElement.GetProperty("error").GetString().ShouldBe("server_error");
    }

    [Fact]
    public async Task PageRewrittenToErrorPage_NotHandled()
    {
        var (handled, _, _) = await Handle("/Error", new InvalidOperationException(), originalPath: "/Account/Login");

        handled.ShouldBeFalse();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Account/Login")]
    [InlineData("/connector")]
    public async Task OutsideProtocolPath_NotHandled(string path)
    {
        var (handled, _, _) = await Handle(path, new InvalidOperationException());

        handled.ShouldBeFalse();
    }
}
