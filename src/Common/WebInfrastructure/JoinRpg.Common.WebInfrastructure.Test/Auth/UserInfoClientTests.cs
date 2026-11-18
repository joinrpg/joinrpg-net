using System.Net;
using System.Text;
using System.Text.Json;
using JoinRpg.Common.WebInfrastructure.Auth;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Test.Auth;

public class UserInfoClientTests
{
    [Fact]
    public async Task GetUserInfoClaimsAsync_ReturnsStringClaims()
    {
        var handler = new StubHttpMessageHandler(
            _ => JsonResponse("""{"sub":"123","preferred_username":"nick","picture":"url"}"""));
        var client = CreateClient(handler);

        var claims = await client.GetUserInfoClaimsAsync("test-token", CancellationToken.None);

        claims.Single(c => c.Type == "preferred_username").Value.ShouldBe("nick");
        claims.Single(c => c.Type == "picture").Value.ShouldBe("url");
    }

    [Fact]
    public async Task GetUserInfoClaimsAsync_ParsesNonStringScalars()
    {
        var handler = new StubHttpMessageHandler(
            _ => JsonResponse("""{"num":42,"flag":true,"nothing":null,"nested":{"a":1},"arr":[1,2]}"""));
        var client = CreateClient(handler);

        var claims = await client.GetUserInfoClaimsAsync("test-token", CancellationToken.None);

        claims.Single(c => c.Type == "num").Value.ShouldBe("42");
        claims.Single(c => c.Type == "flag").Value.ShouldBe("true");
        claims.ShouldNotContain(c => c.Type == "nothing");
        claims.ShouldNotContain(c => c.Type == "nested");
        claims.ShouldNotContain(c => c.Type == "arr");
    }

    [Fact]
    public async Task GetUserInfoClaimsAsync_SendsBearerToken()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{"sub":"123"}"""));
        var client = CreateClient(handler);

        _ = await client.GetUserInfoClaimsAsync("test-token", CancellationToken.None);

        handler.CapturedAuthorization.ShouldBe("Bearer test-token");
    }

    [Fact]
    public async Task GetUserInfoClaimsAsync_InvalidJson_Throws()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("not-json"));
        var client = CreateClient(handler);

        _ = await Should.ThrowAsync<JsonException>(
            () => client.GetUserInfoClaimsAsync("test-token", CancellationToken.None));
    }

    private static UserInfoClient CreateClient(StubHttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("https://issuer.example/") });

    private static HttpResponseMessage JsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => this.responder = responder;

        public string? CapturedAuthorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedAuthorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(responder(request));
        }
    }
}
