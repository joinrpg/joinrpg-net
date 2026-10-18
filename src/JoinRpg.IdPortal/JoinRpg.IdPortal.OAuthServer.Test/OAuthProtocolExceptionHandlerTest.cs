using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.IdPortal.OAuthServer.Test;

public class OAuthProtocolExceptionHandlerTest
{
    private static async Task<(bool Handled, HttpContext HttpContext, string Body)> Handle(string path, Exception exception)
    {
        var httpContext = new DefaultHttpContext
        {
            Request = { Path = path },
            Response = { Body = new MemoryStream() },
        };
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
