using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Фингерпринтные (content-addressed) файлы /_framework/* отдаются с immutable-кешем middleware'ом
/// до routing — иначе StaticAssetsInvoker MapStaticAssets перезаписал бы Cache-Control на no-cache
/// и браузер качал бы мегабайты WASM-рантайма при каждом открытии страницы с островом.
/// Нефингерпринтные точки входа (blazor.web.js, dotnet.js) и обычная статика не затрагиваются.
/// </summary>
public class FrameworkCacheControlPipelineTest
{
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";
    private const string FingerprintedWasm = "/_framework/JoinRpg.Blazor.Client.49q3ee79xq.wasm";

    [Fact]
    public async Task FingerprintedFrameworkFile_IsServedWithImmutableCache()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.GetAsync(FingerprintedWasm);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe(ImmutableCacheControl);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/wasm");
        (await response.Content.ReadAsStringAsync()).ShouldBe("fake-wasm-content");
    }

    [Fact]
    public async Task FingerprintedFrameworkFile_HeadReturnsHeadersWithoutBody()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, FingerprintedWasm));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe(ImmutableCacheControl);
        response.Content.Headers.ContentLength.ShouldBe(17);
        (await response.Content.ReadAsStringAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task FingerprintedFrameworkFile_BrotliVersionIsServedWhenAccepted()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, FingerprintedWasm);
        request.Headers.AcceptEncoding.ParseAdd("br");
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe(ImmutableCacheControl);
        response.Content.Headers.ContentEncoding.ShouldContain("br");
        response.Headers.Vary.ShouldContain("Accept-Encoding");
        response.Content.Headers.ContentLength.ShouldBe(24);
        (await response.Content.ReadAsStringAsync()).ShouldBe("fake-wasm-content-brotli");
    }

    [Fact]
    public async Task FingerprintedFrameworkFile_GzipVersionIsServedWhenBrotliNotAccepted()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, FingerprintedWasm);
        request.Headers.AcceptEncoding.ParseAdd("gzip");
        var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe(ImmutableCacheControl);
        response.Content.Headers.ContentEncoding.ShouldContain("gzip");
        response.Content.Headers.ContentLength.ShouldBe(23);
        (await response.Content.ReadAsStringAsync()).ShouldBe("fake-wasm-content-gzip!");
    }

    [Fact]
    public async Task NonFingerprintedFrameworkFile_KeepsNoCacheFromMapStaticAssets()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/_framework/blazor.web.js");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe("no-cache");
        (await response.Content.ReadAsStringAsync()).ShouldBe("console.log(\"blazor\");\n");
    }

    [Fact]
    public async Task BlazorWebAssemblyEntryPoint_KeepsNoCache()
    {
        // Регрессия: сегмент "webassembly" — 11 символов [a-z], при нестрогой проверке длины
        // точка входа ошибочно уходила бы в immutable и не обновлялась бы после деплоя.
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/_framework/blazor.webassembly.js");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe("no-cache");
    }

    [Fact]
    public async Task PlainStaticFileOutsideFramework_IsNotAffected()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/app.js");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl?.ToString().ShouldBe("no-cache");
        response.Headers.CacheControl?.ToString().ShouldNotBe(ImmutableCacheControl);
    }

    [Fact]
    public async Task MissingFingerprintedFile_PassesThroughToPipeline()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/_framework/Gone.12345678ab.wasm");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (response.Headers.CacheControl?.ToString()).ShouldNotBe(ImmutableCacheControl);
    }

    [Fact]
    public async Task NonGetRequest_PassesThroughToPipeline()
    {
        await using var app = await StartApp();
        using var client = app.GetTestClient();

        var response = await client.PostAsync(FingerprintedWasm, content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        (response.Headers.CacheControl?.ToString()).ShouldNotBe(ImmutableCacheControl);
    }

    private static async Task<WebApplication> StartApp([CallerFilePath] string testSourcePath = "")
    {
        // Content root — каталог тестового проекта: MapStaticAssets берёт манифест из вывода сборки,
        // а файлы по plain-путям — из wwwroot проекта (WebRootFileProvider).
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Production",
            ContentRootPath = Path.GetDirectoryName(testSourcePath),
        });
        _ = builder.WebHost.UseTestServer();
        _ = builder.Services.AddJoinFrameworkCacheControl();

        var app = builder.Build();
        // Манифест генерируется SDK.Web от AssemblyName тестового проекта; под тестраннером ApplicationName
        // — testhost, поэтому путь передаём явно.
        var manifestPath = Path.Combine(AppContext.BaseDirectory, "JoinRpg.Common.WebInfrastructure.Test.staticwebassets.endpoints.json");
        _ = app.MapStaticAssets(manifestPath);
        await app.StartAsync();
        return app;
    }
}
