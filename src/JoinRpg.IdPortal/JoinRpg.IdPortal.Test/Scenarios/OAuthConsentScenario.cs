using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.Test.Scenarios;

// Covers ADR012 §4 (consent screen + per-project claim). The consent page itself is
// InteractiveServer (see OAuthClientCreateScenario for why that can't be driven over plain
// HTTP), so these tests exercise AuthorizeMethod directly by simulating what the page sends
// back: a "consent=granted&projects=..." (or "consent=denied") query on connect/authorize.
// Each test uses its own client id: authorizations are keyed by (subject, client), and the
// same test user is shared across tests in the collection, so reusing a client id would leak
// consent state between tests.
[Collection("IdPortal")]
public class OAuthConsentScenario(IdPortalApplicationFactory factory)
{
    private const string RedirectUri = "http://localhost/mcp-callback";

    [Fact]
    public async Task Authorize_WithJoinRpgScope_NoExistingConsent_RedirectsToConsentPage()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, challenge: CreatePkcePair().Challenge));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith("/oauth/consent");
        location.ShouldContain($"client_id={clientId}");
    }

    [Fact]
    public async Task Authorize_ConsentDenied_RedirectsToClientWithAccessDenied()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, challenge: CreatePkcePair().Challenge) +
            $"&{OAuthConsent.ConsentParameter}={OAuthConsent.Denied}");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(RedirectUri);
        location.ShouldContain($"error={Errors.AccessDenied}");
    }

    [Fact]
    public async Task Authorize_ConsentGranted_CreatesAuthorizationWithProjectsProperty()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();
        var (_, challenge) = CreatePkcePair();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, challenge) +
            $"&{OAuthConsent.ConsentParameter}={OAuthConsent.Granted}&{OAuthConsent.ProjectsParameter}=123,456");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(RedirectUri, Case.Insensitive);
        location.ShouldContain("code=");

        using var scope = factory.Services.CreateScope();
        var authorizationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await applicationManager.FindByClientIdAsync(clientId);
        var applicationId = await applicationManager.GetIdAsync(application!);

        object? authorization = null;
        await foreach (var candidate in authorizationManager.FindAsync(
            subject: null, client: applicationId, status: Statuses.Valid, type: AuthorizationTypes.Permanent, scopes: null))
        {
            authorization = candidate;
            break;
        }
        authorization.ShouldNotBeNull();

        var properties = await authorizationManager.GetPropertiesAsync(authorization);
        properties.ShouldContainKey(OAuthConsent.ProjectsClaimType);
        var projectIds = properties[OAuthConsent.ProjectsClaimType].Deserialize<int[]>();
        projectIds.ShouldBe([123, 456]);
    }

    [Fact]
    public async Task Authorize_WithExistingConsent_SkipsConsentPage()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        // First pass grants consent...
        var granted = await client.GetAsync(BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge) +
            $"&{OAuthConsent.ConsentParameter}={OAuthConsent.Granted}&{OAuthConsent.ProjectsParameter}=789");
        granted.StatusCode.ShouldBe(HttpStatusCode.Found);

        // ...second pass, without a consent decision, should go straight to the client callback.
        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(RedirectUri, Case.Insensitive);
    }

    private async Task<string> CreateMcpClientAsync()
    {
        var clientId = $"test-mcp-consent-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IOAuthClientService>();
        await clientService.CreateOrUpdateClientAsync(
            clientId,
            clientSecret: null,
            displayName: "Тестовый MCP-клиент",
            redirectUris: [new Uri(RedirectUri)],
            clientType: OAuthClientType.Public,
            scopes: [JoinRpgScopes.Read],
            allowRefreshToken: false);
        return clientId;
    }

    private async Task<HttpClient> LoginAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var loginPage = await client.GetAsync("/Account/Login");
        var doc = await loginPage.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;
        fields["Input.Password"] = IdPortalApplicationFactory.TestUserPassword;
        await client.PostAsync(doc.GetFormAction("/Account/Login"), new FormUrlEncodedContent(fields!));

        return client;
    }

    private static string BuildAuthorizeUrl(string clientId, string challenge) =>
        $"/connect/authorize?client_id={clientId}" +
        "&response_type=code" +
        $"&scope={Uri.EscapeDataString(JoinRpgScopes.Read)}" +
        $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
        $"&code_challenge={Uri.EscapeDataString(challenge)}&code_challenge_method=S256";

    private static (string Verifier, string Challenge) CreatePkcePair()
    {
        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64UrlEncode(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        return (verifier, challenge);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
