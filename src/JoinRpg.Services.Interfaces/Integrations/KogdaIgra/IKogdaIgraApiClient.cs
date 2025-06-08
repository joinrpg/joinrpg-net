using System.Text.Json.Serialization;

namespace JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
public interface IKogdaIgraApiClient
{
    Task<KogdaIgraGameUpdateMarker[]> GetChangedGamesSince(DateTimeOffset since);
    Task<KogdaIgraGameInfo?> GetGameInfo(int gameId);
}

public record class KogdaIgraGameUpdateMarker(int Id, DateTimeOffset UpdateDate)
{ }

public record class KogdaIgraGameInfo(int Id, string Name, string GameData,
    [property: JsonPropertyName("update_date")]
    DateTimeOffset UpdateDate)
{ }
