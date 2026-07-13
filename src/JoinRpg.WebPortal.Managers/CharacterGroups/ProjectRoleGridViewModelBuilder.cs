using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Markdown;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;
using ProjectRolesList = JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesList;

namespace JoinRpg.WebPortal.Managers.CharacterGroups;

internal static class ProjectRoleGridViewModelBuilder
{
    public static ProjectRoleGridViewModel Build(
        ProjectRolesList config,
        string? groupName,
        bool canEditSettings,
        bool canViewPrivate,
        IReadOnlyList<CharacterGroupInfo> orderedGroups,
        ILookup<CharacterGroupIdentification, Character> charactersByGroup,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupFullInfo> groupFullInfos,
        ProjectInfo projectInfo)
    {
        var hasGroupsColumn = config.GroupsColumn != ProjectRolesListVisibilityMode.None;

        var fields = config.Fields.Select(projectInfo.GetFieldById).ToList();
        var fieldColumnNames = fields.Select(f => f.Name).ToList();

        var rows = BuildRows(config, orderedGroups, charactersByGroup, groupFullInfos, hasGroupsColumn, canViewPrivate, canEditSettings, fields, projectInfo);

        return new ProjectRoleGridViewModel(
            RolesListId: config.ProjectRolesListId,
            Name: config.Name,
            GroupName: groupName,
            CanEditSettings: canEditSettings,
            HasGroupsColumn: hasGroupsColumn,
            FieldColumnNames: fieldColumnNames,
            Rows: rows);
    }

    private static List<ProjectRoleGridRowViewModel> BuildRows(
        ProjectRolesList config,
        IReadOnlyList<CharacterGroupInfo> orderedGroups,
        ILookup<CharacterGroupIdentification, Character> charactersByGroup,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupFullInfo> groupFullInfos,
        bool hasGroupsColumn,
        bool canViewPrivate,
        bool canEditSettings,
        IReadOnlyList<ProjectFieldInfo> fields,
        ProjectInfo projectInfo)
    {
        var result = new List<ProjectRoleGridRowViewModel>();
        var seen = new HashSet<int>();
        var topGroupId = orderedGroups.Count > 0 ? orderedGroups[0].Id : (CharacterGroupIdentification?)null;

        foreach (var group in orderedGroups)
        {
            var ordered = charactersByGroup[group.Id]
                .OrderByStoredOrder(c => c.CharacterId, group.ChildCharactersOrdering)
                .ToList();

            if (config.ShowCharacterGroups && (group.Id == topGroupId || ordered.Count > 0))
            {
                var description = groupFullInfos.GetValueOrDefault(group.Id)?.Description?.ToHtmlString().Value;
                var groupLink = new CharacterGroupLinkSlimViewModel(group);
                result.Add(new ProjectRoleGridGroupHeaderRowViewModel(groupLink, description, group.GroupType));
            }

            foreach (var character in ordered)
            {
                if (config.ShowCharacterGroups || seen.Add(character.CharacterId))
                {
                    result.Add(BuildCharacterRow(character, config, projectInfo, hasGroupsColumn, canViewPrivate, canEditSettings, fields));
                }
            }
        }

        return result;
    }

    private static ProjectRoleGridCharacterRowViewModel BuildCharacterRow(
        Character character,
        ProjectRolesList config,
        ProjectInfo projectInfo,
        bool hasGroupsColumn,
        bool canViewPrivate,
        bool canEditRoles,
        IReadOnlyList<ProjectFieldInfo> fields)
    {
        var characterSlim = new CharacterLinkSlimViewModel(
            character.GetId(),
            character.CharacterName,
            character.IsActive,
            ViewModeSelector.Create(character.IsPublic, canViewPrivate));

        // Ссылку на принятую заявку показываем только мастеру (canViewPrivate).
        var approvedClaimId = canViewPrivate ? character.GetApprovedClaimIdOrDefault() : null;

        var characterLink = new CharacterLinkWithEditViewModel(characterSlim, canEditRoles, approvedClaimId);

        var player = BuildPlayerCell(character, config.ContactsColumn, canViewPrivate, projectInfo);

        GroupsCellViewModel? groups = hasGroupsColumn
            ? BuildGroupsCell(character, config.GroupsColumn, projectInfo)
            : null;

        var fieldsDict = character.GetFieldsDict(projectInfo);
        var fieldValues = fields.Select(f => fieldsDict[f.Id].DisplayString).ToList();

        return new ProjectRoleGridCharacterRowViewModel(characterLink, player, groups, fieldValues);
    }

    private static PlayerCellViewModel BuildPlayerCell(
        Character character,
        ProjectRolesListVisibilityMode contactsColumn,
        bool canViewPrivate,
        ProjectInfo projectInfo)
    {
        var applyStatus = new CharacterApplyViewModel(
            character.GetId(),
            character.GetBusyStatus(),
            character.CharacterSlotLimit,
            character.IsHot,
            character.CharacterType == CharacterType.Slot);

        var player = character.ApprovedClaim?.Player;
        if (player is null)
        {
            // Нет одобренной заявки — «нет игрока» (Link == null).
            return new PlayerCellViewModel(applyStatus, Contacts: null, Link: null);
        }

        // CharacterVisibility.PlayerHidden: игрок публичен только при HidePlayerForCharacter == false.
        // ViewMode.Hide → компонент покажет «занято» (роль занята, игрок скрыт), а не «нет игрока».
        var playerViewMode = ViewModeSelector.Create(!character.HidePlayerForCharacter, canViewPrivate);
        if (playerViewMode == ViewMode.Hide)
        {
            // Реальные данные игрока на клиент не сериализуем — отдаём sentinel «скрыто».
            return new PlayerCellViewModel(applyStatus, Contacts: null, UserLinkViewModel.Hidden);
        }

        var contacts = BuildContacts(player, contactsColumn, projectInfo);
        var link = new UserLinkViewModel(player.UserId, player.GetDisplayName(), playerViewMode);
        return new PlayerCellViewModel(applyStatus, contacts, link);
    }

    private static UserContacts? BuildContacts(
        User player,
        ProjectRolesListVisibilityMode contactsColumn,
        ProjectInfo projectInfo)
    {
        // Для архивных проектов контакты не показываем (как GetPlayerString(showContacts: false)).
        if (projectInfo.ProjectStatus == ProjectLifecycleStatus.Archived)
        {
            return null;
        }

        var showContacts = contactsColumn switch
        {
            ProjectRolesListVisibilityMode.All => true,
            // Контакты показываем, только если игрок открыл их в профиле.
            ProjectRolesListVisibilityMode.PublicOnly => player.Extra?.SocialNetworksAccess == ContactsAccessType.Public,
            _ => false,
        };

        if (!showContacts)
        {
            return null;
        }

        // Telegram строим как канонический TelegramId (числовой Id из привязанного логина +
        // @username), как в UserExtensions.GetUserInfo. Это переиспользует компонент TelegramLink.
        var telegram = TelegramId.FromOptional(
            player.ExternalLogins.SingleOrDefault(x => x.Provider == UserExternalLogin.TelegramProvider)?.Key,
            PrefferedName.FromOptional(player.Extra?.Telegram));

        // Email — непубличный контакт, показывается только в режиме All.
        // VkVerified не проверяем (как в markdown-рендерере).
        return new UserContacts(
            contactsColumn == ProjectRolesListVisibilityMode.All ? Email.FromOptional(player.Email) : null,
            VkId.FromOptional(player.Extra?.Vk),
            telegram,
            LiveJournalId.FromOptional(player.Extra?.Livejournal));
    }

    private static GroupsCellViewModel BuildGroupsCell(
        Character character,
        ProjectRolesListVisibilityMode groupsColumn,
        ProjectInfo projectInfo)
    {
        var groups = character.GetIntrestingGroupsForDisplayToTop(projectInfo);
        if (groupsColumn == ProjectRolesListVisibilityMode.PublicOnly)
        {
            groups = groups.Where(g => g.IsPublic);
        }

        var links = groups.Select(g => new CharacterGroupLinkSlimViewModel(g)).ToList();
        return new GroupsCellViewModel(links);
    }
}
