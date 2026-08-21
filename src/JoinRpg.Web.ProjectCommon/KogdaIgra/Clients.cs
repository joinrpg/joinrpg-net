namespace JoinRpg.Web.ProjectCommon.KogdaIgra;

public interface IKogdaIgraSyncClient
{
    /// <summary>
    /// Initiates fetch and resync from kogda-igra.
    /// Returns number of kogda-igra games in database after sync
    /// </summary>
    Task<ResyncOperationResultsViewModel> ResyncKograIgra();

    Task<SyncStatusViewModel> GetSyncStatus();

    Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates();

    Task<KogdaIgraShortViewModel[]> GetFutureKogdaIgraCandidates();

    Task<KogdaIgraShortViewModel[]> GetKogdaIgraNotUpdated();

    Task<KogdaIgraCardViewModel[]> GetKogdaIgraCards(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIds);

    Task<ResyncOperationResultsViewModel> ForceResyncGames(KogdaIgraIdentification[] gameIds);

    Task<ResyncOperationResultsViewModel> ScheduleUpdateAllGames();

    Task<ResyncOperationResultsViewModel> RunSyncJob();
}

public interface IKogdaIgraBindClient
{
    Task UpdateProjectKogdaIgraBindings(KogdaIgraBindViewModel command);
}
