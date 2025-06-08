namespace JoinRpg.Integrations.KogdaIgra;

public class KogdaIgraApiClient(HttpClient httpClient) : IKogdaIgraApiClient
{
    private static readonly TimeZoneInfo mskTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
    public async Task<KogdaIgraGameUpdateMarker[]> GetChangedGamesSince(DateTimeOffset since)
    {
        var sinceInMsk = (DateTimeOffset)TimeZoneInfo.ConvertTimeFromUtc(since.UtcDateTime, mskTimeZone);
        // Kogda igra expects MSK unix seconds, not UTC. Need to fix them
        var sinceAsUnixTimestamp = Math.Max(sinceInMsk.ToUnixTimeSeconds(), 0);

        HttpResponseMessage result;
        try
        {
            result = (await httpClient.GetAsync($"api/changed/{sinceAsUnixTimestamp}")).EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            throw new KogdaIgraConnectException(e);
        }
        var strResult = await result.Content.ReadAsStringAsync();

        return ResultParser.ParseGameUpdateMarkers(strResult);
    }

    public async Task<KogdaIgraGameInfo?> GetGameInfo(int gameId)
    {
        HttpResponseMessage result;
        try
        {
            result = (await httpClient.GetAsync($"api/game/{gameId}")).EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            throw new KogdaIgraConnectException(e);
        }
        var strResult = await result.Content.ReadAsStringAsync();
        return ResultParser.ParseGameInfo(gameId, strResult);
    }
}
