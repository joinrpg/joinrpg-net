using System.Text.Json;

namespace JoinRpg.Integrations.KogdaIgra;

internal static class ResultParser
{
    internal static KogdaIgraGameUpdateMarker[] ParseGameUpdateMarkers(string strResult)
    {
        var deserializeResult = JsonSerializer.Deserialize<kogda_igra_game_marker[]>(strResult) ?? throw new Exception("Failed to parse JSON");
        return deserializeResult.Select(x => x.ToMarker()).WhereNotNull().ToArray();
    }

    internal static KogdaIgraGameInfo? ParseGameInfo(int gameId, string strResult)
    {
        var parsedResult = JsonSerializer.Deserialize<kogda_igra_game_data>(strResult) ?? throw new Exception("Failed to parse result");
        var ret = new KogdaIgraGameInfo(parsedResult.id, parsedResult.name, strResult, parsedResult.update_date ?? DateTimeOffset.Now);
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

#pragma warning disable IDE1006 // Naming Styles
    private record class kogda_igra_game_data(int id, string name, DateTimeOffset? update_date)
    {

    }

    private record class kogda_igra_game_marker(int id, DateTimeOffset? update_date, string redirect_id)
    {
        internal KogdaIgraGameUpdateMarker? ToMarker()
        {
            if (redirect_id is not null)
            {
                return null;
            }
            if (update_date is null)
            {
                return null;
            }
            try
            {
                return new(id, update_date.Value);
            }
            catch
            {
                // Ignore errors during parsing
                return null;
            }
        }
    }
#pragma warning restore IDE1006 // Naming Styles
}
