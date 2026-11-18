using System.Net;
using System.Security.Claims;
using System.Text;
using JoinRpg.Common.WebInfrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure.Test.Auth;

public class LoginPrincipalEnricherTests
{
    [Fact]
    public async Task EnrichAsync_MergesClaims()
    {
        var handler = new StubHttpMessageHandler(
            _ => JsonResponse("""{"sub":"1","preferred_username":"nick","picture":"url"}"""));
        var enricher = CreateEnricher(handler);
        var principal = CreatePrincipal();
        var properties = CreatePropertiesWithToken();

        var result = await enricher.EnrichAsync(principal, properties, CancellationToken.None);

        result.Claims.Single(c => c.Type == "preferred_username").Value.ShouldBe("nick");
        result.Claims.Single(c => c.Type == "picture").Value.ShouldBe("url");
        result.Claims.ShouldContain(c => c.Type == "sub");
    }

    [Fact]
    public async Task EnrichAsync_PartialResponse_MergesWhatPresent()
    {
        var handler = new StubHttpMessageHandler(
            _ => JsonResponse("""{"preferred_username":"nick"}"""));
        var enricher = CreateEnricher(handler);
        var principal = CreatePrincipal();
        var properties = CreatePropertiesWithToken();

        var result = await enricher.EnrichAsync(principal, properties, CancellationToken.None);

        result.Claims.Single(c => c.Type == "preferred_username").Value.ShouldBe("nick");
        result.Claims.ShouldNotContain(c => c.Type == "picture");
    }

    [Fact]
    public async Task EnrichAsync_HttpError_ReturnsOriginal()
    {
        var handler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var enricher = CreateEnricher(handler);
        var principal = CreatePrincipal();
        var properties = CreatePropertiesWithToken();

        var result = await enricher.EnrichAsync(principal, properties, CancellationToken.None);

        result.ShouldBeSameAs(principal);
        result.Claims.ShouldNotContain(c => c.Type == "preferred_username");
    }

    [Fact]
    public async Task EnrichAsync_MissingToken_NoRequest()
    {
        var handler = new StubHttpMessageHandler(
            _ => JsonResponse("""{"preferred_username":"nick"}"""));
        var enricher = CreateEnricher(handler);
        var principal = CreatePrincipal();
        var properties = new AuthenticationProperties();

        var result = await enricher.EnrichAsync(principal, properties, CancellationToken.None);

        result.ShouldBeSameAs(principal);
        handler.RequestCount.ShouldBe(0);
    }

    private static LoginPrincipalEnricher CreateEnricher(StubHttpMessageHandler handler)
        => new(
            new UserInfoClient(new HttpClient(handler) { BaseAddress = new Uri("https://issuer.example/") }),
            NullLogger<LoginPrincipalEnricher>.Instance);

    private static ClaimsPrincipal CreatePrincipal()
        => new(new ClaimsIdentity([new Claim("sub", "1")], "test"));

    private static AuthenticationProperties CreatePropertiesWithToken()
    {
        var properties = new AuthenticationProperties();
        properties.StoreTokens([new AuthenticationToken { Name = "access_token", Value = "t" }]);
        return properties;
    }

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

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responder(request));
        }
    }
}
