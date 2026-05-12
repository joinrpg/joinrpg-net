namespace JoinRpg.DomainTypes.ProjectMetadata;

public record CharacterGroupInfo(
    CharacterGroupIdentification Id,
    string Name,
    bool IsRoot,
    bool IsActive,
    bool IsPublic,
    bool IsSpecial,
    IReadOnlyCollection<CharacterGroupIdentification> ChildGroupIds,
    IReadOnlyCollection<CharacterGroupIdentification> ParentGroupIds
);
