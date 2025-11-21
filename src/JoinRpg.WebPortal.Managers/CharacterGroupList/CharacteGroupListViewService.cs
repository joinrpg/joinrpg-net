using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

internal class CharacteGroupListViewService(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : ICharacterGroupsClient
{
    private async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId, Predicate<CharacterGroupDto> predicate)
    {

        var rootGroup = (await projectRepository.LoadGroupWithTreeSlimAsync(projectId)) ?? throw new JoinRpgEntityNotFoundException(projectId, "project");

        var results = new CharacterGroupListGenerator(rootGroup, currentUserAccessor.UserId).Generate();
        return results;
    }

    Task<List<CharacterGroupDto>> ICharacterGroupsClient.GetCharacterGroupsWithSpecial(int projectId) => GetCharacterGroups(projectId, x => true);
    Task<List<CharacterGroupDto>> ICharacterGroupsClient.GetRealCharacterGroups(int projectId) => GetCharacterGroups(projectId, x => !x.IsSpecial);
}
