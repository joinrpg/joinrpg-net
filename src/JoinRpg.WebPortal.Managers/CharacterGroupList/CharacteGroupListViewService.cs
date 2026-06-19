using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Managers.CharacterGroupList;

internal class CharacteGroupListViewService(IProjectMetadataRepository projectMetadataRepository, ICurrentUserAccessor currentUserAccessor) : ICharacterGroupsClient
{
    private List<CharacterGroupDto> GenerateGroups(ProjectInfo projectInfo, Func<CharacterGroupDto, bool> predicate)
    {
        var results = new CharacterGroupListGenerator(projectInfo, currentUserAccessor.UserIdentification).Generate();
        return results.Where(predicate).ToList();
    }

    private async Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId, Func<CharacterGroupDto, bool> predicate)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new ProjectIdentification(projectId));
        return GenerateGroups(projectInfo, predicate);
    }

    Task<List<CharacterGroupDto>> ICharacterGroupsClient.GetCharacterGroupsWithSpecial(int projectId) => GetCharacterGroups(projectId, x => true);
    Task<List<CharacterGroupDto>> ICharacterGroupsClient.GetRealCharacterGroups(int projectId) => GetCharacterGroups(projectId, x => !x.IsSpecial);

    async Task<List<CharacterGroupDto>> ICharacterGroupsClient.GetValidParentGroups(CharacterGroupIdentification groupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(groupId.ProjectId);
        var groupInfo = projectInfo.Groups[groupId];
        var excludedIds = groupInfo.AllChildGroupsIncludingThis.ToHashSet();
        return GenerateGroups(projectInfo, x => !x.IsSpecial && !excludedIds.Contains(x.CharacterGroupId));
    }
}
