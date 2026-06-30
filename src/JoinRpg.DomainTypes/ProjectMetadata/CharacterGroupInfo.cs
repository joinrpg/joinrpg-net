namespace JoinRpg.DomainTypes.ProjectMetadata;

public enum CharacterGroupType
{
    Root,
    Regular,
    SpecialToField,
    SpecialToValue,
}

public record CharacterGroupInfo(
    CharacterGroupIdentification Id,
    string Name,
    bool IsActive,
    bool IsPublic,
    IReadOnlyCollection<CharacterGroupIdentification> DirectChildGroupIds,
    string ChildCharactersOrdering,
    IReadOnlyCollection<CharacterGroupIdentification> DirectParentGroupIds,
    IReadOnlyCollection<CharacterGroupIdentification> AllChildGroups,
    IReadOnlyCollection<CharacterGroupIdentification> AllParentGroups,
    CharacterGroupType GroupType
)
{
    public IReadOnlyList<CharacterGroupIdentification> AllChildGroupsIncludingThis = [Id, .. AllChildGroups];

    public bool IsSpecial => GroupType == CharacterGroupType.SpecialToField || GroupType == CharacterGroupType.SpecialToValue;

    public bool IsRoot => GroupType == CharacterGroupType.Root;

    public bool IsIntresting => (GroupType == CharacterGroupType.Regular || GroupType == CharacterGroupType.SpecialToValue) && IsActive;
}
