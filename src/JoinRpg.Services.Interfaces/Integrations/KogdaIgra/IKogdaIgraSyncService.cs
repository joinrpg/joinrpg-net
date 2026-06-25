namespace JoinRpg.Services.Interfaces.Integrations.KogdaIgra;

public interface IKogdaIgraSyncService
{
    Task<SyncStatus> PerformSync();
    Task<SyncStatus> GetSyncStatus();
    Task<SyncStatus> ForceResyncGames(KogdaIgraIdentification[] gameIds);
}

public interface IKogdaIgraBindService
{
    Task UpdateKogdaIgraBindings(ProjectIdentification projectId, KogdaIgraIdentification[] kogdaIgraIdentifications, bool DisableKogdaIgraMapping);
}

public record class SyncStatus(int CountOfGames, DateTimeOffset LastUpdated, int PendingGamesCount) { }
