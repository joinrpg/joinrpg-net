using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

internal class CharacteGroupListViewService(IProjectMetadataRepository projectMetadataRepository, ICurrentUserAccessor currentUserAccessor) : ICharacterGroupsClient
{
    private async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId, Func<CharacterGroupDto, bool> predicate)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectId));
        var results = new CharacterGroupListGenerator(projectInfo, currentUserAccessor.UserIdentification).Generate();
        return results.Where(predicate).ToList();
    }

    Task<List<CharacterGroupDto>> ICharacterGroupsClient.GetCharacterGroupsWithSpecial(int projectId) => GetCharacterGroups(projectId, x => true);
    Task<List<CharacterGroupDto>> ICharacterGroupsClient.GetRealCharacterGroups(int projectId) => GetCharacterGroups(projectId, x => !x.IsSpecial);
}
