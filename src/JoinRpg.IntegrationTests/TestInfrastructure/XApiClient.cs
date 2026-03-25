using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// TypedClient for XApi/XGameApi endpoints.
/// All methods throw <see cref="HttpRequestException"/> on non-success status codes.
/// </summary>
public class XApiClient(HttpClient httpClient)
{
    /// <summary>POST /x-api/token — obtain JWT token</summary>
    public async Task<AuthenticationResponse> GetTokenAsync(string username, string password)
    {
        var response = await httpClient.PostAsync("x-api/token",
            new FormUrlEncodedContent(
            [
                new KeyValuePair<string?, string?>("username", username),
                new KeyValuePair<string?, string?>("password", password),
                new KeyValuePair<string?, string?>("grant_type", "password"),
            ]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthenticationResponse>())!;
    }

    /// <summary>GET /x-api/me/projects/active — active projects for current user</summary>
    public async Task<IReadOnlyList<ProjectHeader>> GetMyActiveProjectsAsync()
    {
        var response = await httpClient.GetAsync("x-api/me/projects/active");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ProjectHeader>>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/metadata/fields — project field metadata</summary>
    public async Task<ProjectFieldsMetadata> GetFieldsMetadataAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"x-game-api/{projectId}/metadata/fields");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectFieldsMetadata>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/characters — character list</summary>
    public async Task<IReadOnlyList<CharacterHeader>> GetCharactersAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"x-game-api/{projectId}/characters");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<CharacterHeader>>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/characters/{characterId}/ — character details</summary>
    public async Task<CharacterInfo> GetCharacterAsync(int projectId, int characterId)
    {
        var response = await httpClient.GetAsync($"x-game-api/{projectId}/characters/{characterId}/");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CharacterInfo>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/checkin/allclaims — claims ready for check-in</summary>
    public async Task<IReadOnlyList<ClaimHeaderInfo>> GetCheckInClaimsAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"x-game-api/{projectId}/checkin/allclaims");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ClaimHeaderInfo>>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/checkin/stat — check-in statistics</summary>
    public async Task<CheckInStats> GetCheckInStatAsync(int projectId)
    {
        var response = await httpClient.GetAsync($"x-game-api/{projectId}/checkin/stat");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckInStats>())!;
    }

    /// <summary>GET /x-game-api/{projectId}/claims/{claimId} — claim details</summary>
    public async Task<ClaimInfo> GetClaimAsync(int projectId, int claimId)
    {
        var response = await httpClient.GetAsync($"x-game-api/{projectId}/claims/{claimId}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClaimInfo>())!;
    }

    /// <summary>GET /x-game-api/schedule/projects/active — projects with schedules</summary>
    public async Task<IReadOnlyList<ProjectHeader>> GetActiveScheduleProjectsAsync()
    {
        var response = await httpClient.GetAsync("x-game-api/schedule/projects/active");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<List<ProjectHeader>>())!;
    }

    /// <summary>Returns the raw HttpResponseMessage without throwing on error status codes.</summary>
    public Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage request)
        => httpClient.SendAsync(request);
}
