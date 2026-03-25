using System.Net.Http.Json;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Typed HTTP client for the XApi/XGameApi endpoints.
/// </summary>
public class XApiClient(HttpClient httpClient)
{
    // ── /x-api/token ────────────────────────────────────────────────────────

    public Task<HttpResponseMessage> PostLoginRawAsync(
        string username,
        string password,
        string grantType = "password")
        => httpClient.PostAsync("/x-api/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password,
                ["grant_type"] = grantType,
            }));

    public async Task<AuthenticationResponse> LoginAsync(string username, string password)
    {
        var response = await PostLoginRawAsync(username, password);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthenticationResponse>())!;
    }

    // ── /x-api/me/projects/active ────────────────────────────────────────────

    public Task<HttpResponseMessage> GetActiveProjectsRawAsync()
        => httpClient.GetAsync("/x-api/me/projects/active");

    public async Task<IEnumerable<ProjectHeader>> GetActiveProjectsAsync()
    {
        var response = await GetActiveProjectsRawAsync();
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IEnumerable<ProjectHeader>>())!;
    }

    // ── /x-game-api/{projectId}/metadata/fields ──────────────────────────────

    public Task<HttpResponseMessage> GetFieldsMetadataRawAsync(int projectId)
        => httpClient.GetAsync($"/x-game-api/{projectId}/metadata/fields");

    public async Task<ProjectFieldsMetadata> GetFieldsMetadataAsync(int projectId)
    {
        var response = await GetFieldsMetadataRawAsync(projectId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectFieldsMetadata>())!;
    }

    // ── /x-game-api/{projectId}/characters ───────────────────────────────────

    public Task<HttpResponseMessage> GetCharactersRawAsync(int projectId)
        => httpClient.GetAsync($"/x-game-api/{projectId}/characters/");

    public async Task<IEnumerable<CharacterHeader>> GetCharactersAsync(int projectId)
    {
        var response = await GetCharactersRawAsync(projectId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IEnumerable<CharacterHeader>>())!;
    }

    public Task<HttpResponseMessage> GetCharacterRawAsync(int projectId, int characterId)
        => httpClient.GetAsync($"/x-game-api/{projectId}/characters/{characterId}/");

    // ── /x-game-api/{projectId}/checkin ──────────────────────────────────────

    public Task<HttpResponseMessage> GetClaimsForCheckInRawAsync(int projectId)
        => httpClient.GetAsync($"/x-game-api/{projectId}/checkin/allclaims");

    public async Task<IEnumerable<ClaimHeaderInfo>> GetClaimsForCheckInAsync(int projectId)
    {
        var response = await GetClaimsForCheckInRawAsync(projectId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IEnumerable<ClaimHeaderInfo>>())!;
    }

    public Task<HttpResponseMessage> GetCheckInStatRawAsync(int projectId)
        => httpClient.GetAsync($"/x-game-api/{projectId}/checkin/stat");

    public async Task<CheckInStats> GetCheckInStatAsync(int projectId)
    {
        var response = await GetCheckInStatRawAsync(projectId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckInStats>())!;
    }

    // ── /x-game-api/{projectId}/claims/{claimId} ─────────────────────────────

    public Task<HttpResponseMessage> GetClaimRawAsync(int projectId, int claimId)
        => httpClient.GetAsync($"/x-game-api/{projectId}/claims/{claimId}");
}
