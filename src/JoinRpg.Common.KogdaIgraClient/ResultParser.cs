using System.Text.Json;
using System.Text.Json.Serialization;

namespace JoinRpg.Common.KogdaIgraClient;

internal static class ResultParser
{
    private static readonly JsonSerializerOptions serializerOptions = new() { NumberHandling = JsonNumberHandling.AllowReadingFromString };
    internal static KogdaIgraGameUpdateMarker[] ParseGameUpdateMarkers(string strResult)
    {
        var deserializeResult = JsonSerializer.Deserialize<kogda_igra_game_marker[]>(strResult) ?? throw new Exception("Failed to parse JSON");
        return [.. deserializeResult.Select(x => x.ToMarker()).WhereNotNull()];
    }

    internal static KogdaIgraGameInfo? TryParseGameInfo(string strResult)
    {
        var parsedResult = JsonSerializer.Deserialize<kogda_igra_game_data>(strResult, serializerOptions) ?? throw new Exception("Failed to parse result");
        if (parsedResult.id is 0 || parsedResult.time is 0)
        {
            return null;
        }
        Uri? siteUri;
        if (string.IsNullOrWhiteSpace(parsedResult.uri))
        {
            siteUri = null;
        }
        else if (!Uri.TryCreate(parsedResult.uri, UriKind.Absolute, out siteUri))
        {
            return null;
        }
        if (siteUri is not null && siteUri.Scheme != "http" && siteUri.Scheme != "https")
        {
            return null;
        }
        var ret = new KogdaIgraGameInfo(
            parsedResult.id,
            parsedResult.name,
            strResult,
            parsedResult.update_date ?? DateTimeOffset.Now,
            parsedResult.begin,
            parsedResult.begin.AddDays(parsedResult.time - 1),
            parsedResult.sub_region_disp_name,
            parsedResult.sub_region_name,
            parsedResult.mg,
            siteUri);
        return ret;
    }

#pragma warning disable IDE1006 // Naming Styles
    private record class kogda_igra_game_data(int id, string name, DateTimeOffset? update_date,
        DateOnly begin, int time, string mg, string sub_region_disp_name, string uri, string sub_region_name)
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
