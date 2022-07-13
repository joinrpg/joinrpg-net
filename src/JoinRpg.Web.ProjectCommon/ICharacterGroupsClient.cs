namespace JoinRpg.Web.ProjectCommon;

public interface ICharacterGroupsClient
{
    Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId);
}
