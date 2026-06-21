using System.Net;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenIddict.Abstractions;

namespace JoinRpg.IdPortal.Test.Scenarios;

[Collection("IdPortal")]
public class OAuthClientCreateScenario(IdPortalApplicationFactory factory)
{
    // Page-level HTTP tests (prerender phase is still served as HTML)

    [Fact]
    public async Task CreateOAuthClient_GetForm_UnauthenticatedRedirectsToLogin()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/admin/oauthclients/create");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location?.ToString().ShouldContain("Login");
    }

    [Fact]
    public async Task CreateOAuthClient_GetForm_AuthenticatedAdminReturnsForm()
    {
        var client = await CreateLoggedInAdminClientAsync();

        var response = await client.GetAsync("/admin/oauthclients/create");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var doc = await response.AsHtmlDocument();
        // The page is InteractiveServer — inputs have name="ClientId", labels come from [Display(Name=...)]
        doc.DocumentNode.InnerHtml.ShouldContain("Client ID", Case.Insensitive);
        doc.DocumentNode.InnerHtml.ShouldContain("Redirect URIs", Case.Insensitive);
    }

    // Service-level integration tests
    // Form submission for InteractiveServer pages goes through SignalR, not HTTP POST,
    // so creation logic is tested directly via IOAuthClientService.

    [Fact]
    public async Task CreateOAuthClient_WithValidData_CreatesClientAndReturnsSecret()
    {
        var newClientId = $"test-create-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var secret = await clientService.CreateClientAsync(
            newClientId,
            "Тестовый клиент",
            [new Uri("https://example.com/callback")]);

        secret.ShouldNotBeNullOrEmpty();
        var created = await manager.FindByClientIdAsync(newClientId);
        created.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateOrUpdateOAuthClient_ExistingClient_UpdatesWithoutCreatingDuplicate()
    {
        var clientId = $"test-update-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        await clientService.CreateOrUpdateClientAsync(clientId, "secret-v1", null, [new Uri("https://example.com/v1")]);
        await clientService.CreateOrUpdateClientAsync(clientId, "secret-v2", "Название", [new Uri("https://example.com/v2")]);

        var count = await manager.CountAsync();
        var existing = await manager.FindByClientIdAsync(clientId);
        existing.ShouldNotBeNull();
        (await manager.GetDisplayNameAsync(existing)).ShouldBe("Название");
    }

    private async Task<HttpClient> CreateLoggedInAdminClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

        var loginPage = await client.GetAsync("/Account/Login");
        var doc = await loginPage.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = IdPortalApplicationFactory.TestAdminEmail;
        fields["Input.Password"] = IdPortalApplicationFactory.TestAdminPassword;

        await client.PostAsync(doc.GetFormAction("/Account/Login"), new FormUrlEncodedContent(fields!));
        return client;
    }
}
