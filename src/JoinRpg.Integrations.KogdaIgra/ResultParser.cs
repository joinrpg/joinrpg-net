using System.Text.Json;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces.Integrations.KogdaIgra;

namespace JoinRpg.Integrations.KogdaIgra;

internal static class ResultParser
{
    internal static KogdaIgraGameUpdateMarker[] ParseGameUpdateMarkers(string strResult)
    {
        var deserializeResult = JsonSerializer.Deserialize<kogda_igra_game_marker[]>(strResult) ?? throw new Exception("Failed to parse JSON");
        return deserializeResult.Select(x => x.ToMarker()).WhereNotNull().ToArray();
    }

    private record class kogda_igra_game_marker(string id, string update_date, string redirect_id)
    {
        internal KogdaIgraGameUpdateMarker? ToMarker()
        {
            if (redirect_id is not null)
            {
                return null;
            }
            try
            {
                return new(int.Parse(id), DateTimeOffset.Parse(update_date));
            }
            catch
            {
                // Ignore errors during parsing
                return null;
            }
        }
    }
}
