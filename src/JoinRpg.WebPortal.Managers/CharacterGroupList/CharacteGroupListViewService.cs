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

    async Task<List<CharacterGroupDto>> ICharacterGroupsClient.GetValidParentGroups(CharacterGroupIdentification groupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(groupId.ProjectId);
        var groupInfo = projectInfo.Groups[groupId];
        var excludedIds = groupInfo.AllChildGroupsIncludingThis.ToHashSet();
        var allGroups = await GetCharacterGroups(groupId.ProjectId.Value, x => !x.IsSpecial);
        return allGroups.Where(g => !excludedIds.Contains(g.CharacterGroupId)).ToList();
    }
}
