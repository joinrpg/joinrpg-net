using JoinRpg.IdPortal.OAuthServer;
using OpenIddict.Abstractions;

namespace JoinRpg.IdPortal.Test.Scenarios;

[Collection("IdPortal")]
public class OAuthClientDeleteScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task DeleteClient_ExistingClient_RemovesClient()
    {
        var clientId = $"test-delete-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await clientService.CreateClientAsync(
            clientId,
            "Тестовый клиент для удаления",
            [new Uri("https://example.com/callback")]);

        await clientService.DeleteClientAsync(clientId);

        var deleted = await manager.FindByClientIdAsync(clientId);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteClient_NonExistingClient_ThrowsInvalidOperationException()
    {
        var clientId = $"test-delete-missing-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();

        await Should.ThrowAsync<InvalidOperationException>(() => clientService.DeleteClientAsync(clientId));
    }
}
