using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

[Collection("IdPortal")]
public class OAuthServerScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task AuthorizationCodeFlow_ReturnsAccessToken()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (verifier, challenge) = CreatePkcePair();

        var code = (await RunAuthorizationCodeStepsAsync(client, challenge))?["code"];
        code.ShouldNotBeNull("Authorization code should be in redirect to callback URI");

        var tokenResponse = await ExchangeCodeForTokenAsync(client, code, verifier);
        tokenResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        tokenJson.ShouldNotBeNull();
        tokenJson.ShouldContainKey("access_token");
        tokenJson.ShouldContainKey("token_type");
        tokenJson["token_type"]!.ToString()!.ToLowerInvariant().ShouldBe("bearer");
    }

    [Fact]
    public async Task AuthorizationCodeFlow_WithoutPkce_IsRejected()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Required PKCE is validated by OpenIddict before our AuthorizeMethod runs at all -
        // the request is rejected with 400 directly, even before the login redirect.
        var authorizeResponse = await client.GetAsync(BuildAuthorizeUrl(codeChallenge: null, scope: "openid email profile"));

        authorizeResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await authorizeResponse.Content.ReadAsStringAsync();
        body.ShouldContain("invalid_request");
    }

    [Fact]
    public async Task RefreshTokenFlow_ReturnsNewAccessToken()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (verifier, challenge) = CreatePkcePair();

        var code = (await RunAuthorizationCodeStepsAsync(client, challenge, scope: "openid email offline_access"))?["code"];
        code.ShouldNotBeNull();

        var tokenResponse = await ExchangeCodeForTokenAsync(client, code, verifier);
        tokenResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        var refreshToken = tokenJson?["refresh_token"]?.ToString();
        refreshToken.ShouldNotBeNull("Requesting offline_access should issue a refresh token");

        var refreshResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new KeyValuePair<string?, string?>("grant_type", "refresh_token"),
            new KeyValuePair<string?, string?>("refresh_token", refreshToken),
            new KeyValuePair<string?, string?>("client_id", IdPortalApplicationFactory.TestClientId),
            new KeyValuePair<string?, string?>("client_secret", IdPortalApplicationFactory.TestClientSecret),
        ]));

        refreshResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshJson = await refreshResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        refreshJson.ShouldNotBeNull();
        refreshJson.ShouldContainKey("access_token");
    }

    [Fact]
    public async Task UserInfo_WithValidToken_ReturnsClaims()
    {
        // First get a valid token
        var accessToken = await GetAccessTokenAsync();
        accessToken.ShouldNotBeNull();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userInfoResponse = await client.GetAsync("/connect/user_info");
        userInfoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        userInfo.ShouldNotBeNull();
        userInfo.ShouldContainKey("sub");
        userInfo.ShouldContainKey("email");
        userInfo["email"]!.ToString().ShouldBe(IdPortalApplicationFactory.TestUserEmail);
    }

    private static (string Verifier, string Challenge) CreatePkcePair()
    {
        var verifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64UrlEncode(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier)));
        return (verifier, challenge);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string BuildAuthorizeUrl(string? codeChallenge, string scope) =>
        $"/connect/authorize?client_id={IdPortalApplicationFactory.TestClientId}" +
        $"&response_type=code" +
        $"&scope={Uri.EscapeDataString(scope)}" +
        $"&redirect_uri={Uri.EscapeDataString(IdPortalApplicationFactory.TestRedirectUri)}" +
        (codeChallenge is null ? "" : $"&code_challenge={Uri.EscapeDataString(codeChallenge)}&code_challenge_method=S256");

    private static Task<HttpResponseMessage> ExchangeCodeForTokenAsync(HttpClient client, string? code, string? codeVerifier) =>
        client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new KeyValuePair<string?, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string?, string?>("code", code),
            new KeyValuePair<string?, string?>("redirect_uri", IdPortalApplicationFactory.TestRedirectUri),
            new KeyValuePair<string?, string?>("client_id", IdPortalApplicationFactory.TestClientId),
            new KeyValuePair<string?, string?>("client_secret", IdPortalApplicationFactory.TestClientSecret),
            .. codeVerifier is null ? [] : new[] { new KeyValuePair<string?, string?>("code_verifier", codeVerifier) },
        ]));

    /// Drives GET /connect/authorize through login and follows redirects up to the callback URI,
    /// returning its query string (either "code"+"state" on success, or "error" on rejection).
    private static async Task<System.Collections.Specialized.NameValueCollection?> RunAuthorizationCodeStepsAsync(HttpClient client, string? codeChallenge, string scope = "openid email profile")
    {
        var authorizeResponse = await client.GetAsync(BuildAuthorizeUrl(codeChallenge, scope));
        var loginRedirect = authorizeResponse.Headers.Location?.ToString();
        if (loginRedirect is null)
        {
            return null;
        }

        var loginPageResponse = await client.GetAsync(loginRedirect);
        var loginDoc = await loginPageResponse.AsHtmlDocument();
        var loginFields = loginDoc.GetFormFields();
        loginFields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;
        loginFields["Input.Password"] = IdPortalApplicationFactory.TestUserPassword;

        var loginResponse = await client.PostAsync(loginDoc.GetFormAction(loginRedirect), new FormUrlEncodedContent(loginFields!));
        var afterLoginRedirect = loginResponse.Headers.Location?.ToString();
        if (afterLoginRedirect is null)
        {
            return null;
        }

        var maxRedirects = 5;
        var currentUrl = afterLoginRedirect;
        while (maxRedirects-- > 0)
        {
            var redirectResponse = await client.GetAsync(currentUrl);
            if (redirectResponse.StatusCode != HttpStatusCode.Found)
            {
                break;
            }

            var nextLocation = redirectResponse.Headers.Location?.ToString();
            if (nextLocation is null)
            {
                break;
            }

            if (nextLocation.StartsWith(IdPortalApplicationFactory.TestRedirectUri, StringComparison.OrdinalIgnoreCase))
            {
                var callbackUri = new Uri(nextLocation);
                return System.Web.HttpUtility.ParseQueryString(callbackUri.Query);
            }
            currentUrl = nextLocation;
        }

        return null;
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var (verifier, challenge) = CreatePkcePair();

        var code = (await RunAuthorizationCodeStepsAsync(client, challenge))?["code"];
        if (code is null)
        {
            return null;
        }

        var tokenResponse = await ExchangeCodeForTokenAsync(client, code, verifier);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        return tokenJson?["access_token"]?.ToString();
    }
}
