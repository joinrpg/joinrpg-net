using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using JoinRpg.IdPortal.OAuthServer;
using JoinRpg.IdPortal.OAuthServer.Cimd;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// CIMD — client_id в виде HTTPS-URL (ADR012 §3). Проверяется именно встраивание в OpenIddict:
/// что подменённый менеджер резолвит такого клиента на обоих эндпоинтах, что redirect_uri
/// сверяется с документом и что строка в БД заводится только после аутентификации.
/// Сетевая часть подменена (<see cref="FakeCimdMetadataLoader"/>): тестовый хост живёт на
/// loopback, куда защита от SSRF справедливо не пускает. Она покрыта юнит-тестами.
/// </summary>
[Collection("IdPortal")]
public class CimdScenario(IdPortalApplicationFactory factory)
{
    private const string RedirectUri = "http://localhost/cimd-callback";

    [Fact]
    public async Task Discovery_AdvertisesCimdSupport()
    {
        // §5 драфта: без этого флага клиент даже не попробует CIMD.
        var discovery = await factory.CreateClient()
            .GetFromJsonAsync<Dictionary<string, JsonElement>>("/.well-known/openid-configuration");

        discovery.ShouldNotBeNull();
        discovery.ShouldContainKey(CimdRegistration.SupportedMetadataParameter);
        discovery[CimdRegistration.SupportedMetadataParameter].GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Authorize_WithCimdClientId_ResolvesClientAndAsksForConsent()
    {
        var clientId = PublishDocument();
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId));

        // Дошли до экрана согласия — значит клиент опознан по документу, хотя строки в БД нет.
        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldStartWith("/oauth/consent");
    }

    [Fact]
    public async Task Authorize_RedirectUriIsCheckedAgainstDocument()
    {
        var clientId = PublishDocument();
        var client = await LoginAsync();

        // Оба запроса — один и тот же клиент, отличается только redirect_uri. Проверять надо
        // именно парой: отказ сам по себе ничего не доказывает, ведь неизвестный клиент тоже
        // даёт 400, и тест проходил бы даже без поддержки CIMD.
        var fromDocument = await client.GetAsync(BuildAuthorizeUrl(clientId));
        var notFromDocument = await client.GetAsync(
            BuildAuthorizeUrl(clientId, redirectUri: "http://localhost/not-in-document"));

        fromDocument.StatusCode.ShouldBe(HttpStatusCode.Found);
        // Перенаправлять некуда — OpenIddict отвечает ошибкой, а не редиректом.
        notFromDocument.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Authorize_DocumentUnavailable_IsRejectedWhileSameShapeClientWorks()
    {
        var published = PublishDocument();
        var unpublished = $"https://unknown-{Guid.NewGuid():N}.example.com/client.json";
        var client = await LoginAsync();

        // Опять пара: URL одинаковой формы, разница только в том, что по одному документ
        // отдаётся, а по другому нет.
        var withDocument = await client.GetAsync(BuildAuthorizeUrl(published));
        var withoutDocument = await client.GetAsync(BuildAuthorizeUrl(unpublished));

        withDocument.StatusCode.ShouldBe(HttpStatusCode.Found);
        withoutDocument.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Authorize_Anonymous_DoesNotCreateApplicationRow()
    {
        // Ключевое ограничение: иначе кто угодно смог бы засорять таблицу приложений,
        // подставляя произвольные URL в client_id, без единого залогиненного пользователя.
        var clientId = PublishDocument();
        var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await anonymous.GetAsync(BuildAuthorizeUrl(clientId));

        response.StatusCode.ShouldBe(HttpStatusCode.Found); // уводит на логин
        (await CountApplicationRowsAsync(clientId)).ShouldBe(0);
    }

    [Fact]
    public async Task Authorize_ConsentGranted_PersistsApplicationRow()
    {
        var clientId = PublishDocument();
        var client = await LoginAsync();

        (await CountApplicationRowsAsync(clientId)).ShouldBe(0);

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId)
            + $"&{OAuthConsent.ConsentParameter}={OAuthConsent.Granted}&{OAuthConsent.ProjectsParameter}=42");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldStartWith(RedirectUri, Case.Insensitive);

        // Строка обязана появиться: у Authorizations и Tokens внешний ключ на приложение,
        // без неё выданные токены не связались бы с клиентом.
        (await CountApplicationRowsAsync(clientId)).ShouldBe(1);
    }

    [Fact]
    public async Task Authorize_RepeatedConsent_DoesNotDuplicateApplicationRow()
    {
        var clientId = PublishDocument();
        var client = await LoginAsync();

        for (var i = 0; i < 2; i++)
        {
            var response = await client.GetAsync(BuildAuthorizeUrl(clientId)
                + $"&{OAuthConsent.ConsentParameter}={OAuthConsent.Granted}&{OAuthConsent.ProjectsParameter}=42");
            response.StatusCode.ShouldBe(HttpStatusCode.Found);
        }

        (await CountApplicationRowsAsync(clientId)).ShouldBe(1);
    }

    [Fact]
    public async Task OrdinaryClientIds_StillResolveFromDatabase()
    {
        // Подмена менеджера не должна ломать обычных клиентов.
        var client = await LoginAsync();

        var response = await client.GetAsync(
            $"/connect/authorize?client_id={IdPortalApplicationFactory.TestClientId}" +
            "&response_type=code&scope=openid" +
            $"&redirect_uri={Uri.EscapeDataString(IdPortalApplicationFactory.TestRedirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(CreateChallenge())}&code_challenge_method=S256");

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString()
            .ShouldStartWith(IdPortalApplicationFactory.TestRedirectUri, Case.Insensitive);
    }

    private string PublishDocument(bool loopbackOnly = false)
    {
        var clientId = $"https://app-{Guid.NewGuid():N}.example.com/client.json";
        var redirects = loopbackOnly
            ? "\"http://127.0.0.1:3000/callback\""
            : $"\"{RedirectUri}\"";

        factory.Cimd.Publish(clientId, $$"""
            {
              "client_id": "{{clientId}}",
              "client_name": "Тестовый CIMD-клиент",
              "redirect_uris": [{{redirects}}],
              "token_endpoint_auth_method": "none"
            }
            """);

        return clientId;
    }

    private async Task<int> CountApplicationRowsAsync(string clientId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdPortalDbContext>();
        return await db.Set<OpenIddictEntityFrameworkCoreApplication>()
            .CountAsync(a => a.ClientId == clientId);
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

    private static string BuildAuthorizeUrl(string clientId, string redirectUri = RedirectUri) =>
        $"/connect/authorize?client_id={Uri.EscapeDataString(clientId)}" +
        "&response_type=code" +
        $"&scope={Uri.EscapeDataString(JoinRpgScopes.Read)}" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        $"&code_challenge={Uri.EscapeDataString(CreateChallenge())}&code_challenge_method=S256";

    private static string CreateChallenge()
    {
        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return Base64UrlEncode(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
