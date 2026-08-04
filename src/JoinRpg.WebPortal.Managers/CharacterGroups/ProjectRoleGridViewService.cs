using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Interfaces;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.ProjectCommon;
using ProjectRolesList = JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesList;

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
        return await BuildResult(projectInfo, config);
    }

    public async Task<ProjectRoleGridViewResult> GetClassicRoleGrid(ProjectIdentification projectId, CharacterGroupIdentification? groupId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var resolvedGroupId = groupId ?? projectInfo.RootCharacterGroupId;
        var groupName = projectInfo.GetGroupById(resolvedGroupId.CharacterGroupId).Name;
        // Транзиентная настройка «на лету»: режим дерева + колонка описания, публичная (как старая Index).
        var config = ClassicRolesGridDefaults.Build(groupId, groupName, projectInfo.CharacterDescriptionField?.Id);
        return await BuildResult(projectInfo, config);
    }

    private async Task<ProjectRoleGridViewResult> BuildResult(ProjectInfo projectInfo, ProjectRolesList config)
    {
        // Доступ зависит от данных (PublicMode), поэтому проверяется здесь, а не атрибутом.
        // Возвращаем результат «нет доступа» (а не исключение), чтобы остров показал панель.
        if (!config.PublicMode && !projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault))
        {
            return new ProjectRoleGridViewResult(HasAccess: false, Grid: null, NoAccess: NoAccessToProjectViewModelBuilder.Build(projectInfo));
        }

        var groupId = config.CharacterGroupId ?? projectInfo.RootCharacterGroupId;
        // Если корень не задан явно, строим сетку от верха, не используя спецгруппы (см. doc-комментарий CharacterGroupId).
        var excludeSpecialGroups = config.CharacterGroupId is null;

        var orderedGroups = projectInfo.GetChildGroupsIncludingThis(groupId)
            .Where(g => !excludeSpecialGroups || !g.IsSpecial)
            .ToList();
        var groupIds = orderedGroups.Select(g => g.Id).ToList();

        var characters = (await projectRepository.GetCharacterByGroups(groupIds))
            .Where(c => c.IsActive)
            .ToList();

        // Скрытое (приватные персонажи и скрытые игроки) видит только мастер.
        var canViewPrivate = projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault);
        var visibleCharacters = canViewPrivate ? characters : characters.Where(c => c.IsPublic).ToList();

        var charactersByGroup = ApplyRolesFilter(visibleCharacters, config.ShowRolesFilter)
            .SelectMany(c => c.GetDirectGroupIds().Select(g => (group: g, character: c)))
            .ToLookup(x => x.group, x => x.character);

        var groupFullInfos =
            config.GroupsViewMode != RolesGridGroupsViewMode.None
                ? (await characterGroupRepository.GetCharacterGroupsFullInfo([.. orderedGroups.Select(g => g.Id)]))
                    .ToDictionary(g => g.Id)
                : [];

        var canEditSettings = projectInfo.HasMasterAccess(
            currentUserAccessor.UserIdentificationOrDefault,
            Permission.CanEditRoles);

        return new ProjectRoleGridViewResult(
            HasAccess: true,
            Grid: ProjectRoleGridViewModelBuilder.Build(
                config, canEditSettings, canViewPrivate, excludeSpecialGroups,
                orderedGroups, charactersByGroup, groupFullInfos, projectInfo),
            NoAccess: null);
    }

    private static List<Character> ApplyRolesFilter(List<Character> characters, ShowRolesFilter filter)
        => filter switch
        {
            ShowRolesFilter.VacantOnly => characters
                .Where(c => c.GetBusyStatus() is
                    CharacterBusyStatusView.Vacancy or CharacterBusyStatusView.HotVacancy or
                    CharacterBusyStatusView.Slot or CharacterBusyStatusView.HotSlot or
                    CharacterBusyStatusView.Discussed)
                .ToList(),
            ShowRolesFilter.HotOnly => characters.Where(c => c.IsHot).ToList(),
            _ => characters,
        };
}
