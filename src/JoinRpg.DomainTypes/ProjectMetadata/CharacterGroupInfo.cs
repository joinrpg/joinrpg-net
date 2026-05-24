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
    IReadOnlyCollection<CharacterGroupIdentification> DirectParentGroupIds,
    IReadOnlyCollection<CharacterGroupIdentification> AllChildGroups,
    IReadOnlyCollection<CharacterGroupIdentification> AllParentGroups
)
{
    public IReadOnlyCollection<CharacterGroupIdentification> AllChildGroupsIncludingThis = [Id, .. AllChildGroups];
}
