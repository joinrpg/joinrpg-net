using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Users;
using JoinRpg.WebPortal.Managers.CharacterGroups;
using ProjectRolesList = JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesList;

namespace JoinRpg.WebPortal.Managers.Test.CharacterGroups;

public class ProjectRoleGridViewModelBuilderTests
{
    private readonly MockedProject _mock = new();

    private ProjectRolesList Config(
        ProjectRolesListVisibilityMode contacts = ProjectRolesListVisibilityMode.None,
        ProjectRolesListVisibilityMode groups = ProjectRolesListVisibilityMode.None,
        IReadOnlyList<ProjectFieldIdentification>? fields = null)
        => new(
            new ProjectRolesListIdentification(_mock.ProjectInfo.ProjectId, 1),
            "Сетка",
            CharacterGroupId: null,
            PublicMode: false,
            fields ?? [],
            contacts,
            groups);

    [Fact]
    public void Build_NoExtraColumns_OnlyCharacter()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = ProjectRoleGridViewModelBuilder.Build(Config(), null, canEditSettings: false, [character], _mock.ProjectInfo);

        result.HasPlayerColumn.ShouldBeFalse();
        result.HasGroupsColumn.ShouldBeFalse();
        result.FieldColumnNames.ShouldBeEmpty();
        var row = result.Rows.ShouldHaveSingleItem();
        row.Character.CharacterId.ShouldBe(character.GetId());
        row.Character.Name.ShouldBe("Вася");
        row.Player.ShouldBeNull();
        row.Groups.ShouldBeNull();
    }

    [Fact]
    public void Build_PlayerColumn_NoClaim_ShowsNoPlayer()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = ProjectRoleGridViewModelBuilder.Build(
            Config(contacts: ProjectRolesListVisibilityMode.All), null, canEditSettings: false, [character], _mock.ProjectInfo);

        result.HasPlayerColumn.ShouldBeTrue();
        var row = result.Rows.ShouldHaveSingleItem();
        row.Player.ShouldNotBeNull();
        row.Player.Name.ShouldBe("нет игрока");
        row.Player.Contacts.ShouldBeNull();
    }

    [Fact]
    public void Build_Npc_ShowsNpcStatus()
    {
        var character = _mock.CreateCharacter("Страж");
        character.CharacterType = CharacterType.NonPlayer;

        var result = ProjectRoleGridViewModelBuilder.Build(
            Config(contacts: ProjectRolesListVisibilityMode.All), null, canEditSettings: false, [character], _mock.ProjectInfo);

        result.Rows.ShouldHaveSingleItem().Player!.Name.ShouldBe("NPC");
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

        var result = ProjectRoleGridViewModelBuilder.Build(
            Config(contacts: ProjectRolesListVisibilityMode.All), null, canEditSettings: false, [character], _mock.ProjectInfo);

        var player = result.Rows.ShouldHaveSingleItem().Player.ShouldNotBeNull();
        player.Name.ShouldBe("Player");
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

        var result = ProjectRoleGridViewModelBuilder.Build(
            Config(contacts: ProjectRolesListVisibilityMode.PublicOnly), null, canEditSettings: false, [character], _mock.ProjectInfo);

        var player = result.Rows.ShouldHaveSingleItem().Player.ShouldNotBeNull();
        player.Name.ShouldBe("Player");
        player.Contacts.ShouldBeNull();
    }

    [Fact]
    public void Build_PublicOnly_ShowsContactsWhenPlayerAllowed()
    {
        var character = _mock.CreateCharacter("Вася");
        _mock.Player.Extra = new UserExtra { SocialNetworksAccess = ContactsAccessType.Public };
        _mock.CreateApprovedClaim(character, _mock.Player);

        var result = ProjectRoleGridViewModelBuilder.Build(
            Config(contacts: ProjectRolesListVisibilityMode.PublicOnly), null, canEditSettings: false, [character], _mock.ProjectInfo);

        result.Rows.ShouldHaveSingleItem().Player!.Contacts.ShouldNotBeNull();
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

        var result = ProjectRoleGridViewModelBuilder.Build(
            Config(contacts: ProjectRolesListVisibilityMode.All), null, canEditSettings: false, [character], _mock.ProjectInfo);

        result.Rows.ShouldHaveSingleItem().Player!.Contacts.ShouldBeNull();
    }

    [Fact]
    public void Build_Fields_MapNamesAndValues()
    {
        var character = _mock.CreateCharacter("Вася");
        var field = _mock.PublicFieldInfo;

        var result = ProjectRoleGridViewModelBuilder.Build(
            Config(fields: [field.Id]), null, canEditSettings: false, [character], _mock.ProjectInfo);

        result.FieldColumnNames.ShouldBe([field.Name]);
        result.Rows.ShouldHaveSingleItem().FieldValues.Count.ShouldBe(1);
    }

    [Fact]
    public void Build_CanEditSettings_FlowsToViewModel()
    {
        var character = _mock.CreateCharacter("Вася");

        var result = ProjectRoleGridViewModelBuilder.Build(
            Config(), null, canEditSettings: true, [character], _mock.ProjectInfo);

        result.CanEditSettings.ShouldBeTrue();
        result.RolesListId.ShouldBe(Config().ProjectRolesListId);
    }

    [Fact]
    public void Build_GroupsColumn_ListsIntrestingGroups()
    {
        _mock.Group.IsPublic = true;
        _mock.ReInitProjectInfo();

        var character = _mock.CreateCharacter("Вася");
        character.ParentCharacterGroupIds = [_mock.Group.CharacterGroupId];

        var resultAll = ProjectRoleGridViewModelBuilder.Build(
            Config(groups: ProjectRolesListVisibilityMode.All), null, canEditSettings: false, [character], _mock.ProjectInfo);

        var groupsCell = resultAll.Rows.ShouldHaveSingleItem().Groups.ShouldNotBeNull();
        groupsCell.Groups.ShouldHaveSingleItem().CharacterGroupId.ShouldBe(_mock.Group.GetId());
    }
}
