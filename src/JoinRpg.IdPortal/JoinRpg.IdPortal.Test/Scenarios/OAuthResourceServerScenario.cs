using System.Text.Json;
using JoinRpg.IdPortal.OAuthServer;
using OpenIddict.Abstractions;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// Resource server (ADR012 §5) — клиент, который не получает токены, а проверяет чужие через
/// connect/introspect. Права на интроспекцию не должно быть ни у кого больше: иначе любой
/// MCP-клиент смог бы разворачивать чужие токены.
/// </summary>
[Collection("IdPortal")]
public class OAuthResourceServerScenario(IdPortalApplicationFactory factory)
{
    /// <summary>
    /// Страж: Portal валидирует MCP-токены через интроспекцию, поэтому эндпоинт обязан быть
    /// в discovery. Без этого теста регрессия видна только на живом стенде — именно так она и
    /// обнаружилась на dev: /mcp отдавал 404, а introspection_endpoint отсутствовал.
    /// </summary>
    [Fact]
    public async Task Discovery_AdvertisesIntrospectionEndpoint()
    {
        var client = factory.CreateClient();

        var discovery = await client.GetFromJsonAsync<Dictionary<string, JsonElement>>(
            "/.well-known/openid-configuration");

        discovery.ShouldNotBeNull();
        discovery.ShouldContainKey("introspection_endpoint");
        discovery["introspection_endpoint"].GetString().ShouldEndWith("/connect/introspect");
    }

    [Fact]
    public async Task CreateResourceServer_GrantsIntrospectionAndNothingElse()
    {
        var clientId = $"test-resource-server-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var secret = await clientService.CreateResourceServerAsync(clientId, "Portal /mcp");

        secret.ShouldNotBeNullOrEmpty();

        var created = await manager.FindByClientIdAsync(clientId);
        created.ShouldNotBeNull();
        (await manager.GetClientTypeAsync(created)).ShouldBe(OpenIddictConstants.ClientTypes.Confidential);

        var permissions = await manager.GetPermissionsAsync(created);
        permissions.ShouldBe([OpenIddictConstants.Permissions.Endpoints.Introspection]);

        // Resource server сам токенов не получает — redirect URI ему не нужны.
        (await manager.GetRedirectUrisAsync(created)).ShouldBeEmpty();
    }

    [Fact]
    public async Task OrdinaryClient_DoesNotGetIntrospectionPermission()
    {
        var clientId = $"test-ordinary-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await clientService.CreateClientAsync(
            clientId,
            "Обычный MCP-клиент",
            [new Uri("https://example.com/callback")],
            OAuthClientType.Confidential,
            [JoinRpgScopes.Read],
            allowRefreshToken: true);

        var created = await manager.FindByClientIdAsync(clientId);
        created.ShouldNotBeNull();
        (await manager.GetPermissionsAsync(created))
            .ShouldNotContain(OpenIddictConstants.Permissions.Endpoints.Introspection);
    }
}
