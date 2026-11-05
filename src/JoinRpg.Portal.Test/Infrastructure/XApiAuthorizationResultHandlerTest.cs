using System.Text.Encodings.Web;
using System.Text.Json;
using JoinRpg.Portal.Infrastructure.XApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Test.Infrastructure;

public class XApiAuthorizationResultHandlerTest
{
    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails();
        _ = services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, StubBearerHandler>(JwtBearerDefaults.AuthenticationScheme, _ => { });
        var provider = services.BuildServiceProvider();
        return new DefaultHttpContext
        {
            RequestServices = provider,
            Request = { Path = path },
            Response = { Body = new MemoryStream() },
        };
    }

    private static async Task<JsonDocument> HandleAndReadProblemAsync(DefaultHttpContext context, PolicyAuthorizationResult authorizeResult)
    {
        var handler = new XApiAuthorizationResultHandler(context.RequestServices.GetRequiredService<IProblemDetailsService>());
        var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
        await handler.HandleAsync(_ => Task.CompletedTask, context, policy, authorizeResult);
        context.Response.Body.Position = 0;
        return JsonDocument.Parse(context.Response.Body);
    }

    [Fact]
    public async Task Forbid_OnXApiPath_ReturnsProblemDetails403()
    {
        var context = CreateHttpContext("/x-api/users/1/");

        var problem = await HandleAndReadProblemAsync(context, PolicyAuthorizationResult.Forbid());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        context.Response.ContentType.ShouldBe("application/problem+json");
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(403);
        problem.RootElement.GetProperty("title").GetString().ShouldBe("Недостаточно прав для этого действия");
    }

    [Fact]
    public async Task Challenge_OnXApiPath_ReturnsProblemDetails401AndKeepsAuthenticateHeader()
    {
        var context = CreateHttpContext("/x-api/users/1/");

        var problem = await HandleAndReadProblemAsync(context, PolicyAuthorizationResult.Challenge());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        context.Response.Headers.WWWAuthenticate.ShouldNotBeEmpty();
        context.Response.ContentType.ShouldBe("application/problem+json");
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(401);
        problem.RootElement.GetProperty("title").GetString().ShouldBe("Токен доступа отсутствует, невалиден или истёк");
    }

    [Theory]
    [InlineData("/1611/project/settings")]
    [InlineData("/webapi/1611/project/SaveContactSettings")]
    public async Task Forbid_OnNonXApiPath_KeepsDefaultBehavior(string path)
    {
        var context = CreateHttpContext(path);

        var handler = new XApiAuthorizationResultHandler(context.RequestServices.GetRequiredService<IProblemDetailsService>());
        var policy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).RequireAuthenticatedUser().Build();
        await handler.HandleAsync(_ => Task.CompletedTask, context, policy, PolicyAuthorizationResult.Forbid());

        context.Response.StatusCode.ShouldBe(StatusCodes.Status403Forbidden);
        context.Response.ContentType.ShouldBeNull($"путь {path}: ожидался дефолтный ответ без ProblemDetails, получен Content-Type: {context.Response.ContentType}");
        context.Response.Body.Length.ShouldBe(0);
    }

    /// <summary>Stub JwtBearer: аутентификации нет, challenge — 401 + WWW-Authenticate (как настоящий handler).</summary>
    private class StubBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers.WWWAuthenticate = Scheme.Name;
            return Task.CompletedTask;
        }
    }
}
