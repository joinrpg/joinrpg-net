using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.ProjectCommon;
using ProjectRolesList = JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesList;

namespace JoinRpg.WebPortal.Managers.CharacterGroups;

internal class ProjectRoleGridViewService(
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository,
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

    public async Task<ProjectRoleGridViewResult> GetClassicRoleGrid(ProjectIdentification projectId, CharacterGroupIdentification? groupId, bool hotOnly = false)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        // Транзиентная настройка «на лету»: режим дерева + колонка описания, публичная (как старая Index);
        // для hotOnly — плоский список только горячих ролей (как старая GameGroups/Hot).
        var config = hotOnly
            ? ClassicRolesGridDefaults.BuildHot(groupId, projectInfo.CharacterDescriptionField?.Id)
            : ClassicRolesGridDefaults.Build(
                groupId,
                projectInfo.GetGroupById((groupId ?? projectInfo.GroupTree.RootGroupId).CharacterGroupId).Name,
                projectInfo.CharacterDescriptionField?.Id);
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

        var groupId = config.CharacterGroupId ?? projectInfo.GroupTree.RootGroupId;
        // Если корень не задан явно, строим сетку от верха, не используя спецгруппы (см. doc-комментарий CharacterGroupId).
        var excludeSpecialGroups = config.CharacterGroupId is null;

        var orderedGroups = projectInfo.GroupTree.GetChildGroupsIncludingThis(groupId)
            .Where(g => !excludeSpecialGroups || !g.IsSpecial)
            .ToList();
        var groupIds = orderedGroups.Select(g => g.Id).ToList();

        // Удалённые персонажи сетке не нужны — просим только живых, чтобы не собирать агрегат
        // (поля, заявки) на тех, кто всё равно не попадёт в ответ.
        var characters = (await characterInfoRepository.GetCharacterInfosByGroups(
                projectInfo.ProjectId, groupIds, CharacterStatusSpec.Active))
            .ToList();

        // Скрытое (приватные персонажи и скрытые игроки) видит только мастер.
        var canViewPrivate = projectInfo.HasMasterAccess(currentUserAccessor.UserIdentificationOrDefault);
        var visibleCharacters = canViewPrivate
            ? characters
            : characters.Where(c => c.CharacterTypeInfo.IsNamePublic).ToList();

        var shownCharacters = ApplyRolesFilter(visibleCharacters, config.ShowRolesFilter);

        var charactersByGroup = shownCharacters
            .SelectMany(c => c.DirectGroupIds.Select(g => (group: g, character: c)))
            .ToLookup(x => x.group, x => x.character);

        // Профиль игрока в агрегат персонажа не входит (ADR013), а колонке контактов он нужен —
        // грузим одним запросом на всю сетку, а не по игроку на строку.
        var players = await LoadPlayers(shownCharacters);

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
                orderedGroups, charactersByGroup, groupFullInfos, players, projectInfo),
            NoAccess: null);
    }

    private async Task<IReadOnlyDictionary<UserIdentification, UserInfo>> LoadPlayers(
        IReadOnlyCollection<CharacterInfo> characters)
    {
        IReadOnlyCollection<UserIdentification> playerIds =
            [.. characters.Select(c => c.ApprovedClaim?.PlayerId).WhereNotNull().Distinct()];

        if (playerIds.Count == 0)
        {
            return new Dictionary<UserIdentification, UserInfo>();
        }

        return (await userRepository.GetRequiredUserInfos(playerIds)).ToDictionary(user => user.UserId);
    }

    private static List<CharacterInfo> ApplyRolesFilter(List<CharacterInfo> characters, ShowRolesFilter filter)
        => filter switch
        {
            ShowRolesFilter.VacantOnly => characters
                .Where(c => c.GetBusyStatus() is
                    CharacterBusyStatusView.Vacancy or CharacterBusyStatusView.HotVacancy or
                    CharacterBusyStatusView.Slot or CharacterBusyStatusView.HotSlot or
                    CharacterBusyStatusView.Discussed)
                .ToList(),
            ShowRolesFilter.HotOnly => characters.Where(c => c.CharacterTypeInfo.IsHot).ToList(),
            _ => characters,
        };
}
