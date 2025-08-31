namespace JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
public interface IKogdaIgraSyncService
{
    Task<SyncStatus> PerformSync();
    Task<SyncStatus> GetSyncStatus();
}

public interface IKogdaIgraBindService
{
    Task UpdateKogdaIgraBindings(ProjectIdentification projectId, KogdaIgraIdentification[] kogdaIgraIdentifications);
}

public interface IKogdaIgraInfoService
{
    Task<KogdaIgraGameInfo[]> GetGames(IReadOnlyCollection<KogdaIgraIdentification> id);
}

public record class SyncStatus(int CountOfGames, DateTimeOffset LastUpdated, int PendingGamesCount) { }
