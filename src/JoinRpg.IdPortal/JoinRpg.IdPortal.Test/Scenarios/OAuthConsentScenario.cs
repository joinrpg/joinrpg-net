using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using HtmlAgilityPack;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.Test.Scenarios;

// Covers ADR012 §4 (consent screen + per-project claim). Большинство тестов обращается
// к AuthorizeMethod напрямую, повторяя то, что присылает страница согласия: запрос
// "consent=granted&projects=..." (или "consent=denied") на connect/authorize. Сама страница
// рендерится статически, поэтому её форму можно прогнать и по-настоящему — см. тесты
// ConsentPage_* в конце файла.
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

        var projectIds = await ReadGrantedProjectsAsync(clientId);
        projectIds.ShouldBe([123, 456]);
    }

    /// <summary>Читает из выданного клиенту согласия список проектов, к которым открыт доступ.</summary>
    private async Task<int[]> ReadGrantedProjectsAsync(string clientId)
    {
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
        return properties[OAuthConsent.ProjectsClaimType].Deserialize<int[]>()!;
    }

    /// <summary>
    /// Чекбоксы на странице согласия шлют каждый проект отдельным projects=N — проверяем,
    /// что connect/authorize разбирает такой запрос так же, как список через запятую.
    /// </summary>
    [Fact]
    public async Task Authorize_ConsentGrantedWithRepeatedProjectsParameter_StoresAllProjects()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge) +
            $"&{OAuthConsent.ConsentParameter}={OAuthConsent.Granted}" +
            $"&{OAuthConsent.ProjectsParameter}=123&{OAuthConsent.ProjectsParameter}=456");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldContain("code=");

        var projectIds = await ReadGrantedProjectsAsync(clientId);
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

    [Fact]
    public async Task ConsentPage_RendersStaticallyWithoutServerCircuit()
    {
        var client = await LoginAsync();
        var page = await GetConsentPageAsync(client, await CreateMcpClientAsync());

        page.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Маркер, которым Blazor размечает компонент с интерактивным render mode.
        // Смотрим только на тело страницы: в шапке сайта живёт своя обвязка макета.
        var doc = await page.AsHtmlDocument();
        var body = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'body-content')]");
        body.ShouldNotBeNull();
        body.InnerHtml.ShouldNotContain("\"type\":\"server\"");

        // И форма согласия действительно есть — значит статического рендера хватает.
        doc.DocumentNode.SelectSingleNode("//form[@id='oauth-consent-grant']").ShouldNotBeNull();

        // Кнопки лежат снаружи форм и привязаны к ним атрибутом form.
        doc.DocumentNode
            .SelectNodes("//button[@type='submit' and @form]")
            .Select(button => button.GetAttributeValue("form", ""))
            .ShouldBe(["oauth-consent-grant", "oauth-consent-deny"], ignoreOrder: true);
    }

    [Fact]
    public async Task ConsentPage_SubmitGrantForm_RedirectsToClientWithCode()
    {
        var client = await LoginAsync();
        var page = await GetConsentPageAsync(client, await CreateMcpClientAsync());

        var response = await client.GetAsync(BuildGetFormUrl(await page.AsHtmlDocument(), "oauth-consent-grant"));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(RedirectUri, Case.Insensitive);
        location.ShouldContain("code=");
    }

    [Fact]
    public async Task ConsentPage_SubmitDenyForm_RedirectsToClientWithAccessDenied()
    {
        var client = await LoginAsync();
        var page = await GetConsentPageAsync(client, await CreateMcpClientAsync());

        var response = await client.GetAsync(BuildGetFormUrl(await page.AsHtmlDocument(), "oauth-consent-deny"));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(RedirectUri, Case.Insensitive);
        location.ShouldContain($"error={Errors.AccessDenied}");
    }

    /// <summary>Идёт на connect/authorize и переходит по редиректу на страницу согласия.</summary>
    private static async Task<HttpResponseMessage> GetConsentPageAsync(HttpClient client, string clientId)
    {
        var authorize = await client.GetAsync(BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge));
        authorize.StatusCode.ShouldBe(HttpStatusCode.Found);
        var consentUrl = authorize.Headers.Location!.ToString();
        consentUrl.ShouldStartWith("/oauth/consent");
        return await client.GetAsync(consentUrl);
    }

    /// <summary>
    /// Собирает адрес, по которому браузер уйдёт при сабмите GET-формы с указанным id:
    /// action плюс все её поля. Чекбоксы (выбор проектов) по умолчанию сняты, поэтому
    /// в запрос не попадают — ровно как в браузере.
    /// </summary>
    private static string BuildGetFormUrl(HtmlDocument doc, string formId)
    {
        var form = doc.DocumentNode.SelectSingleNode($"//form[@id='{formId}']");
        form.ShouldNotBeNull();

        var fields = (form.SelectNodes(".//input") ?? Enumerable.Empty<HtmlNode>())
            .Where(input => input.GetAttributeValue("type", "") != "checkbox"
                || input.Attributes.Contains("checked"))
            .Select(input => new KeyValuePair<string, string?>(
                input.GetAttributeValue("name", ""),
                WebUtility.HtmlDecode(input.GetAttributeValue("value", ""))));

        return QueryHelpers.AddQueryString(form.GetAttributeValue("action", ""), fields);
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
