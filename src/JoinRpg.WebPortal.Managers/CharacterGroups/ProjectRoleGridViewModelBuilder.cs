using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
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
        IReadOnlyCollection<Character> characters,
        ProjectInfo projectInfo)
    {
        var hasPlayerColumn = config.ContactsColumn != ProjectRolesListVisibilityMode.None;
        var hasGroupsColumn = config.GroupsColumn != ProjectRolesListVisibilityMode.None;

        var fields = config.Fields.Select(projectInfo.GetFieldById).ToList();
        var fieldColumnNames = fields.Select(f => f.Name).ToList();

        var rows = characters
            .Select(character => BuildRow(character, config, projectInfo, hasPlayerColumn, hasGroupsColumn, fields))
            .ToList();

        return new ProjectRoleGridViewModel(
            RolesListId: config.ProjectRolesListId,
            Name: config.Name,
            GroupName: groupName,
            CanEditSettings: canEditSettings,
            HasPlayerColumn: hasPlayerColumn,
            HasGroupsColumn: hasGroupsColumn,
            FieldColumnNames: fieldColumnNames,
            Rows: rows);
    }

    private static ProjectRoleGridRowViewModel BuildRow(
        Character character,
        ProjectRolesList config,
        ProjectInfo projectInfo,
        bool hasPlayerColumn,
        bool hasGroupsColumn,
        IReadOnlyList<ProjectFieldInfo> fields)
    {
        var characterLink = new CharacterLinkSlimViewModel(
            character.GetId(),
            character.CharacterName,
            character.IsActive,
            ViewModeSelector.Create(character.IsPublic, canViewPrivate: true));

        PlayerCellViewModel? player = hasPlayerColumn
            ? BuildPlayerCell(character, config.ContactsColumn, projectInfo)
            : null;

        GroupsCellViewModel? groups = hasGroupsColumn
            ? BuildGroupsCell(character, config.GroupsColumn, projectInfo)
            : null;

        var fieldsDict = character.GetFieldsDict(projectInfo);
        var fieldValues = fields.Select(f => fieldsDict[f.Id].DisplayString).ToList();

        return new ProjectRoleGridRowViewModel(characterLink, player, groups, fieldValues);
    }

    private static PlayerCellViewModel BuildPlayerCell(
        Character character,
        ProjectRolesListVisibilityMode contactsColumn,
        ProjectInfo projectInfo)
    {
        // TODO[Unify]: статус игрока дублирует приватный GetPlayerString в JoinrpgMarkdownLinkRenderer.
        var player = character.ApprovedClaim?.Player;
        var name = (character.CharacterType, player) switch
        {
            (CharacterType.NonPlayer, _) => "NPC",
            (CharacterType.Slot, _) => "шаблон",
            (CharacterType.Player, null) => "нет игрока",
            (CharacterType.Player, User p) => p.GetDisplayName(),
            _ => throw new NotImplementedException(),
        };

        var contacts = player is null ? null : BuildContacts(player, contactsColumn, projectInfo);
        return new PlayerCellViewModel(name, contacts);
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

        var links = groups
            .Select(g => new CharacterGroupLinkSlimViewModel(g.Id, g.Name, g.IsPublic, g.IsActive))
            .ToList();
        return new GroupsCellViewModel(links);
    }
}
