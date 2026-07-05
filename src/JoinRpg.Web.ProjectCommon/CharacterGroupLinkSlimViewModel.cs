namespace JoinRpg.Web.ProjectCommon;

public record CharacterGroupLinkSlimViewModel(CharacterGroupIdentification CharacterGroupId, string Name, bool IsPublic, bool IsActive)
{
    public CharacterGroupLinkSlimViewModel(CharacterGroupInfo groupInfo) : this(groupInfo.Id, groupInfo.Name, groupInfo.IsPublic, groupInfo.IsActive)
    {

    }
}
