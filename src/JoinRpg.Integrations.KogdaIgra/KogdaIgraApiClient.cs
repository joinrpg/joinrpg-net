namespace JoinRpg.Integrations.KogdaIgra;

public class KogdaIgraApiClient(HttpClient httpClient) : IKogdaIgraApiClient
{
    private static readonly TimeZoneInfo mskTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
    public async Task<KogdaIgraGameUpdateMarker[]> GetChangedGamesSince(DateTimeOffset since)
    {
        var sinceAsUnixTimestamp = ConvertToKogdaIgraTimeStamp(since);

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

    //For tests
    internal static long ConvertToKogdaIgraTimeStamp(DateTimeOffset since)
    {
        // Когда будешь править этот метод, помни, что у тебя на компьютере одна таймзона, а на сервере — другая.
        var sinceInMsk = TimeZoneInfo.ConvertTime(since, mskTimeZone);
        // Kogda igra expects MSK unix seconds, not UTC. Need to fix them
        return Math.Max(sinceInMsk.ToUnixTimeSeconds(), 0);
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
        var ret = ResultParser.ParseGameInfo(strResult);
        if (ret.Id == 0)
        {
            return null;
        }
        if (ret.Id != gameId && string.IsNullOrWhiteSpace(ret.Name))
        {
            throw new KogdaIgraParseException(gameId, "Incorrect record");
        }
        return ret;
    }
}
