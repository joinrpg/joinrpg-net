using JoinRpg.Common.WebComponents;

namespace JoinRpg.Web.ProjectCommon.KogdaIgra;

public record class JoinRpgSyncCandidateViewModel(
    ProjectIdentification ProjectId, string Name, UserLinkViewModel[] Masters, DateTimeOffset LastUpdatedAt, MarkdownString? Description);

public record class KogdaIgraBindViewModel(
    ProjectIdentification ProjectId,
    KogdaIgraIdentification[] KogdaIgraIdentifications,
    bool DisableKogdaIgraMapping);

public record class KogdaIgraShortViewModel(KogdaIgraIdentification KogdaIgraId, string Name, Uri KogdaIgraLink, int? Year);

public record class KogdaIgraCardViewModel(
    KogdaIgraIdentification KogdaIgraId,
    Uri KogdaIgraUri,
    string Name,
    DateOnly Begin,
    DateOnly End,
    string RegionName,
    string MasterGroupName, Uri? SiteUri,
    VkId? Vk = null,
    LiveJournalId? LiveJournal = null,
    string? TelegramChannel = null);

public record class ResyncOperationResultsViewModel(bool OperationSuccessful, string OperationStatusMessage, SyncStatusViewModel SyncStatus);

public record class SyncStatusViewModel(int CountOfGames, DateTimeOffset LastUpdated, int PendingGamesCount);
