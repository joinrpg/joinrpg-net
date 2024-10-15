using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

public class CharacteGroupListViewService(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : ICharacterGroupsClient
{
    public async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId)
    {

        var rootGroup = (await projectRepository.LoadGroupWithTreeSlimAsync(projectId)) ?? throw new JoinRpgEntityNotFoundException(projectId, "project");

        var results = new CharacterGroupListGenerator(rootGroup, currentUserAccessor.UserId).Generate();
        return results;
    }
}
