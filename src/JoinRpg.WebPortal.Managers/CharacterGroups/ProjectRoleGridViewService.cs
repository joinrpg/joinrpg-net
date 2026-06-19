using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

namespace JoinRpg.WebPortal.Managers.CharacterGroups;

internal class ProjectRoleGridViewService(
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor)
    : IProjectRoleGridClient
{
    public async Task<ProjectRoleGridViewModel> GetRoleGrid(ProjectRolesListIdentification id)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
        var config = projectInfo.GetRolesListById(id);

        // Доступ зависит от данных (PublicMode), поэтому проверяется здесь, а не атрибутом.
        if (!config.PublicMode && !projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault))
        {
            throw new NoAccessToProjectException(projectInfo, currentUserAccessor.UserIdOrDefault);
        }

        var groupId = config.CharacterGroupId ?? projectInfo.RootCharacterGroupId;

        var groupIds = projectInfo.GetChildGroupIdsIncludingThis(groupId).ToList();
        var characters = (await projectRepository.GetCharacterByGroups(groupIds))
            .Where(c => c.IsActive)
            .ToList();

        var orderedCharacters = OrderCharacters(characters, projectInfo, groupId);

        var groupName = projectInfo.GetGroupById(groupId.CharacterGroupId).Name;

        var canEditSettings = projectInfo.HasMasterAccess(
            currentUserAccessor.UserIdentificationOrDefault,
            Permission.CanEditRoles);

        return ProjectRoleGridViewModelBuilder.Build(config, groupName, canEditSettings, orderedCharacters, projectInfo);
    }

    /// <summary>
    /// Детерминированный порядок: упорядоченный DFS по группам (ChildGroupsOrdering уже зашит в снепшоте),
    /// внутри каждой группы — прямые персонажи в порядке ChildCharactersOrdering. Персонаж, входящий
    /// в несколько групп, берётся по первому вхождению.
    /// </summary>
    private static IReadOnlyList<Character> OrderCharacters(
        IReadOnlyCollection<Character> characters,
        ProjectInfo projectInfo,
        CharacterGroupIdentification groupId)
    {
        var charactersByGroup = characters
            .SelectMany(c => c.GetDirectGroupIds().Select(g => (group: g, character: c)))
            .ToLookup(x => x.group, x => x.character);

        var result = new List<Character>();
        var seen = new HashSet<int>();
        foreach (var group in projectInfo.GetChildGroupsIncludingThis(groupId))
        {
            var ordered = charactersByGroup[group.Id]
                .OrderByStoredOrder(c => c.CharacterId, group.ChildCharactersOrdering);
            foreach (var character in ordered)
            {
                if (seen.Add(character.CharacterId))
                {
                    result.Add(character);
                }
            }
        }

        return result;
    }
}
