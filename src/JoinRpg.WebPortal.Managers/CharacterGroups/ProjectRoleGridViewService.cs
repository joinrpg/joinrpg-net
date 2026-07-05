using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.Models;

namespace JoinRpg.WebPortal.Managers.CharacterGroups;

internal class ProjectRoleGridViewService(
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor)
    : IProjectRoleGridClient
{
    public async Task<ProjectRoleGridViewResult> GetRoleGrid(ProjectRolesListIdentification id)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(id.ProjectId);
        var config = projectInfo.GetRolesListById(id);

        // Доступ зависит от данных (PublicMode), поэтому проверяется здесь, а не атрибутом.
        // Возвращаем результат «нет доступа» (а не исключение), чтобы остров показал панель.
        if (!config.PublicMode && !projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault))
        {
            return new ProjectRoleGridViewResult(HasAccess: false, Grid: null, NoAccess: NoAccessToProjectViewModelBuilder.Build(projectInfo));
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

        // Скрытое (приватные персонажи и скрытые игроки) видит только мастер.
        // На публичной сетке не-мастер/аноним их не увидит.
        var canViewPrivate = projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault);

        var grid = ProjectRoleGridViewModelBuilder.Build(config, groupName, canEditSettings, canViewPrivate, orderedCharacters, projectInfo);
        return new ProjectRoleGridViewResult(HasAccess: true, Grid: grid, NoAccess: null);
    }

    /// <summary>
    /// Детерминированный порядок: упорядоченный DFS по группам (ChildGroupsOrdering уже зашит в снепшоте),
    /// внутри каждой группы — прямые персонажи в порядке ChildCharactersOrdering. Персонаж, входящий
    /// в несколько групп, берётся по первому вхождению.
    /// </summary>
    private static List<Character> OrderCharacters(
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
