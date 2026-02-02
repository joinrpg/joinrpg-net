namespace JoinRpg.PrimitiveTypes.ProjectMetadata;

public record class KogdaIgraGameData(
    KogdaIgraIdentification Id,
    string Name,
    DateOnly Begin,
    DateOnly End,
    string RegionName,
    string MasterGroupName,
    Uri? SiteUri
,
    bool IsActive);
