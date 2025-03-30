using System.Text.Json;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;

namespace JoinRpg.Integrations.KogdaIgra;

public class KogdaIgraApiClient(HttpClient httpClient) : IKogdaIgraApiClient
{
    private static readonly TimeZoneInfo mskTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
    public async Task<KogdaIgraGameUpdateMarker[]> GetChangedGamesSince(DateTimeOffset since)
    {
        var sinceInMsk = (DateTimeOffset)TimeZoneInfo.ConvertTimeFromUtc(since.UtcDateTime, mskTimeZone);
        // Kogda igra expects MSK unix seconds, not UTC. Need to fix them
        var sinceAsUnixTimestamp = Math.Max(sinceInMsk.ToUnixTimeSeconds(), 0);
        var result = (await httpClient.GetAsync($"api/changed/{sinceAsUnixTimestamp}")).EnsureSuccessStatusCode();
        var strResult = await result.Content.ReadAsStringAsync();

        return ResultParser.ParseGameUpdateMarkers(strResult);
    }

    public async Task<KogdaIgraGameInfo> GetGameInfo(int gameId)
    {
        var result = (await httpClient.GetAsync($"api/game/{gameId}")).EnsureSuccessStatusCode();
        var strResult = await result.Content.ReadAsStringAsync();
        var parsedResult = JsonSerializer.Deserialize<KogdaIgraGameInfo>(strResult) ?? throw new Exception("Failed to parse result");
        return parsedResult with { GameData = strResult };
    }
}
