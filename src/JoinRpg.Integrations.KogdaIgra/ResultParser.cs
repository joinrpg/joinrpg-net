using System.Text.Json;
using System.Text.Json.Serialization;

namespace JoinRpg.Integrations.KogdaIgra;

internal static class ResultParser
{
    private static JsonSerializerOptions serializerOptions = new() { NumberHandling = JsonNumberHandling.AllowReadingFromString };
    internal static KogdaIgraGameUpdateMarker[] ParseGameUpdateMarkers(string strResult)
    {
        var deserializeResult = JsonSerializer.Deserialize<kogda_igra_game_marker[]>(strResult) ?? throw new Exception("Failed to parse JSON");
        return deserializeResult.Select(x => x.ToMarker()).WhereNotNull().ToArray();
    }

    internal static KogdaIgraGameInfo ParseGameInfo(string strResult)
    {
        var parsedResult = JsonSerializer.Deserialize<kogda_igra_game_data>(strResult, serializerOptions) ?? throw new Exception("Failed to parse result");
        var ret = new KogdaIgraGameInfo(
            parsedResult.id,
            parsedResult.name,
            strResult,
            parsedResult.update_date ?? DateTimeOffset.Now,
            parsedResult.begin,
            parsedResult.begin.AddDays(parsedResult.time),
            parsedResult.sub_region_disp_name,
            parsedResult.mg,
            parsedResult.uri);
        return ret;
    }

#pragma warning disable IDE1006 // Naming Styles
    private record class kogda_igra_game_data(int id, string name, DateTimeOffset? update_date,
        DateOnly begin, int time, string mg, string sub_region_disp_name, string uri)
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
