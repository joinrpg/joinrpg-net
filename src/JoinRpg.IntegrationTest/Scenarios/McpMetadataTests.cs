using System.Net;
using System.Text.Json;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// То, что MCP-клиент видит до того, как у него появился токен: метаданные ресурса
/// (RFC 9728) и challenge на 401. Оба огреха, которые чинит этот набор, были найдены
/// запросами к dev — юнит-тест на объект настроек их бы не поймал, потому что проблема
/// ровно в том, что уезжает по HTTP.
/// </summary>
public class McpMetadataTests : IAsyncLifetime
{
    private readonly McpEnabledApplicationFactory factory = new();

    public Task InitializeAsync() => ((IAsyncLifetime)factory).InitializeAsync();

    public Task DisposeAsync() => ((IAsyncLifetime)factory).DisposeAsync();

    [Theory]
    [InlineData("/.well-known/oauth-protected-resource")]
    // RFC 9728 §3: клиенту advertise'ится URL с путём ресурса в суффиксе — именно его он и дёрнет.
    [InlineData("/.well-known/oauth-protected-resource/mcp")]
    public async Task ResourceMetadata_ListsScopesClientMustRequest(string path)
    {
        var client = factory.CreateClient();

        var metadata = await client.GetFromJsonAsync<JsonElement>(path);

        var scopes = metadata.GetProperty("scopes_supported")
            .EnumerateArray().Select(x => x.GetString()).ToList();

        // Пустой список означал бы, что клиент не знает, какие scope просить у IdPortal.
        scopes.ShouldContain("joinrpg.read");

        // А лишнего тут быть не должно. Клиент запрашивает всё, что мы объявили, и если его
        // регистрация такого права не даёт, падает весь вход целиком (ID2051) — так и вышло
        // при подключении Claude Code к dev. Пишущих инструментов у /mcp нет, значит и scope
        // на запись объявлять нечего.
        scopes.ShouldNotContain("joinrpg.characters.write");
    }

    [Fact]
    public async Task Challenge_HasSingleWwwAuthenticate_PointingAtResourceMetadata()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync("/mcp", JsonContent.Create(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
        }));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Раньше заголовков было два: голый Bearer от OpenIddict и Bearer с resource_metadata
        // от MCP. Клиент, читающий первый, не находил указателя на метаданные.
        var challenges = response.Headers.WwwAuthenticate.ToList();
        challenges.Count.ShouldBe(1);
        challenges[0].Parameter.ShouldNotBeNull();
        challenges[0].Parameter!.ShouldContain("resource_metadata");
    }
}
