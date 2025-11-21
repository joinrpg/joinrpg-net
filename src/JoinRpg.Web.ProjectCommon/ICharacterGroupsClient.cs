namespace JoinRpg.Web.ProjectCommon;

public interface ICharacterGroupsClient
{
    Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId);
    Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId);
}
