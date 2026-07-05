using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.Models;

namespace JoinRpg.WebPortal.Managers.CharacterGroups;

internal class ProjectRoleGridViewService(
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterGroupRepository characterGroupRepository,
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

        var orderedGroups = projectInfo.GetChildGroupsIncludingThis(groupId).ToList();

        // Скрытое (приватные персонажи и скрытые игроки) видит только мастер.
        var canViewPrivate = projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault);
        var visibleCharacters = canViewPrivate ? characters : characters.Where(c => c.IsPublic).ToList();

        var charactersByGroup = visibleCharacters
            .SelectMany(c => c.GetDirectGroupIds().Select(g => (group: g, character: c)))
            .ToLookup(x => x.group, x => x.character);

        var groupFullInfos =
            config.ShowCharacterGroups
                ? (await characterGroupRepository.GetCharacterGroupsFullInfo([.. orderedGroups.Select(g => g.Id)]))
                    .ToDictionary(g => g.Id)
                : [];

        var groupName = projectInfo.GetGroupById(groupId.CharacterGroupId).Name;

        var canEditSettings = projectInfo.HasMasterAccess(
            currentUserAccessor.UserIdentificationOrDefault,
            Permission.CanEditRoles);

        return new ProjectRoleGridViewResult(
            HasAccess: true,
            Grid: ProjectRoleGridViewModelBuilder.Build(
                config, groupName, canEditSettings, canViewPrivate,
                orderedGroups, charactersByGroup, groupFullInfos, projectInfo),
            NoAccess: null);
    }
}
