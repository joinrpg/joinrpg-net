namespace JoinRpg.DomainTypes.ProjectMetadata;

public record CharacterGroupInfo(
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
    IReadOnlyCollection<CharacterGroupIdentification> AllParentGroups
)
{
    public IReadOnlyList<CharacterGroupIdentification> AllChildGroupsIncludingThis = [Id, .. AllChildGroups];
}
