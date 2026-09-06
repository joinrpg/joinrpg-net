using System.Net;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenIddict.Abstractions;

namespace JoinRpg.IdPortal.Test.Scenarios;

[Collection("IdPortal")]
public class OAuthClientEditScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task EditOAuthClient_GetForm_UnauthenticatedRedirectsToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/oauthclients/some-client/edit");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.ToString().ShouldContain("Login");
    }

    // Service-level integration tests
    // Form submission for InteractiveServer pages goes through SignalR, not HTTP POST,
    // so update logic is tested directly via IOAuthClientService.

    [Fact]
    public async Task UpdateClient_ExistingConfidentialClient_KeepsOldSecretValid()
    {
        var clientId = $"test-edit-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var originalSecret = await clientService.CreateClientAsync(
            clientId,
            "Исходное название",
            [new Uri("https://example.com/v1")],
            OAuthClientType.Confidential,
            [OpenIddictConstants.Scopes.OpenId],
            allowRefreshToken: false);
        originalSecret.ShouldNotBeNullOrEmpty();

        var newSecret = await clientService.UpdateClientAsync(
            clientId,
            "Новое название",
            [new Uri("https://example.com/v2")],
            OAuthClientType.Confidential,
            [OpenIddictConstants.Scopes.OpenId, OpenIddictConstants.Scopes.Email],
            allowRefreshToken: true);

        newSecret.ShouldBeNull();

        var updated = await manager.FindByClientIdAsync(clientId);
        updated.ShouldNotBeNull();
        (await manager.GetDisplayNameAsync(updated)).ShouldBe("Новое название");
        (await manager.GetRedirectUrisAsync(updated)).ShouldBe(["https://example.com/v2"]);
        (await manager.ValidateClientSecretAsync(updated, originalSecret!)).ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateClient_SwitchedFromPublicToConfidential_GeneratesNewSecret()
    {
        var clientId = $"test-edit-switch-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await clientService.CreateClientAsync(
            clientId,
            null,
            [new Uri("https://example.com/callback")],
            OAuthClientType.Public,
            [JoinRpgScopes.Read],
            allowRefreshToken: false);

        var newSecret = await clientService.UpdateClientAsync(
            clientId,
            null,
            [new Uri("https://example.com/callback")],
            OAuthClientType.Confidential,
            [JoinRpgScopes.Read],
            allowRefreshToken: false);

        newSecret.ShouldNotBeNullOrEmpty();

        var updated = await manager.FindByClientIdAsync(clientId);
        updated.ShouldNotBeNull();
        (await manager.ValidateClientSecretAsync(updated, newSecret!)).ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateClient_NonExistingClient_ThrowsInvalidOperationException()
    {
        var clientId = $"test-edit-missing-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();

        await Should.ThrowAsync<InvalidOperationException>(() => clientService.UpdateClientAsync(
            clientId,
            null,
            [new Uri("https://example.com/callback")],
            OAuthClientType.Confidential,
            [OpenIddictConstants.Scopes.OpenId],
            allowRefreshToken: false));
    }

    [Fact]
    public async Task RegenerateSecret_ExistingConfidentialClient_InvalidatesOldSecret()
    {
        var clientId = $"test-regen-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var originalSecret = await clientService.CreateClientAsync(
            clientId,
            null,
            [new Uri("https://example.com/callback")],
            OAuthClientType.Confidential,
            [OpenIddictConstants.Scopes.OpenId],
            allowRefreshToken: false);

        var newSecret = await clientService.RegenerateSecretAsync(clientId);

        newSecret.ShouldNotBeNullOrEmpty();
        newSecret.ShouldNotBe(originalSecret);

        var updated = await manager.FindByClientIdAsync(clientId);
        updated.ShouldNotBeNull();
        (await manager.ValidateClientSecretAsync(updated, originalSecret!)).ShouldBeFalse();
        (await manager.ValidateClientSecretAsync(updated, newSecret)).ShouldBeTrue();
    }

    [Fact]
    public async Task RegenerateSecret_PublicClient_ThrowsInvalidOperationException()
    {
        var clientId = $"test-regen-public-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();

        await clientService.CreateClientAsync(
            clientId,
            null,
            [new Uri("https://example.com/callback")],
            OAuthClientType.Public,
            [JoinRpgScopes.Read],
            allowRefreshToken: false);

        await Should.ThrowAsync<InvalidOperationException>(() => clientService.RegenerateSecretAsync(clientId));
    }

    [Fact]
    public async Task RegenerateSecret_NonExistingClient_ThrowsInvalidOperationException()
    {
        var clientId = $"test-regen-missing-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();

        await Should.ThrowAsync<InvalidOperationException>(() => clientService.RegenerateSecretAsync(clientId));
    }
}
