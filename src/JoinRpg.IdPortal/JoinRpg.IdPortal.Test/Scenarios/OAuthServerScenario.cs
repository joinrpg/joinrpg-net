using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IdPortal.Test.Scenarios;

[Collection("IdPortal")]
public class OAuthServerScenario(IdPortalApplicationFactory factory)
{
    [Fact(Skip = "TODO: fix")]
    public async Task AuthorizationCodeFlow_ReturnsAccessToken()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Step 1: GET /connect/authorize - should redirect to login
        var authorizeUrl = BuildAuthorizeUrl();
        var authorizeResponse = await client.GetAsync(authorizeUrl);

        authorizeResponse.StatusCode.ShouldBe(HttpStatusCode.Found);
        var loginRedirect = authorizeResponse.Headers.Location?.ToString();
        loginRedirect.ShouldNotBeNull();
        loginRedirect.ShouldContain("Login");

        // Step 2: GET /Account/Login
        var loginPageResponse = await client.GetAsync(loginRedirect);
        loginPageResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Step 3: POST login credentials
        var loginDoc = await loginPageResponse.AsHtmlDocument();
        var loginFields = loginDoc.GetFormFields();
        loginFields["Input.Email"] = IdPortalApplicationFactory.TestUserEmail;
        loginFields["Input.Password"] = IdPortalApplicationFactory.TestUserPassword;

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(loginFields!));
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Found);

        // Step 4: Follow redirect back to /connect/authorize
        var afterLoginRedirect = loginResponse.Headers.Location?.ToString();
        afterLoginRedirect.ShouldNotBeNull();

        // Follow redirects until we get the code (may be multiple redirects)
        string? code = null;
        var maxRedirects = 5;
        var currentUrl = afterLoginRedirect;
        while (maxRedirects-- > 0 && code is null)
        {
            var redirectResponse = await client.GetAsync(currentUrl);
            if (redirectResponse.StatusCode == HttpStatusCode.Found)
            {
                var nextLocation = redirectResponse.Headers.Location?.ToString();
                nextLocation.ShouldNotBeNull();

                if (nextLocation.StartsWith(IdPortalApplicationFactory.TestRedirectUri, StringComparison.OrdinalIgnoreCase))
                {
                    var codeUri = new Uri(nextLocation);
                    var queryParams = System.Web.HttpUtility.ParseQueryString(codeUri.Query);
                    code = queryParams["code"];
                    break;
                }
                currentUrl = nextLocation;
            }
            else
            {
                break;
            }
        }

        code.ShouldNotBeNull("Authorization code should be in redirect to callback URI");

        // Step 5: Exchange code for token
        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new KeyValuePair<string?, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string?, string?>("code", code),
            new KeyValuePair<string?, string?>("redirect_uri", IdPortalApplicationFactory.TestRedirectUri),
            new KeyValuePair<string?, string?>("client_id", IdPortalApplicationFactory.TestClientId),
            new KeyValuePair<string?, string?>("client_secret", IdPortalApplicationFactory.TestClientSecret),
        ]));

        tokenResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        tokenJson.ShouldNotBeNull();
        tokenJson.ShouldContainKey("access_token");
        tokenJson.ShouldContainKey("token_type");
        tokenJson["token_type"]!.ToString()!.ToLowerInvariant().ShouldBe("bearer");
    }

    [Fact(Skip = "TODO: fix")]
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

    private string BuildAuthorizeUrl() =>
        $"/connect/authorize?client_id={IdPortalApplicationFactory.TestClientId}" +
        $"&response_type=code" +
        $"&scope=openid%20email%20profile" +
        $"&redirect_uri={Uri.EscapeDataString(IdPortalApplicationFactory.TestRedirectUri)}";

    private async Task<string?> GetAccessTokenAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var authorizeUrl = BuildAuthorizeUrl();

        var authorizeResponse = await client.GetAsync(authorizeUrl);
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

        var loginResponse = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(loginFields!));
        var afterLoginRedirect = loginResponse.Headers.Location?.ToString();

        if (afterLoginRedirect is null)
        {
            return null;
        }

        string? code = null;
        var maxRedirects = 5;
        var currentUrl = afterLoginRedirect;
        while (maxRedirects-- > 0 && code is null)
        {
            var redirectResponse = await client.GetAsync(currentUrl);
            if (redirectResponse.StatusCode == HttpStatusCode.Found)
            {
                var nextLocation = redirectResponse.Headers.Location?.ToString();
                if (nextLocation is null)
                {
                    break;
                }

                if (nextLocation.StartsWith(IdPortalApplicationFactory.TestRedirectUri, StringComparison.OrdinalIgnoreCase))
                {
                    var codeUri = new Uri(nextLocation);
                    code = System.Web.HttpUtility.ParseQueryString(codeUri.Query)["code"];
                    break;
                }
                currentUrl = nextLocation;
            }
            else
            {
                break;
            }
        }

        if (code is null)
        {
            return null;
        }

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(
        [
            new KeyValuePair<string?, string?>("grant_type", "authorization_code"),
            new KeyValuePair<string?, string?>("code", code),
            new KeyValuePair<string?, string?>("redirect_uri", IdPortalApplicationFactory.TestRedirectUri),
            new KeyValuePair<string?, string?>("client_id", IdPortalApplicationFactory.TestClientId),
            new KeyValuePair<string?, string?>("client_secret", IdPortalApplicationFactory.TestClientSecret),
        ]));

        if (!tokenResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<Dictionary<string, object?>>();
        return tokenJson?["access_token"]?.ToString();
    }
}
