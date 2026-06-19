namespace JoinRpg.DomainTypes.ProjectMetadata;

public record CharacterGroupFullInfo(
    CharacterGroupIdentification Id,
    string Name,
    bool IsRoot,
    bool IsActive,
    bool IsPublic,
    bool IsSpecial,
    bool IsIntresting,
    IReadOnlyCollection<CharacterGroupIdentification> DirectChildGroupIds,
    string ChildCharactersOrdering,
    IReadOnlyCollection<CharacterGroupIdentification> DirectParentGroupIds,
    IReadOnlyCollection<CharacterGroupIdentification> AllChildGroups,
    IReadOnlyCollection<CharacterGroupIdentification> AllParentGroups,
    MarkdownString? Description,
    CreateUpdateMarksInfo Marks
) : CharacterGroupInfo(Id, Name, IsRoot, IsActive, IsPublic, IsSpecial, IsIntresting,
    DirectChildGroupIds, ChildCharactersOrdering, DirectParentGroupIds, AllChildGroups, AllParentGroups);
