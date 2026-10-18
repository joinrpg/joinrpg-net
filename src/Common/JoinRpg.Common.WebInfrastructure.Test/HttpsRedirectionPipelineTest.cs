using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Редирект http → https и HSTS встраиваются в начало конвейера, после ForwardedHeaders.
/// </summary>
public class HttpsRedirectionPipelineTest
{
    // localhost HSTS не получает (ExcludedHosts по умолчанию), поэтому HSTS проверяем на другом хосте
    private const string PublicHost = "joinrpg.example";

    [Fact]
    public async Task HttpIsRedirectedToHttps()
    {
        await using var app = await StartApp(Environments.Production);
        using var client = CreateClient(app);

        var response = await client.GetAsync("/project/1?x=2");

        response.StatusCode.ShouldBe(HttpStatusCode.TemporaryRedirect);
        response.Headers.Location.ShouldBe(new Uri("https://localhost/project/1?x=2"));
    }

    [Fact]
    public async Task ForwardedHttpsIsNotRedirectedAndGetsHsts()
    {
        await using var app = await StartApp(Environments.Production);
        using var client = CreateClient(app);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/project/1");
        request.Headers.Host = PublicHost;
        request.Headers.Add("X-Forwarded-Proto", "https");
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("Strict-Transport-Security").ShouldBe(["max-age=86400"]);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/metrics")]
    public async Task InfrastructurePathsAreNotRedirected(string path)
    {
        await using var app = await StartApp(Environments.Production);
        using var client = CreateClient(app);

        var response = await client.GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoHstsInDevelopment()
    {
        await using var app = await StartApp(Environments.Development);
        using var client = CreateClient(app);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/project/1");
        request.Headers.Host = PublicHost;
        request.Headers.Add("X-Forwarded-Proto", "https");
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("Strict-Transport-Security").ShouldBeFalse();
    }

    [Fact]
    public async Task DisabledSwitchTurnsOffRedirectAndHsts()
    {
        await using var app = await StartApp(Environments.Production, enabled: false);
        using var client = CreateClient(app);

        var httpResponse = await client.GetAsync("/project/1");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/project/1");
        request.Headers.Host = PublicHost;
        request.Headers.Add("X-Forwarded-Proto", "https");
        var httpsResponse = await client.SendAsync(request);

        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        httpsResponse.Headers.Contains("Strict-Transport-Security").ShouldBeFalse();
    }

    private static HttpClient CreateClient(WebApplication app)
    {
        var client = app.GetTestClient();
        client.BaseAddress = new Uri("http://localhost/");
        return client;
    }

    private static async Task<WebApplication> StartApp(string environmentName, bool? enabled = null)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        _ = builder.WebHost.UseTestServer();
        if (enabled is { } value)
        {
            builder.Configuration[$"{JoinHttpsRedirectionOptions.SectionName}:{nameof(JoinHttpsRedirectionOptions.Enabled)}"] = value.ToString();
        }
        _ = builder.Services.AddJoinForwardedHeaders(builder.Environment);

        var app = builder.Build();
        app.Run(context => context.Response.WriteAsync("app"));
        await app.StartAsync();
        return app;
    }
}
