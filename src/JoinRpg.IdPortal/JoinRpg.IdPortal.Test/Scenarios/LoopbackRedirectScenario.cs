using System.Net;
using System.Security.Cryptography;
using System.Text;
using JoinRpg.IdPortal.OAuthServer;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// RFC 8252 §7.3: для loopback-адресов сервер обязан принимать любой порт — нативное приложение
/// получает свободный порт от ОС в момент запроса и заранее его не знает. Claude Code объявляет
/// в своих метаданных <c>http://localhost/callback</c> без порта, а приходит с
/// <c>http://localhost:43575/callback</c>; при точной сверке он не подключался вовсе.
/// </summary>
[Collection("IdPortal")]
public class LoopbackRedirectScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task LoopbackRedirect_WithDifferentPort_IsAccepted()
    {
        // Зарегистрирован без порта — ровно как в документе Claude Code.
        var clientId = await CreateClientAsync("http://localhost/callback");
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, "http://localhost:43575/callback"));

        response.StatusCode.ShouldBe(HttpStatusCode.Found);
        response.Headers.Location!.ToString().ShouldStartWith("/oauth/consent");
    }

    [Fact]
    public async Task LoopbackRedirect_DifferentPath_IsStillRejected()
    {
        // Послабление касается только порта: путь обязан совпадать.
        var clientId = await CreateClientAsync("http://localhost/callback");
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, "http://localhost:43575/stolen"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LoopbackRedirect_DifferentHost_IsStillRejected()
    {
        // localhost и 127.0.0.1 остаются разными адресами.
        var clientId = await CreateClientAsync("http://localhost/callback");
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, "http://127.0.0.1:43575/callback"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task NonLoopbackRedirect_WithDifferentPort_IsStillRejected()
    {
        // К публичным адресам послабление не применяется — иначе это дыра в open redirect.
        var clientId = await CreateClientAsync("https://app.example.com/callback");
        var client = await LoginAsync();

        var response = await client.GetAsync(BuildAuthorizeUrl(clientId, "https://app.example.com:8443/callback"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<string> CreateClientAsync(string redirectUri)
    {
        var clientId = $"test-loopback-{Guid.NewGuid():N}";
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IOAuthClientService>()
            .CreateClientAsync(clientId, "Тестовый нативный клиент", [new Uri(redirectUri)],
                OAuthClientType.Public, [JoinRpgScopes.Read], allowRefreshToken: false);
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

    private static string BuildAuthorizeUrl(string clientId, string redirectUri)
    {
        var verifier = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return $"/connect/authorize?client_id={clientId}&response_type=code" +
            $"&scope={Uri.EscapeDataString(JoinRpgScopes.Read)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&code_challenge={Uri.EscapeDataString(challenge)}&code_challenge_method=S256";
    }
}
