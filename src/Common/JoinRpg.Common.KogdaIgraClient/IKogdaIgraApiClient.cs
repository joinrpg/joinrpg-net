using System.Text.Json.Serialization;

namespace JoinRpg.Common.KogdaIgraClient;

public interface IKogdaIgraApiClient
{
    Task<KogdaIgraGameUpdateMarker[]> GetChangedGamesSince(DateTimeOffset since);
    Task<KogdaIgraGameInfo?> GetGameInfo(int gameId);
}

public record class KogdaIgraGameUpdateMarker(int Id, DateTimeOffset UpdateDate);

public record class KogdaIgraGameInfo(
    int Id,
    string Name,
    string GameData,
    [property: JsonPropertyName("update_date")]
    DateTimeOffset UpdateDate,
    DateOnly Begin,
    DateOnly End,
    string RegionName,
    string RegionShortName,
    string MasterGroupName,
    Uri? SiteUri,
    string? VkClub,
    string? LiveJournalCommunity,
    string? TelegramChannel,
    // На КогдаИгре игра помечена удалённой (deleted_flag = 1), но API по-прежнему отдаёт её данные
    bool IsDeleted,
    // Числовой код статуса игры на КогдаИгре (0=OK, 1=Прошла, 3=Отложена, 5=Отменена и т.д.); null — API его не вернула
    int? Status,
    // Числовой код субрегиона проведения игры (sub_region_id); null — API его не вернула
    int? SubRegionId
    );
