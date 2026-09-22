using System.Text.Encodings.Web;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Markdown;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.ProjectCommon;
using ProjectRolesList = JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesList;

namespace JoinRpg.WebPortal.Managers.CharacterGroups;

internal static class ProjectRoleGridViewModelBuilder
{
    public static ProjectRoleGridViewModel Build(
        ProjectRolesList config,
        bool canEditSettings,
        bool canViewPrivate,
        bool excludeSpecialGroups,
        IReadOnlyList<CharacterGroupInfo> orderedGroups,
        ILookup<CharacterGroupIdentification, CharacterInfo> charactersByGroup,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupFullInfo> groupFullInfos,
        IReadOnlyDictionary<UserIdentification, UserInfo> players,
        ProjectInfo projectInfo)
    {
        var hasGroupsColumn = config.GroupsColumn != ProjectRolesListVisibilityMode.None;

        var fields = config.Fields.Select(projectInfo.GetFieldById).ToList();
        var fieldColumnNames = fields.Select(f => f.Name).ToList();

        var rootGroup = orderedGroups[0];

        var rows = config.GroupsViewMode == RolesGridGroupsViewMode.Tree
            ? BuildTreeRows(config, rootGroup.Id, charactersByGroup, groupFullInfos, players, hasGroupsColumn, canViewPrivate, canEditSettings, excludeSpecialGroups, fields, projectInfo)
            : BuildRows(config, orderedGroups, charactersByGroup, groupFullInfos, players, hasGroupsColumn, canViewPrivate, canEditSettings, fields, projectInfo);

        return new ProjectRoleGridViewModel(
            RolesListId: config.ProjectRolesListId,
            Name: config.Name,
            CanEditSettings: canEditSettings,
            HasMasterAccess: canViewPrivate,
            HasGroupsColumn: hasGroupsColumn,
            FieldColumnNames: fieldColumnNames,
            Rows: rows,
            GroupsViewMode: config.GroupsViewMode,
            RootGroupId: rootGroup.Id,
            RootGroupType: rootGroup.GroupType,
            SuppressFieldLabels: fields.Count == 1 && fields[0].IsDescription);
    }

    private static List<ProjectRoleGridRowViewModel> BuildRows(
        ProjectRolesList config,
        IReadOnlyList<CharacterGroupInfo> orderedGroups,
        ILookup<CharacterGroupIdentification, CharacterInfo> charactersByGroup,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupFullInfo> groupFullInfos,
        IReadOnlyDictionary<UserIdentification, UserInfo> players,
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
                .OrderByStoredOrder(c => c.Id.CharacterId, group.ChildCharactersOrdering)
                .ToList();

            if (config.GroupsViewMode != RolesGridGroupsViewMode.None && (group.Id == topGroupId || ordered.Count > 0))
            {
                var description = groupFullInfos.GetValueOrDefault(group.Id)?.Description?.ToHtmlString().Value;
                var groupLink = new CharacterGroupLinkSlimViewModel(group);
                result.Add(new ProjectRoleGridGroupHeaderRowViewModel(groupLink, description, group.GroupType));
            }

            foreach (var character in ordered)
            {
                if (config.GroupsViewMode != RolesGridGroupsViewMode.None || seen.Add(character.Id.CharacterId))
                {
                    result.Add(BuildCharacterRow(character, config, players, hasGroupsColumn, canViewPrivate, canEditSettings, fields, group.Id));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Режим дерева: рекурсивный preorder-DFS по дереву групп с глубиной, как в классической
    /// сетке ролей (GameGroups/Index). Пустые группы включаются, повторные вхождения группы
    /// или персонажа (несколько родителей) помечаются FirstCopy == false («см. выше»).
    /// </summary>
    private static List<ProjectRoleGridRowViewModel> BuildTreeRows(
        ProjectRolesList config,
        CharacterGroupIdentification rootGroupId,
        ILookup<CharacterGroupIdentification, CharacterInfo> charactersByGroup,
        IReadOnlyDictionary<CharacterGroupIdentification, CharacterGroupFullInfo> groupFullInfos,
        IReadOnlyDictionary<UserIdentification, UserInfo> players,
        bool hasGroupsColumn,
        bool canViewPrivate,
        bool canEditSettings,
        bool excludeSpecialGroups,
        IReadOnlyList<ProjectFieldInfo> fields,
        ProjectInfo projectInfo)
    {
        var result = new List<ProjectRoleGridRowViewModel>();
        var seenGroups = new HashSet<CharacterGroupIdentification>();
        var seenCharacters = new HashSet<int>();

        AddGroupSubtree(projectInfo.GetGroupById(rootGroupId.CharacterGroupId), depth: 0, parentPath: "", parentGroupId: null);
        return result;

        void AddGroupSubtree(CharacterGroupInfo group, int depth, string parentPath, CharacterGroupIdentification? parentGroupId)
        {
            var firstCopy = seenGroups.Add(group.Id);
            var path = parentPath.Length == 0 ? group.Name : $"{parentPath}→{group.Name}";

            string? boundExpression = null;
            if (group.IsSpecial && projectInfo.GetVariantByGroupIdOrDefault(group.Id) is { } variant)
            {
                boundExpression = $"{variant.ParentFieldName} = {variant.Label}";
            }

            result.Add(new ProjectRoleGridGroupHeaderRowViewModel(
                new CharacterGroupLinkSlimViewModel(group),
                DescriptionHtml: firstCopy ? groupFullInfos.GetValueOrDefault(group.Id)?.Description?.ToHtmlString().Value : null,
                GroupType: group.GroupType,
                Depth: depth,
                FirstCopy: firstCopy,
                Path: path,
                BoundExpression: boundExpression,
                ParentGroupId: parentGroupId));

            if (!firstCopy)
            {
                return;
            }

            var ordered = charactersByGroup[group.Id]
                .OrderByStoredOrder(c => c.Id.CharacterId, group.ChildCharactersOrdering);
            foreach (var character in ordered)
            {
                result.Add(BuildCharacterRow(
                    character, config, players, hasGroupsColumn, canViewPrivate, canEditSettings,
                    fields, group.Id, firstCopy: seenCharacters.Add(character.Id.CharacterId)));
            }

            // Как в старой сетке: скрытые ветки не показываем не-мастерам; если корень задан явно —
            // спецгруппы остаются, но последними; если строим от верха (null-корень) — исключаем совсем.
            var children = group.DirectChildGroupIds
                .Select(id => projectInfo.GetGroupById(id.CharacterGroupId))
                .Where(g => g.IsActive && (canViewPrivate || g.IsPublic))
                .Where(g => !excludeSpecialGroups || !g.IsSpecial)
                .OrderBy(g => g.IsSpecial);
            foreach (var child in children)
            {
                AddGroupSubtree(child, depth + 1, depth == 0 ? "" : path, parentGroupId: group.Id);
            }
        }
    }

    private static ProjectRoleGridCharacterRowViewModel BuildCharacterRow(
        CharacterInfo character,
        ProjectRolesList config,
        IReadOnlyDictionary<UserIdentification, UserInfo> players,
        bool hasGroupsColumn,
        bool canViewPrivate,
        bool canEditRoles,
        IReadOnlyList<ProjectFieldInfo> fields,
        CharacterGroupIdentification groupId,
        bool firstCopy = true)
    {
        var characterSlim = new CharacterLinkSlimViewModel(
            character.Id,
            character.CharacterName,
            character.IsActive,
            ViewModeSelector.Create(character.CharacterTypeInfo.IsNamePublic, canViewPrivate));

        // Ссылку на принятую заявку показываем только мастеру (canViewPrivate).
        var approvedClaimId = canViewPrivate ? character.ApprovedClaimId : null;

        var characterLink = new CharacterLinkWithEditViewModel(characterSlim, canEditRoles, approvedClaimId);

        var player = BuildPlayerCell(character, players, config.ContactsColumn, canViewPrivate);

        GroupsCellViewModel? groups = hasGroupsColumn
            ? BuildGroupsCell(character, config.GroupsColumn)
            : null;

        var fieldsDict = character.GetAllFields().ToDictionary(field => field.Field.Id);
        var fieldValuesHtml = firstCopy
            ? fields.Select(f => RenderFieldValue(f, fieldsDict[f.Id].DisplayString)).ToList()
            : [];

        // Количество активных заявок видно всем, как в классической сетке ролей.
        var activeClaimsCount = character.ActiveClaimsCount;

        return new ProjectRoleGridCharacterRowViewModel(characterLink, player, groups, fieldValuesHtml, groupId, activeClaimsCount, firstCopy);
    }


    // Как и DisplayString в FieldValueViewModel: markdown-поля рендерим в HTML,
    // остальные — экранируем как обычный текст (renderer не передаём, как и для Description группы).
    private static string RenderFieldValue(ProjectFieldInfo field, string value) =>
        field.SupportsMarkdown
            ? new MarkdownString(value).ToHtmlString().Value
            : HtmlEncoder.Default.Encode(value);

    private static PlayerCellViewModel BuildPlayerCell(
        CharacterInfo character,
        IReadOnlyDictionary<UserIdentification, UserInfo> players,
        ProjectRolesListVisibilityMode contactsColumn,
        bool canViewPrivate)
    {
        var applyStatus = new CharacterApplyViewModel(
            character.Id,
            character.GetBusyStatus(),
            character.CharacterTypeInfo.SlotLimit,
            character.CharacterTypeInfo.IsHot,
            ClaimValidator.IsAvailableForPlayer(character, character.ProjectInfo));

        // Профиль игрока в агрегат персонажа не входит (ADR013) — он приходит отдельной загрузкой.
        var player = character.ApprovedClaim is { } approvedClaim
            ? players.GetValueOrDefault(approvedClaim.PlayerId)
            : null;
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

        var contacts = BuildContacts(player, contactsColumn, character.ProjectInfo);
        var link = new UserLinkViewModel(player.ToUserInfoHeader(), playerViewMode);
        return new PlayerCellViewModel(applyStatus, contacts, link);
    }

    private static UserContacts? BuildContacts(
        UserInfo player,
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
            ProjectRolesListVisibilityMode.PublicOnly => player.Social.SocialNetworksAccess == ContactsAccessType.Public,
            _ => false,
        };

        if (!showContacts)
        {
            return null;
        }

        // Ссылки на соцсети уже собраны в UserInfo (там же, где их строит UserExtensions.GetUserInfo),
        // поэтому здесь ничего разбирать не надо.
        // Email — непубличный контакт, показывается только в режиме All.
        return new UserContacts(
            contactsColumn == ProjectRolesListVisibilityMode.All ? player.Email : null,
            player.Social.Vk,
            player.Social.Telegram,
            LiveJournalId.FromOptional(player.Social.LiveJournal));
    }

    private static GroupsCellViewModel BuildGroupsCell(
        CharacterInfo character,
        ProjectRolesListVisibilityMode groupsColumn)
    {
        var groups = character.IntrestingGroupsForDisplay;
        if (groupsColumn == ProjectRolesListVisibilityMode.PublicOnly)
        {
            groups = groups.Where(g => g.IsPublic);
        }

        var links = groups.Select(g => new CharacterGroupLinkSlimViewModel(g)).ToList();
        return new GroupsCellViewModel(links);
    }
}
