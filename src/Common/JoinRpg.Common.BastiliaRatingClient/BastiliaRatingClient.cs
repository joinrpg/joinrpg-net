using System.Net.Http.Json;

namespace JoinRpg.Common.BastiliaRatingClient;

internal class BastiliaRatingClient(HttpClient httpClient) : IBastiliaRatingClient
{
    public async Task<int[]> GetBastiliaActualMembers()
    {
        return (await httpClient.GetFromJsonAsync<int[]>($"/api/members/actual"))!;
    }
    public async Task UpdateKogdaIgra(int gameId)
    {
        (await httpClient.GetAsync($"/api/kogda-igra/add/{gameId}")).EnsureSuccessStatusCode();
    }
}
