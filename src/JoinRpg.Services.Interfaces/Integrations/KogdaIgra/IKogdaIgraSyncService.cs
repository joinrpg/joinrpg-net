namespace JoinRpg.Services.Interfaces.Integrations.KogdaIgra;
public interface IKogdaIgraSyncService
{
    Task PerformSync();
    Task<SyncStatus> GetSyncStatus();
}

public record class SyncStatus(int CountOfGames, DateTimeOffset LastUpdated) { }
