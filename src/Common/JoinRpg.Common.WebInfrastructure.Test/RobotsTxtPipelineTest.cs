using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// /robots.txt отдаётся без участия приложения и раньше его middleware — иначе запросы роботов
/// попадают в логирование и SPA-фоллбэки.
/// </summary>
public class RobotsTxtPipelineTest
{
    private const string AppMiddlewareHeader = "X-App-Middleware";

    [Fact]
    public async Task RobotsTxtIsServedBeforeAppMiddleware()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/Robots.txt");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        response.Headers.Contains(AppMiddlewareHeader).ShouldBeFalse();
        (await response.Content.ReadAsStringAsync()).ShouldBe("User-agent: *\nDisallow: /health\nDisallow: /metrics\nDisallow: /fromcode\nDisallow: /fromconfig\n");
    }

    [Fact]
    public async Task PostGoesToApp()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.PostAsync("/robots.txt", content: null);

        response.Headers.Contains(AppMiddlewareHeader).ShouldBeTrue();
    }

    private static async Task<WebApplication> StartApp()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.WebHost.UseTestServer();
        builder.Configuration["RobotsTxt:DisallowedPaths:0"] = "/fromconfig";

        _ = builder.Services.AddOptions<RobotsTxtOptions>()
            .Configure(robots => robots.DisallowedPaths.Add("/fromcode"));
        _ = builder.Services.AddJoinRobotsTxt();

        var app = builder.Build();
        _ = app.Use((context, next) =>
        {
            context.Response.Headers[AppMiddlewareHeader] = "1";
            return next(context);
        });
        await app.StartAsync();
        return app;
    }
}
