namespace JoinRpg.Services.Interfaces.Integrations.KogdaIgra;

public interface IKogdaIgraSyncService
{
    Task<SyncStatus> PerformSync();
    Task<SyncStatus> GetSyncStatus();
}

public interface IKogdaIgraBindService
{
    Task UpdateKogdaIgraBindings(ProjectIdentification projectId, KogdaIgraIdentification[] kogdaIgraIdentifications, bool DisableKogdaIgraMapping);
}

public interface IKogdaIgraInfoService
{
    Task<KogdaIgraGameData[]> GetGames(IReadOnlyCollection<KogdaIgraIdentification> id);
}

public record class SyncStatus(int CountOfGames, DateTimeOffset LastUpdated, int PendingGamesCount) { }

public record class KogdaIgraGameData(
    int Id,
    string Name,
    DateTimeOffset UpdateDate,
    DateOnly Begin,
    DateOnly End,
    string RegionName,
    string MasterGroupName,
    Uri? SiteUri
, bool IsActive);
