using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.WebComponents;
using JoinRpg.WebPortal.Managers.CharacterGroups;
using ProjectRolesList = JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesList;

namespace JoinRpg.WebPortal.Managers.Test.CharacterGroups;

public class ProjectRoleGridViewModelBuilderTests
{
    private readonly MockedProject _mock = new();

    private ProjectRolesList Config(
        ProjectRolesListVisibilityMode contacts = ProjectRolesListVisibilityMode.None,
        ProjectRolesListVisibilityMode groups = ProjectRolesListVisibilityMode.None,
        IReadOnlyList<ProjectFieldIdentification>? fields = null,
        RolesGridGroupsViewMode groupsViewMode = RolesGridGroupsViewMode.None)
        => new(
            new ProjectRolesListIdentification(_mock.ProjectInfo.ProjectId, 1),
            "Сетка",
            CharacterGroupId: null,
            PublicMode: false,
            fields ?? [],
            contacts,
            groups,
            GroupsViewMode: groupsViewMode,
            ShowRolesFilter: ShowRolesFilter.All);

    private ProjectRoleGridViewModel BuildGrid(
        ProjectRolesList config,
        IReadOnlyCollection<Character> characters,
        string? groupName = null,
        bool canEditSettings = false,
        bool canViewPrivate = true)
    {
        var rootId = _mock.ProjectInfo.RootCharacterGroupId;
        var orderedGroups = _mock.ProjectInfo.GetChildGroupsIncludingThis(rootId).ToList();
        var visibleCharacters = canViewPrivate ? characters : characters.Where(c => c.IsPublic).ToList();
        var charactersByGroup = visibleCharacters
            .SelectMany(c => c.GetDirectGroupIds().Select(g => (group: g, character: c)))
            .ToLookup(x => x.group, x => x.character);
        return ProjectRoleGridViewModelBuilder.Build(
            config, groupName, canEditSettings, canViewPrivate,
            orderedGroups, charactersByGroup,
            new Dictionary<CharacterGroupIdentification, CharacterGroupFullInfo>(),
            _mock.ProjectInfo);
    }

    [Fact]
    public void Build_NoExtraColumns_OnlyCharacter()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = BuildGrid(Config(), [character]);

        result.HasGroupsColumn.ShouldBeFalse();
        result.FieldColumnNames.ShouldBeEmpty();
        var row = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>();
        row.Character.Character.CharacterId.ShouldBe(character.GetId());
        row.Character.Character.Name.ShouldBe("Вася");
        row.Player.ShouldNotBeNull();
        row.Groups.ShouldBeNull();
    }

    [Fact]
    public void Build_PlayerColumn_NoClaim_ShowsNoPlayer()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.All), [character]);

        var row = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>();
        row.Player.ShouldNotBeNull();
        row.Player.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.Vacancy);
        row.Player.Link.ShouldBeNull();
        row.Player.Contacts.ShouldBeNull();
    }

    [Fact]
    public void Build_Npc_ShowsNpcStatus()
    {
        var character = _mock.CreateCharacter("Страж");
        character.CharacterType = CharacterType.NonPlayer;

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.All), [character]);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player!.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.Npc);
    }

    [Fact]
    public void Build_AllMode_IncludesContactsWithTelegramLink()
    {
        var character = _mock.CreateCharacter("Вася");
        _mock.Player.Extra = new UserExtra
        {
            Vk = "vasya",
            Telegram = "vasya_tg",
            Livejournal = "vasya_lj",
            SocialNetworksAccess = ContactsAccessType.OnlyForMasters, // в All — игнорируется
        };
        // Канонический TelegramId требует числового Id из привязанного telegram-логина.
        _mock.Player.ExternalLogins.Add(new UserExternalLogin
        {
            Provider = UserExternalLogin.TelegramProvider,
            Key = "12345",
        });
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.All), [character]);

        var player = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player.ShouldNotBeNull();
        player.Link.ShouldNotBeNull().DisplayName.ShouldBe("Player");
        var contacts = player.Contacts.ShouldNotBeNull();
        contacts.Email!.Value.ShouldBe("player@example.com");
        contacts.Vk!.Value.ShouldBe("vasya");
        contacts.LiveJournal!.Value.ShouldBe("vasya_lj");
        // Канонический TelegramId: числовой Id из логина + @username из профиля.
        contacts.Telegram!.Id.ShouldBe(12345);
        contacts.Telegram.UserName!.Value.ShouldBe("vasya_tg");
    }

    [Fact]
    public void Build_PublicOnly_HidesContactsUnlessPlayerAllowed()
    {
        var character = _mock.CreateCharacter("Вася");
        _mock.Player.Extra = new UserExtra { SocialNetworksAccess = ContactsAccessType.OnlyForMasters };
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.PublicOnly), [character]);

        var player = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player.ShouldNotBeNull();
        player.Link.ShouldNotBeNull().DisplayName.ShouldBe("Player");
        player.Contacts.ShouldBeNull();
    }

    [Fact]
    public void Build_PublicOnly_ShowsContactsWhenPlayerAllowed()
    {
        var character = _mock.CreateCharacter("Вася");
        _mock.Player.Extra = new UserExtra { SocialNetworksAccess = ContactsAccessType.Public };
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.PublicOnly), [character]);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player!.Contacts.ShouldNotBeNull();
    }

    [Fact]
    public void Build_PublicOnly_HidesEmail()
    {
        var character = _mock.CreateCharacter("Вася");
        _mock.Player.Extra = new UserExtra
        {
            Vk = "vasya",
            Telegram = "vasya_tg",
            Livejournal = "vasya_lj",
            SocialNetworksAccess = ContactsAccessType.Public,
        };
        _mock.Player.ExternalLogins.Add(new UserExternalLogin
        {
            Provider = UserExternalLogin.TelegramProvider,
            Key = "12345",
        });
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.PublicOnly), [character]);

        var contacts = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player!.Contacts.ShouldNotBeNull();
        contacts.Email.ShouldBeNull();
        contacts.Vk.ShouldNotBeNull();
        contacts.Telegram.ShouldNotBeNull();
        contacts.LiveJournal.ShouldNotBeNull();
    }

    [Fact]
    public void Build_ArchivedProject_NeverShowsContacts()
    {
        var character = _mock.CreateCharacter("Вася");
        _mock.Player.Extra = new UserExtra { SocialNetworksAccess = ContactsAccessType.Public };
        _mock.CreateApprovedClaim(character, _mock.Player);

        _mock.Project.Active = false;
        _mock.Project.IsAcceptingClaims = false;
        _mock.ReInitProjectInfo();
        _mock.ProjectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.Archived);

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.All), [character]);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player!.Contacts.ShouldBeNull();
    }

    [Fact]
    public void Build_Fields_MapNamesAndValues()
    {
        var character = _mock.CreateCharacter("Вася");
        var field = _mock.PublicFieldInfo;

        var result = BuildGrid(Config(fields: [field.Id]), [character]);

        result.FieldColumnNames.ShouldBe([field.Name]);
        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .FieldValues.Count.ShouldBe(1);
    }

    [Fact]
    public void Build_CanEditSettings_FlowsToViewModel()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = BuildGrid(Config(), [character], canEditSettings: true);

        result.CanEditSettings.ShouldBeTrue();
        result.RolesListId.ShouldBe(Config().ProjectRolesListId);
    }

    [Fact]
    public void Build_PlayerCharacter_IsAvailable()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = BuildGrid(Config(), [character]);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player!.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.Vacancy);
    }

    [Fact]
    public void Build_Npc_IsNotAvailable()
    {
        var character = _mock.CreateCharacter("Страж");
        character.CharacterType = CharacterType.NonPlayer;

        var result = BuildGrid(Config(), [character]);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player!.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.Npc);
    }

    [Fact]
    public void Build_HotCharacter_IsHotInApplyStatus()
    {
        var character = _mock.CreateCharacter("Горячий");
        character.IsHot = true;

        var result = BuildGrid(Config(), [character]);

        var row = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>();
        row.Player!.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.HotVacancy);
        row.Player.ApplyStatus.IsHot.ShouldBeTrue();
    }

    [Fact]
    public void Build_SlotCharacter_SlotCountInApplyStatus()
    {
        var character = _mock.CreateCharacter("Шаблон");
        character.CharacterType = CharacterType.Slot;
        character.CharacterSlotLimit = 5;

        var result = BuildGrid(Config(), [character]);

        var row = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>();
        row.Player!.ApplyStatus.SlotCount.ShouldBe(5);
        row.Player.ApplyStatus.IsSlot.ShouldBeTrue();
    }

    [Fact]
    public void Build_ExhaustedSlot_SlotCountIsZero()
    {
        var character = _mock.CreateCharacter("Шаблон");
        character.CharacterType = CharacterType.Slot;
        character.CharacterSlotLimit = 0;

        var result = BuildGrid(Config(), [character]);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player!.ApplyStatus.SlotCount.ShouldBe(0);
    }

    [Fact]
    public void Build_GroupsColumn_ListsIntrestingGroups()
    {
        _mock.Group.IsPublic = true;
        // Делаем Group дочерней для root, чтобы попасть в обход DFS
        _mock.Group.ParentCharacterGroupIds = [_mock.Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId];
        _mock.ReInitProjectInfo();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [_mock.Group.CharacterGroupId];

        var resultAll = BuildGrid(Config(groups: ProjectRolesListVisibilityMode.All), [character]);

        var groupsCell = resultAll.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Groups.ShouldNotBeNull();
        groupsCell.Groups.ShouldHaveSingleItem().CharacterGroupId.ShouldBe(_mock.Group.GetId());
    }

    [Fact]
    public void Build_PrivateCharacter_NonMaster_RowHidden()
    {
        var character = _mock.CreateCharacter("Тайный");
        character.IsPublic = false;

        var result = BuildGrid(Config(), [character], canViewPrivate: false);

        result.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void Build_PrivateCharacter_Master_RowShownAsPrivate()
    {
        var character = _mock.CreateCharacter("Тайный");
        character.IsPublic = false;

        var result = BuildGrid(Config(), [character]);

        var row = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>();
        row.Character.Character.Name.ShouldBe("Тайный");
        row.Character.Character.ViewMode.ShouldBe(ViewMode.ShowAsPrivate);
    }

    [Fact]
    public void Build_PlayerHidden_NonMaster_PlayerHiddenButCharacterShown()
    {
        var character = _mock.CreateCharacter("Вася");
        character.IsPublic = true;
        character.HidePlayerForCharacter = true;
        _mock.Player.Extra = new UserExtra { SocialNetworksAccess = ContactsAccessType.Public };
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.PublicOnly), [character],
            canViewPrivate: false);

        var row = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>();
        row.Character.Character.Name.ShouldBe("Вася");
        row.Character.Character.ViewMode.ShouldBe(ViewMode.Show);
        var player = row.Player.ShouldNotBeNull();
        // Статус роли остаётся («занята»), несмотря на скрытие игрока.
        player.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.HasPlayer);
        // Роль «занята», но игрок скрыт: sentinel-ссылка без реальных данных игрока.
        var link = player.Link.ShouldNotBeNull();
        link.ViewMode.ShouldBe(ViewMode.Hide);
        link.UserId.ShouldBe(-1);
        link.DisplayName.ShouldNotBe("Player");
        player.Contacts.ShouldBeNull();
    }

    [Fact]
    public void Build_PlayerHidden_Master_PlayerShownAsPrivate()
    {
        var character = _mock.CreateCharacter("Вася");
        character.IsPublic = true;
        character.HidePlayerForCharacter = true;
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(contacts: ProjectRolesListVisibilityMode.All), [character]);

        var link = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Player.ShouldNotBeNull().Link.ShouldNotBeNull();
        link.ViewMode.ShouldBe(ViewMode.ShowAsPrivate);
        link.DisplayName.ShouldBe("Player");
    }

    [Fact]
    public void Build_PublicCharacter_NonMaster_RowShown()
    {
        var character = _mock.CreateCharacter("Вася");
        character.IsPublic = true;
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(), [character], canViewPrivate: false);

        var row = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>();
        row.Character.Character.ViewMode.ShouldBe(ViewMode.Show);
        row.Player.ShouldNotBeNull().Link.ShouldNotBeNull();
    }

    [Fact]
    public void Build_CanEditRoles_CharacterLinkCanEdit()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = BuildGrid(Config(), [character], canEditSettings: true);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Character.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public void Build_NoEditRights_CharacterLinkCannotEdit()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = BuildGrid(Config(), [character]);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Character.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public void Build_ApprovedClaim_Master_ApprovedClaimIdSet()
    {
        var character = _mock.CreateCharacter("Вася");
        var claim = _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(), [character]);

        var approvedClaimId = result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Character.ApprovedClaimId.ShouldNotBeNull();
        approvedClaimId.ClaimId.ShouldBe(claim.ClaimId);
    }

    [Fact]
    public void Build_NoApprovedClaim_ApprovedClaimIdNull()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = BuildGrid(Config(), [character]);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Character.ApprovedClaimId.ShouldBeNull();
    }

    [Fact]
    public void Build_ApprovedClaim_NonMaster_ApprovedClaimIdNull()
    {
        var character = _mock.CreateCharacter("Вася");
        character.IsPublic = true;
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = BuildGrid(Config(), [character], canViewPrivate: false);

        result.Rows.ShouldHaveSingleItem().ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Character.ApprovedClaimId.ShouldBeNull();
    }

    // --- GroupsViewMode ---

    private CharacterGroup SetupChildGroup()
    {
        var rootId = _mock.Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId;
        var group = _mock.CreateCharacterGroup(skipReinit: true);
        group.ParentCharacterGroupIds = [rootId];
        _mock.ReInitProjectInfo();
        return group;
    }

    [Fact]
    public void GroupsViewMode_Sections_InsertsGroupHeaderRows()
    {
        var childGroup = SetupChildGroup();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [childGroup.CharacterGroupId];

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Sections), [character]);

        // Ожидаем: заголовок root, заголовок childGroup, затем персонаж
        var rows = result.Rows;
        rows.Count.ShouldBe(3);
        rows[0].ShouldBeOfType<ProjectRoleGridGroupHeaderRowViewModel>();
        rows[1].ShouldBeOfType<ProjectRoleGridGroupHeaderRowViewModel>()
            .Group.Name.ShouldBe(childGroup.CharacterGroupName);
        rows[2].ShouldBeOfType<ProjectRoleGridCharacterRowViewModel>()
            .Character.Character.Name.ShouldBe("Вася");
    }

    [Fact]
    public void GroupsViewMode_Sections_RepeatsCharacterInMultipleGroups()
    {
        var rootId = _mock.Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId;
        var groupA = SetupChildGroup();
        var groupB = SetupChildGroup();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [groupA.CharacterGroupId, groupB.CharacterGroupId];

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Sections), [character]);

        var charRows = result.Rows.OfType<ProjectRoleGridCharacterRowViewModel>().ToList();
        // Персонаж должен появиться дважды: один раз в groupA, один раз в groupB
        charRows.Count.ShouldBe(2);
        charRows[0].Character.Character.Name.ShouldBe("Вася");
        charRows[1].Character.Character.Name.ShouldBe("Вася");
    }

    [Fact]
    public void GroupsViewMode_None_NoRepetition()
    {
        var rootId = _mock.Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId;
        var groupA = SetupChildGroup();
        var groupB = SetupChildGroup();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [groupA.CharacterGroupId, groupB.CharacterGroupId];

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.None), [character]);

        var charRows = result.Rows.OfType<ProjectRoleGridCharacterRowViewModel>().ToList();
        // Без группировки — персонаж появляется только один раз
        charRows.ShouldHaveSingleItem().Character.Character.Name.ShouldBe("Вася");
        result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>().ShouldBeEmpty();
    }

    // --- GroupsViewMode.Tree ---

    private int RootGroupId => _mock.Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId;

    private CharacterGroup CreateGroup(string name, int parentId)
    {
        var group = _mock.CreateCharacterGroup(skipReinit: true);
        group.CharacterGroupName = name;
        group.ParentCharacterGroupIds = [parentId];
        return group;
    }

    [Fact]
    public void Tree_DepthAndOrder_MatchesPreorderDfs()
    {
        // root -> (A, B), A -> (A1)
        var groupA = CreateGroup("A", RootGroupId);
        var groupB = CreateGroup("B", RootGroupId);
        var groupA1 = CreateGroup("A1", groupA.CharacterGroupId);
        _mock.ReInitProjectInfo();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [groupA.CharacterGroupId];

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), [character]);

        var headers = result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>().ToList();
        headers.Select(h => h.Group.Name).ShouldBe(["test_1", "A", "A1", "B"]);
        headers.Select(h => h.Depth).ShouldBe([0, 1, 2, 1]);

        // Персонаж идёт сразу после заголовка своей группы (A), перед заголовком A1
        var rows = result.Rows.ToList();
        var groupAIndex = rows.IndexOf(headers[1]);
        var characterRow = rows.OfType<ProjectRoleGridCharacterRowViewModel>().ShouldHaveSingleItem();
        var characterIndex = rows.IndexOf(characterRow);
        var groupA1Index = rows.IndexOf(headers[2]);
        characterIndex.ShouldBe(groupAIndex + 1);
        groupA1Index.ShouldBe(characterIndex + 1);
    }

    [Fact]
    public void Tree_EmptyGroup_IsIncludedInOutput()
    {
        var emptyGroup = CreateGroup("Пустая группа", RootGroupId);
        _mock.ReInitProjectInfo();

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), []);

        result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>()
            .ShouldContain(h => h.Group.Name == emptyGroup.CharacterGroupName);
    }

    [Fact]
    public void Tree_GroupWithTwoParents_SecondOccurrenceIsNotFirstCopyAndNotExpanded()
    {
        // root -> (A, B); X — общий ребёнок A и B; у X есть персонаж и дочерняя группа Y
        var groupA = CreateGroup("A", RootGroupId);
        var groupB = CreateGroup("B", RootGroupId);
        var groupX = _mock.CreateCharacterGroup(skipReinit: true);
        groupX.CharacterGroupName = "X";
        groupX.ParentCharacterGroupIds = [groupA.CharacterGroupId, groupB.CharacterGroupId];
        var groupY = CreateGroup("Y", groupX.CharacterGroupId);
        _mock.ReInitProjectInfo();

        var character = _mock.CreateCharacter("Персонаж X");
        character.ParentCharacterGroupIds = [groupX.CharacterGroupId];

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), [character]);

        var xHeaders = result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>()
            .Where(h => h.Group.Name == "X").ToList();
        xHeaders.Count.ShouldBe(2);
        xHeaders[0].FirstCopy.ShouldBeTrue();
        xHeaders[1].FirstCopy.ShouldBeFalse();

        // Персонаж и дочерняя группа Y выведены только один раз (при первом вхождении X)
        result.Rows.OfType<ProjectRoleGridCharacterRowViewModel>().Count().ShouldBe(1);
        result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>().Count(h => h.Group.Name == "Y").ShouldBe(1);

        // Сразу после второго заголовка X ничего из его содержимого не идёт
        var secondXIndex = result.Rows.ToList().IndexOf(xHeaders[1]);
        secondXIndex.ShouldBe(result.Rows.Count - 1);
    }

    [Fact]
    public void Tree_CharacterInTwoGroups_SecondRowIsNotFirstCopy()
    {
        var groupA = CreateGroup("A", RootGroupId);
        var groupB = CreateGroup("B", RootGroupId);
        _mock.ReInitProjectInfo();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [groupA.CharacterGroupId, groupB.CharacterGroupId];

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), [character]);

        var charRows = result.Rows.OfType<ProjectRoleGridCharacterRowViewModel>().ToList();
        charRows.Count.ShouldBe(2);
        charRows[0].FirstCopy.ShouldBeTrue();
        charRows[1].FirstCopy.ShouldBeFalse();
    }

    [Fact]
    public void Tree_PrivateBranch_NonMaster_BranchIsCutEntirely()
    {
        var privateGroup = CreateGroup("Приватная", RootGroupId); // IsPublic по умолчанию false
        var publicChild = CreateGroup("Публичный ребёнок", privateGroup.CharacterGroupId);
        publicChild.IsPublic = true;
        _mock.ReInitProjectInfo();

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), [], canViewPrivate: false);

        result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>()
            .ShouldNotContain(h => h.Group.Name == privateGroup.CharacterGroupName);
        result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>()
            .ShouldNotContain(h => h.Group.Name == publicChild.CharacterGroupName);
    }

    [Fact]
    public void Tree_PrivateBranch_Master_BranchIsShown()
    {
        var privateGroup = CreateGroup("Приватная", RootGroupId);
        var publicChild = CreateGroup("Публичный ребёнок", privateGroup.CharacterGroupId);
        publicChild.IsPublic = true;
        _mock.ReInitProjectInfo();

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), [], canViewPrivate: true);

        result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>()
            .ShouldContain(h => h.Group.Name == privateGroup.CharacterGroupName);
        result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>()
            .ShouldContain(h => h.Group.Name == publicChild.CharacterGroupName);
    }

    [Fact]
    public void Tree_SpecialGroup_SortedLastAmongSiblingsWithBoundExpression()
    {
        // Создаём спецгруппу первой, обычную — второй: по «естественному» порядку спецгруппа
        // была бы первой, но сортировка должна поставить её последней среди детей.
        var specialGroup = _mock.CreateCharacterGroup(skipReinit: true);
        specialGroup.CharacterGroupName = "Эльф";
        specialGroup.IsSpecial = true;
        specialGroup.ParentCharacterGroupIds = [RootGroupId];

        var regularGroup = CreateGroup("Обычная", RootGroupId);
        _mock.ReInitProjectInfo();

        _mock.AddField(f =>
        {
            f.FieldName = "Раса";
            f.FieldType = ProjectFieldType.Dropdown;
            f.CanPlayerView = true;
            f.Description = new MarkdownDbValue();
            f.MasterDescription = new MarkdownDbValue();
            f.DropdownValues.Add(new ProjectFieldDropdownValue
            {
                ProjectFieldDropdownValueId = 1,
                ProjectId = _mock.Project.ProjectId,
                Label = "Эльф",
                IsActive = true,
                CharacterGroupId = specialGroup.CharacterGroupId,
                Description = new MarkdownDbValue(),
                MasterDescription = new MarkdownDbValue(),
            });
        });

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), []);

        var headers = result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>().ToList();
        // root, затем дети: обычная группа перед спецгруппой (спецгруппы — последними)
        headers.Select(h => h.Group.Name).ShouldBe(["test_1", "Обычная", "Эльф"]);

        var specialHeader = headers.Single(h => h.Group.Name == "Эльф");
        specialHeader.BoundExpression.ShouldBe("Раса = Эльф");
    }

    [Fact]
    public void Tree_SecondLevelGroup_PathDoesNotIncludeRoot()
    {
        var groupA = CreateGroup("A", RootGroupId);
        var groupA1 = CreateGroup("A1", groupA.CharacterGroupId);
        _mock.ReInitProjectInfo();

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), []);

        var headerA1 = result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>()
            .Single(h => h.Group.Name == "A1");
        headerA1.Path.ShouldBe("A→A1");
    }

    [Fact]
    public void Tree_ActiveClaimsCount_CountsOnlyActiveClaims()
    {
        var character = _mock.CreateCharacter("Вася");
        _mock.CreateApprovedClaim(character, _mock.Player);
        var declinedClaim = _mock.CreateClaim(character, _mock.Master);
        declinedClaim.ClaimStatus = ClaimStatus.DeclinedByMaster;

        var result = BuildGrid(Config(groupsViewMode: RolesGridGroupsViewMode.Tree), [character]);

        var characterRow = result.Rows.OfType<ProjectRoleGridCharacterRowViewModel>().ShouldHaveSingleItem();
        characterRow.ActiveClaimsCount.ShouldBe(1);
    }
}
