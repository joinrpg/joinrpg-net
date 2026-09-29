using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace JoinRpg.IdPortal.Test.Scenarios;

// Covers ADR012 §4 (consent screen + per-project claim). Согласие выдаётся только через
// страницу согласия — POST-ом её формы с antiforgery-токеном, поэтому тесты прогоняют
// настоящий HTML: берут форму из отрисованной страницы и отправляют так, как это сделал бы
// браузер (см. ConsentFlowHelpers). connect/authorize решение из запроса не принимает —
// это стережёт Authorize_ConsentGrantedInQuery_IsIgnoredAndAsksUserAgain.
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

    /// <summary>
    /// Главная защита: согласие нельзя выдать ссылкой. Иначе достаточно было бы заманить
    /// залогиненного пользователя на «...&amp;consent=granted», чтобы открыть чужому клиенту
    /// доступ к его проектам — а клиент в CIMD регистрируется самозаписью по домену.
    /// </summary>
    [Fact]
    public async Task Authorize_ConsentGrantedInQuery_IsIgnoredAndAsksUserAgain()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge) +
            $"&{OAuthConsent.ConsentParameter}=granted&{OAuthConsent.ProjectsParameter}={factory.TestProjectId}");

        // Не код клиенту, а снова экран согласия.
        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldStartWith("/oauth/consent");

        (await FindAuthorizationAsync(clientId)).ShouldBeNull();
    }

    /// <summary>Без antiforgery-токена форму согласия отправить нельзя.</summary>
    [Fact]
    public async Task ConsentPage_SubmitWithoutAntiforgeryToken_Rejected()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();
        var page = await GetConsentPageAsync(client, clientId);

        var fields = (await page.AsHtmlDocument())
            .GetConsentFormFields(grant: true, factory.TestProjectId)
            .Where(field => field.Key != "__RequestVerificationToken");

        var response = await client.PostAsync(page.RequestMessage!.RequestUri, new FormUrlEncodedContent(fields!));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FindAuthorizationAsync(clientId)).ShouldBeNull();
    }

    [Fact]
    public async Task ConsentPage_SubmitGrant_RedirectsToClientWithCode()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        var response = await client.AuthorizeWithConsentAsync(BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(RedirectUri, Case.Insensitive);
        location.ShouldContain("code=");
    }

    [Fact]
    public async Task ConsentPage_SubmitDeny_RedirectsToClientWithAccessDenied()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        var response = await client.AuthorizeWithConsentAsync(
            BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge), grant: false);

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        var location = response.Headers.Location!.ToString();
        location.ShouldStartWith(RedirectUri, Case.Insensitive);
        location.ShouldContain($"error={Errors.AccessDenied}");

        (await FindAuthorizationAsync(clientId)).ShouldBeNull();
    }

    [Fact]
    public async Task ConsentPage_SubmitGrantWithProject_StoresProjectInAuthorization()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        _ = await client.AuthorizeWithConsentAsync(
            BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge), projectIds: [factory.TestProjectId]);

        (await ReadGrantedProjectsAsync(clientId)).ShouldBe([factory.TestProjectId]);
    }

    /// <summary>
    /// Форму отправляет сам пользователь, поэтому дописать в неё чужой id ничто не мешает —
    /// в согласие должны попасть только те проекты, которые ему на странице показали.
    /// </summary>
    [Fact]
    public async Task ConsentPage_SubmitGrantWithForeignProject_ForeignProjectDropped()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        _ = await client.AuthorizeWithConsentAsync(
            BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge),
            projectIds: [factory.TestProjectId, 999999]);

        (await ReadGrantedProjectsAsync(clientId)).ShouldBe([factory.TestProjectId]);
    }

    [Fact]
    public async Task Authorize_WithExistingConsent_SkipsConsentPage()
    {
        var clientId = await CreateMcpClientAsync();
        var client = await LoginAsync();

        // First pass grants consent...
        _ = await client.AuthorizeWithConsentAsync(
            BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge), projectIds: [factory.TestProjectId]);

        // ...second pass, without a consent decision, should go straight to the client callback.
        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, CreatePkcePair().Challenge));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldStartWith(RedirectUri, Case.Insensitive);
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
        var form = doc.DocumentNode.SelectSingleNode($"//form[@id='{ConsentFlowHelpers.ConsentFormId}']");
        form.ShouldNotBeNull();
        form.GetAttributeValue("method", "").ShouldBe("post");
        form.SelectSingleNode(".//input[@name='__RequestVerificationToken']").ShouldNotBeNull();

        // Проект тестового пользователя предлагается к выбору.
        WebUtility.HtmlDecode(form.InnerText).ShouldContain(IdPortalApplicationFactory.TestProjectName);
        form.SelectSingleNode($".//input[@type='checkbox' and @value='{factory.TestProjectId}']").ShouldNotBeNull();
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

    /// <summary>Читает из выданного клиенту согласия список проектов, к которым открыт доступ.</summary>
    private async Task<int[]> ReadGrantedProjectsAsync(string clientId)
    {
        var authorization = await FindAuthorizationAsync(clientId);
        authorization.ShouldNotBeNull();

        using var scope = factory.Services.CreateScope();
        var authorizationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();
        var properties = await authorizationManager.GetPropertiesAsync(authorization);
        properties.ShouldContainKey(OAuthConsent.ProjectsClaimType);
        return properties[OAuthConsent.ProjectsClaimType].Deserialize<int[]>()!;
    }

    private async Task<object?> FindAuthorizationAsync(string clientId)
    {
        using var scope = factory.Services.CreateScope();
        var authorizationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var application = await applicationManager.FindByClientIdAsync(clientId);
        var applicationId = await applicationManager.GetIdAsync(application!);

        await foreach (var candidate in authorizationManager.FindAsync(
            subject: null, client: applicationId, status: Statuses.Valid, type: AuthorizationTypes.Permanent, scopes: null))
        {
            return candidate;
        }

        return null;
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
