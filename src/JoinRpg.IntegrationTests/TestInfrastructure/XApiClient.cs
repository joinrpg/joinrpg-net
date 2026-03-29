using System.Net.Http.Headers;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Typed HTTP client for XApi and XGameApi endpoints.
/// All methods throw HttpRequestException on non-success status codes.
/// </summary>
public class XApiClient(HttpClient httpClient)
{
    /// <summary>POST /x-api/token — get JWT token</summary>
    public async Task<AuthenticationResponse> LoginAsync(string username, string password)
    {
        var response = await httpClient.PostAsync("/x-api/token",
            new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string?, string?>("username", username),
                new KeyValuePair<string?, string?>("password", password),
                new KeyValuePair<string?, string?>("grant_type", "password"),
            }));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthenticationResponse>())!;
    }

    /// <summary>GET /x-api/me/projects/active — active projects for current user</summary>
    public async Task<IEnumerable<ProjectHeader>> GetActiveProjectsAsync()
    {
        var response = await httpClient.GetAsync("/x-api/me/projects/active");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IEnumerable<ProjectHeader>>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/metadata/fields — project fields metadata</summary>
    public async Task<ProjectFieldsMetadata> GetFieldsMetadataAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"/x-game-api/{projectId}/metadata/fields");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectFieldsMetadata>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/characters — character list</summary>
    public async Task<IEnumerable<CharacterHeader>> GetCharactersAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"/x-game-api/{projectId}/characters");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IEnumerable<CharacterHeader>>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/characters/{characterId}/ — character details</summary>
    public async Task<CharacterInfo> GetCharacterAsync(int projectId, int characterId)
    {
        var response = await httpClient.GetAsync($"/x-game-api/{projectId}/characters/{characterId}/");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CharacterInfo>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/checkin/allclaims — claims ready for check-in</summary>
    public async Task<IEnumerable<ClaimHeaderInfo>> GetCheckInClaimsAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"/x-game-api/{projectId}/checkin/allclaims");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IEnumerable<ClaimHeaderInfo>>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/checkin/stat — check-in statistics</summary>
    public async Task<CheckInStats> GetCheckInStatsAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"/x-game-api/{projectId}/checkin/stat");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckInStats>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/claims/{claimId} — claim details</summary>
    public async Task<ClaimInfo> GetClaimAsync(int projectId, int claimId)
    {
        var response = await httpClient.GetAsync($"/x-game-api/{projectId}/claims/{claimId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClaimInfo>())!;
    }

    /// <summary>POST /x-game-api/{projectId}/characters — create new character</summary>
    public async Task<CharacterHeader> CreateCharacterAsync(int projectId, CreateCharacterRequest request)
    {
        var response = await httpClient.PostAsJsonAsync($"/x-game-api/{projectId}/characters", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CharacterHeader>())!;
    }

    /// <summary>POST /x-game-api/{projectId}/characters/{characterId}/fields — set character fields</summary>
    public async Task SetCharacterFieldsAsync(int projectId, int characterId, Dictionary<int, object?> fieldValues)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"/x-game-api/{projectId}/characters/{characterId}/fields", fieldValues);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>POST /x-game-api/{projectId}/characters/{characterId}/fields — set character fields, returns response for status checking</summary>
    public async Task<HttpResponseMessage> SetCharacterFieldsRawAsync(int projectId, int characterId, Dictionary<int, object?> fieldValues)
    {
        return await httpClient.PostAsJsonAsync(
            $"/x-game-api/{projectId}/characters/{characterId}/fields", fieldValues);
    }

    public static async Task<XApiClient> CreateXApiClient(HttpClient httpClient, string masterEmail, string masterPassword)
    {
        var xapi = new XApiClient(httpClient);

        // Get JWT token
        var authResponse = await xapi.LoginAsync(masterEmail, masterPassword);

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.access_token);
        return xapi;
    }
}
