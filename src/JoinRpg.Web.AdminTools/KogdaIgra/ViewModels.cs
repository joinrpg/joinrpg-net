using JoinRpg.DataModel;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.AdminTools.KogdaIgra;
public record class JoinRpgSyncCandidateViewModel(
    ProjectIdentification ProjectId, string Name, UserLinkViewModel[] Masters, DateTimeOffset LastUpdatedAt, MarkdownString Description);

public record class KogdaIgraCardViewModel(
    Uri KogdaIgraUri,
    string Name,
    DateTimeOffset Begin,
    DateTimeOffset End,
    string RegionName,
    string MasterGroupName,
    Uri SiteUri);
public record class KogdaIgraBindViewModel(
    ProjectIdentification ProjectId,
    KogdaIgraIdentification[] KogdaIgraIdentifications);

public record class KogdaIgraShortViewModel(KogdaIgraIdentification KogdaIgraId, string Name, Uri KogdaIgraLink);

public record class ResyncOperationResultsViewModel(bool OperationSuccessful, string OperationStatusMessage, SyncStatusViewModel SyncStatus);

public record class SyncStatusViewModel(int CountOfGames, DateTimeOffset LastUpdated, int PendingGamesCount);
