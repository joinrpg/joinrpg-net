using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Страница ошибки перезапускается методом GET — и после исключения, и после кода статуса, —
/// а для остального конвейера запрос остаётся с исходным методом.
/// </summary>
public class ErrorPageReExecuteTest
{
    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task AfterException_ErrorPageIsGet(string method)
    {
        var outerMethod = new MethodHolder();
        await using var app = await StartApp(outerMethod);

        var response = await Send(app, method, "/throw");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync()).ShouldBe("error page: GET");
        outerMethod.Value.ShouldBe(method);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task AfterStatusCode_ErrorPageIsGet(string method)
    {
        var outerMethod = new MethodHolder();
        await using var app = await StartApp(outerMethod);

        var response = await Send(app, method, "/not-found");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldBe("error page: GET");
        outerMethod.Value.ShouldBe(method);
    }

    [Fact]
    public async Task OrdinaryRequest_KeepsMethod()
    {
        await using var app = await StartApp(new MethodHolder());

        var response = await Send(app, "DELETE", "/ok");

        (await response.Content.ReadAsStringAsync()).ShouldBe("ok: DELETE");
    }

    private static async Task<HttpResponseMessage> Send(WebApplication app, string method, string path)
        => await app.GetTestClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

    private sealed class MethodHolder
    {
        public string? Value { get; set; }
    }

    private static async Task<WebApplication> StartApp(MethodHolder outerMethod)
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.WebHost.UseTestServer();

        var app = builder.Build();
        // Внешний middleware видит метод уже после всех перезапусков — как метрики и лог хостинга.
        _ = app.Use(async (context, next) =>
        {
            await next(context);
            outerMethod.Value = context.Request.Method;
        });
        _ = app.UseExceptionHandler("/error");
        _ = app.UseStatusCodePagesWithReExecute("/error");
        _ = app.UseErrorPageReExecuteAsGet();
        app.Run(context => context.Request.Path.Value switch
        {
            "/error" => context.Response.WriteAsync($"error page: {context.Request.Method}"),
            "/throw" => throw new InvalidOperationException("Падение для теста"),
            "/not-found" => SetStatus(context, StatusCodes.Status404NotFound),
            _ => context.Response.WriteAsync($"ok: {context.Request.Method}"),
        });
        await app.StartAsync();
        return app;
    }

    private static Task SetStatus(HttpContext context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
}
