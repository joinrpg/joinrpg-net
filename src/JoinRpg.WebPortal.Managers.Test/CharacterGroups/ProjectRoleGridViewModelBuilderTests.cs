using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
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
        bool showCharacterGroups = false)
        => new(
            new ProjectRolesListIdentification(_mock.ProjectInfo.ProjectId, 1),
            "Сетка",
            CharacterGroupId: null,
            PublicMode: false,
            fields ?? [],
            contacts,
            groups,
            ShowCharacterGroups: showCharacterGroups,
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

    // --- ShowCharacterGroups ---

    private CharacterGroup SetupChildGroup()
    {
        var rootId = _mock.Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId;
        var group = _mock.CreateCharacterGroup(skipReinit: true);
        group.ParentCharacterGroupIds = [rootId];
        _mock.ReInitProjectInfo();
        return group;
    }

    [Fact]
    public void ShowCharacterGroups_True_InsertsGroupHeaderRows()
    {
        var childGroup = SetupChildGroup();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [childGroup.CharacterGroupId];

        var result = BuildGrid(Config(showCharacterGroups: true), [character]);

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
    public void ShowCharacterGroups_True_RepeatsCharacterInMultipleGroups()
    {
        var rootId = _mock.Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId;
        var groupA = SetupChildGroup();
        var groupB = SetupChildGroup();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [groupA.CharacterGroupId, groupB.CharacterGroupId];

        var result = BuildGrid(Config(showCharacterGroups: true), [character]);

        var charRows = result.Rows.OfType<ProjectRoleGridCharacterRowViewModel>().ToList();
        // Персонаж должен появиться дважды: один раз в groupA, один раз в groupB
        charRows.Count.ShouldBe(2);
        charRows[0].Character.Character.Name.ShouldBe("Вася");
        charRows[1].Character.Character.Name.ShouldBe("Вася");
    }

    [Fact]
    public void ShowCharacterGroups_False_NoRepetition()
    {
        var rootId = _mock.Project.CharacterGroups.Single(x => x.IsRoot).CharacterGroupId;
        var groupA = SetupChildGroup();
        var groupB = SetupChildGroup();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [groupA.CharacterGroupId, groupB.CharacterGroupId];

        var result = BuildGrid(Config(showCharacterGroups: false), [character]);

        var charRows = result.Rows.OfType<ProjectRoleGridCharacterRowViewModel>().ToList();
        // Без группировки — персонаж появляется только один раз
        charRows.ShouldHaveSingleItem().Character.Character.Name.ShouldBe("Вася");
        result.Rows.OfType<ProjectRoleGridGroupHeaderRowViewModel>().ShouldBeEmpty();
    }
}
