namespace JoinRpg.DomainTypes.ProjectMetadata;

public record CharacterGroupFullInfo(
    CharacterGroupIdentification Id,
    string Name,
    bool IsActive,
    bool IsPublic,
    IReadOnlyCollection<CharacterGroupIdentification> DirectChildGroupIds,
    string ChildCharactersOrdering,
    IReadOnlyCollection<CharacterGroupIdentification> DirectParentGroupIds,
    IReadOnlyCollection<CharacterGroupIdentification> AllChildGroups,
    IReadOnlyCollection<CharacterGroupIdentification> AllParentGroups,
    CharacterGroupType GroupType,
    UserIdentification? ResponsibleMasterId,
    int DirectChildCharactersCount,
    MarkdownString? Description,
    CreateUpdateMarksInfo Marks
) : CharacterGroupInfo(Id, Name, IsActive, IsPublic, DirectChildGroupIds, ChildCharactersOrdering, DirectParentGroupIds, AllChildGroups, AllParentGroups, GroupType, ResponsibleMasterId)

{
    public CharacterGroupFullInfo(CharacterGroupInfo groupInfo, int directChildCharactersCount,
    MarkdownString? description,
    CreateUpdateMarksInfo marks) : this(
            groupInfo.Id, groupInfo.Name, groupInfo.IsActive, groupInfo.IsPublic,
            groupInfo.DirectChildGroupIds, groupInfo.ChildCharactersOrdering, groupInfo.DirectParentGroupIds,
            groupInfo.AllChildGroups, groupInfo.AllParentGroups,
            groupInfo.GroupType,
            groupInfo.ResponsibleMasterId,
            directChildCharactersCount, description, marks)
    {

    }
}
