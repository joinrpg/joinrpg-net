using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// RFC 8707: MCP-клиент присылает <c>resource</c> безусловно — спека MCP требует этого
/// независимо от того, поддерживает ли сервер параметр, — а ресурс обязан проверять, что токен
/// выписан именно ему. Прав <c>rsrc:</c> не выдавалось никому, поэтому authorize обрывался на
/// <c>invalid_target</c>, не доходя до токена вовсе. Найдено при попытке подключить Claude Code
/// к dev.
/// </summary>
[Collection("IdPortal")]
public class OAuthResourceScenario(IdPortalApplicationFactory factory)
{
    private const string RedirectUri = "http://localhost/resource-callback";

    /// <summary>MainHost в тестовом хосте — localhost, см. IdPortalApplicationFactory.</summary>
    private const string McpResource = "https://localhost/mcp";

    [Fact]
    public async Task Authorize_WithResourceParameter_IssuesCode()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, resource: McpResource));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(RedirectUri, Case.Insensitive);
        location.ShouldContain("code=");
    }

    [Fact]
    public async Task Authorize_WithForeignResource_IsRejected()
    {
        // Право выдано ровно на наш /mcp, поэтому чужая аудитория отлетает.
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        var response = await client.GetAsync(
            BuildAuthorizeUrl(clientId, resource: "https://evil.example.com/mcp"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("invalid_target");
    }

    [Fact]
    public async Task TokenRequest_WithResource_Succeeds()
    {
        // Ещё одна проверка: resource приезжает и на token-эндпоинт, там он тоже валидируется
        // (ValidateResourcePermissions есть в обеих цепочках).
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();
        var (verifier, challenge) = CreatePkcePair();

        var authorize = await client.GetAsync(
            BuildAuthorizeUrl(clientId, resource: McpResource, challenge: challenge));
        authorize.StatusCode.ShouldBe(HttpStatusCode.Found);
        var code = System.Web.HttpUtility
            .ParseQueryString(new Uri(authorize.Headers.Location!.ToString()).Query)["code"];
        code.ShouldNotBeNull();

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new KeyValuePair<string?, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string?, string?>("code", code),
            new KeyValuePair<string?, string?>("redirect_uri", RedirectUri),
            new KeyValuePair<string?, string?>("client_id", clientId),
            new KeyValuePair<string?, string?>("code_verifier", verifier),
            new KeyValuePair<string?, string?>("resource", McpResource),
        ]));

        tokenResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("access_token").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task OidcLogin_CannotAskForMcpAudience()
    {
        // Сайтовый вход не должен получать токен, годный для /mcp: прав rsrc: у него нет.
        var client = await LoginAsync();

        var response = await client.GetAsync(
            $"/connect/authorize?client_id={IdPortalApplicationFactory.TestClientId}" +
            "&response_type=code&scope=openid" +
            $"&redirect_uri={Uri.EscapeDataString(IdPortalApplicationFactory.TestRedirectUri)}" +
            $"&resource={Uri.EscapeDataString(McpResource)}" +
            $"&code_challenge={Uri.EscapeDataString(CreatePkcePair().Challenge)}&code_challenge_method=S256");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        // Отвергается по правам клиента (invalid_request), а не по самому ресурсу: ресурс-то
        // объявлен глобально через RegisterResources, а вот права rsrc: на него есть только
        // у joinrpg-клиентов.
        (await response.Content.ReadAsStringAsync()).ShouldContain("not allowed to use the specified");
    }

    private async Task<string> CreateMcpClientAsync()
    {
        var clientId = $"test-mcp-resource-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IOAuthClientService>()
            .CreateOrUpdateClientAsync(clientId, null, "Тестовый MCP-клиент",
                [new Uri(RedirectUri)], OAuthClientType.Public, [JoinRpgScopes.Read],
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

    private static string BuildAuthorizeUrl(string clientId, string resource, string? challenge = null) =>
        $"/connect/authorize?client_id={clientId}&response_type=code" +
        $"&scope={Uri.EscapeDataString(JoinRpgScopes.Read)}" +
        $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
        $"&resource={Uri.EscapeDataString(resource)}" +
        $"&code_challenge={Uri.EscapeDataString(challenge ?? CreatePkcePair().Challenge)}" +
        "&code_challenge_method=S256" +
        $"&{OAuthConsent.ConsentParameter}={OAuthConsent.Granted}&{OAuthConsent.ProjectsParameter}=42";

    private static (string Verifier, string Challenge) CreatePkcePair()
    {
        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (verifier, Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
