using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.AdminTools.KogdaIgra;

public interface IKogdaIgraSyncClient
{
    /// <summary>
    /// Initiates fetch and resync from kogda-igra.
    /// Returns number of kogda-igra games in database after sync
    /// </summary>
    Task<ResyncOperationResultsViewModel> ResyncKograIgra();

    Task<SyncStatusViewModel> GetSyncStatus();

    Task<KogdaIgraShortViewModel[]> GetKogdaIgraCandidates();

    Task<KogdaIgraShortViewModel[]> GetKogdaIgraNotUpdated();

    Task<KogdaIgraCardViewModel[]> GetKogdaIgraCards(IReadOnlyCollection<KogdaIgraIdentification> kogdaIgraIds);
}

public interface IKogdaIgraBindClient
{
    Task UpdateProjectKogdaIgraBindings(KogdaIgraBindViewModel command);
}
