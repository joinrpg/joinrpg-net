namespace JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
public interface IKogdaIgraSyncService
{
    Task<SyncStatus> PerformSync();
    Task<SyncStatus> GetSyncStatus();
}

public record class SyncStatus(int CountOfGames, DateTimeOffset LastUpdated, int PendingGamesCount) { }
