using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// В интроспекцию OpenIddict пускает только сторону, указанную в аудитории токена
/// (<c>Introspection.ValidateAuthorizedParty</c>). Отказ выглядит как <c>200 {"active": false}</c>,
/// то есть ничем не отличается от истёкшего или отозванного токена.
/// </summary>
/// <remarks>
/// Практическое следствие, ради которого тест и написан: <c>client_id</c> resource server обязан
/// совпадать с идентификатором ресурса. На dev он был <c>dev-joinrpg-ru</c> при аудитории
/// <c>https://dev.joinrpg.ru/mcp</c>, и Portal получал <c>active: false</c> на совершенно
/// нормальный токен — а наружу это выходило как 401 на /mcp без всяких объяснений.
///
/// Регистрация resource server с идентификатором-URL возможна только потому, что CIMD такие
/// строки больше не перехватывает, см. <c>CimdApplicationManager</c>.
/// </remarks>
[Collection("IdPortal")]
public class OAuthIntrospectionScenario(IdPortalApplicationFactory factory)
{
    private const string RedirectUri = "http://localhost/introspection-callback";

    /// <summary>MainHost в тестовом хосте — localhost, см. IdPortalApplicationFactory.</summary>
    private const string McpResource = "https://localhost/mcp";

    [Fact]
    public async Task ResourceServerNamedAfterResource_SeesTokenAsActive()
    {
        var token = await IssueTokenAsync();
        var secret = await CreateResourceServerAsync(McpResource);

        var payload = await IntrospectAsync(McpResource, secret, token);

        payload.GetProperty("active").GetBoolean().ShouldBeTrue();
        payload.GetProperty("scope").GetString().ShouldContain(JoinRpgScopes.Read);
    }

    [Fact]
    public async Task ResourceServerWithUnrelatedClientId_SeesTokenAsInactive()
    {
        // Ровно этот случай и был на dev. Ответ 200, поэтому отказ неотличим от истёкшего
        // токена — причину приходится знать заранее.
        var token = await IssueTokenAsync();
        var unrelated = $"test-rs-unrelated-{Guid.NewGuid():N}";
        var secret = await CreateResourceServerAsync(unrelated);

        var payload = await IntrospectAsync(unrelated, secret, token);

        payload.GetProperty("active").GetBoolean().ShouldBeFalse();
    }

    private async Task<string> CreateResourceServerAsync(string clientId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IOAuthClientService>()
            .CreateResourceServerAsync(clientId, "Тестовый resource server");
    }

    private async Task<string> IssueTokenAsync()
    {
        var clientId = $"test-introspection-{Guid.NewGuid():N}";
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IOAuthClientService>()
                .CreateOrUpdateClientAsync(clientId, null, "Тестовый MCP-клиент",
                    [new Uri(RedirectUri)], OAuthClientType.Public, [JoinRpgScopes.Read], false);
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var loginPage = await client.GetAsync("/Account/Login");
        var doc = await loginPage.AsHtmlDocument();
        var fields = doc.GetFormFields();
        fields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;
        fields["Input.Password"] = IdPortalApplicationFactory.TestUserPassword;
        await client.PostAsync(doc.GetFormAction("/Account/Login"), new FormUrlEncodedContent(fields!));

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

        var authorize = await client.GetAsync(
            $"/connect/authorize?client_id={clientId}&response_type=code" +
            $"&scope={Uri.EscapeDataString(JoinRpgScopes.Read)}" +
            $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&resource={Uri.EscapeDataString(McpResource)}" +
            $"&code_challenge={challenge}&code_challenge_method=S256" +
            $"&{OAuthConsent.ConsentParameter}={OAuthConsent.Granted}&{OAuthConsent.ProjectsParameter}=42");

        authorize.StatusCode.ShouldBe(HttpStatusCode.Found);
        var code = System.Web.HttpUtility
            .ParseQueryString(new Uri(authorize.Headers.Location!.ToString()).Query)["code"];

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
        return (await tokenResponse.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Токен не выдан");
    }

    private async Task<JsonElement> IntrospectAsync(string rsId, string rsSecret, string token)
    {
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(
                $"{Uri.EscapeDataString(rsId)}:{Uri.EscapeDataString(rsSecret)}")));

        var response = await http.PostAsync("/connect/introspect", new FormUrlEncodedContent(
            [new KeyValuePair<string?, string?>("token", token)]));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
