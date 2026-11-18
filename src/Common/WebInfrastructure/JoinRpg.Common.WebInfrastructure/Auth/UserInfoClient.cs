using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace JoinRpg.Common.WebInfrastructure.Auth;

internal sealed class UserInfoClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<Claim>> GetUserInfoClaimsAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var claims = new List<Claim>();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var value = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => property.Value.GetRawText(),
                _ => null,
            };
            if (value is not null)
            {
                claims.Add(new Claim(property.Name, value));
            }
        }

        return claims;
    }
}
