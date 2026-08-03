namespace JoinRpg.DomainTypes.ProjectMetadata;

public record class KogdaIgraGameData(
    KogdaIgraIdentification Id,
    string Name,
    DateOnly Begin,
    DateOnly End,
    string RegionName,
    string MasterGroupName,
    Uri? SiteUri
,
    bool IsActive,
    VkId? VkClub = null,
    LiveJournalId? LjComm = null,
    string? TelegramChannel = null);
