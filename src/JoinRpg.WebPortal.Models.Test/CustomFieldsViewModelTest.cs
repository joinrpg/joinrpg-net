using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain.Access;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.WebPortal.Models.Test;

public class CustomFieldsViewModelTest
{
    private static MockedProject Mock { get; } = new MockedProject();

    /// <summary>
    /// Директивы (%персонаж, %контакты, %список) разворачиваются только во вводных, в значениях
    /// полей — нет: см. docs/plot/special.rst в joinrpg-docs. Текст должен остаться как есть.
    /// </summary>
    [Fact]
    public void DirectiveInFieldValueIsNotRendered()
    {
        var mock = new MockedProject();
        var markdownField = mock.CreateField("Знакомства", canPlayerEdit: true, showOnUnApprovedClaims: true, fieldType: ProjectFieldType.Text);
        var target = mock.CreateCharacter("Элендил");
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(markdownField, $"%персонаж{target.CharacterId}"));

        var vm = new CustomFieldsViewModel(
            mock.Character,
            mock.ProjectInfo,
            AccessArgumentsFactory.Create(mock.Character, new UserIdentification(mock.Player.UserId), mock.ProjectInfo),
            FieldUserLinksLoader.None);

        vm.Field(markdownField)!.DisplayString.ToHtmlString().ShouldBe($"<p>%персонаж{target.CharacterId}</p>");
    }

    [Fact]
    public void HideMasterOnlyFieldOnAddClaimTest()
    {
        var vm = new CustomFieldsViewModel(Mock.Character, Mock.ProjectInfo, AccessArgumentsFactory.CreateForAdd(Mock.GetCharacterInfo(Mock.Character), new(Mock.Player.UserId)), FieldUserLinksLoader.None, []);
        vm.Field(Mock.MasterOnlyFieldInfo)!.CanView.ShouldBeFalse();
    }

    [Fact]
    public void HideUnApprovedFieldOnAddClaimTest()
    {
        var vm = new CustomFieldsViewModel(Mock.Character, Mock.ProjectInfo, AccessArgumentsFactory.CreateForAdd(Mock.GetCharacterInfo(Mock.Character), new(Mock.Player.UserId)), FieldUserLinksLoader.None, []);
        vm.Field(Mock.HideForUnApprovedClaimInfo)!.CanView.ShouldBeFalse();
    }

    [Fact]
    public void ShowPublicFieldToAnon()
    {
        MockedProject.AssignFieldValues(Mock.Character,
            new FieldWithValue(Mock.PublicFieldInfo, "1"));

        var vm = new CustomFieldsViewModel(character: Mock.Character, projectInfo: Mock.ProjectInfo,
            AccessArgumentsFactory.Create(Mock.Character, new UserIdentification(Mock.Player.UserId), Mock.ProjectInfo),
            FieldUserLinksLoader.None
            );

        var publicField = vm.Field(Mock.PublicFieldInfo);
        _ = publicField.ShouldNotBeNull();
        publicField.CanView.ShouldBeTrue();
        _ = publicField.Value.ShouldNotBeNull();
    }

    [Fact]
    public void ShowPublicFieldToAnonEvenIfEditDisabled()
    {
        MockedProject.AssignFieldValues(Mock.Character, new FieldWithValue(Mock.PublicFieldInfo, "1"));

        var vm = new CustomFieldsViewModel(
            character: Mock.Character,
            projectInfo: Mock.ProjectInfo,
            accessArguments: AccessArgumentsFactory.Create(Mock.Character, new UserIdentification(Mock.Player.UserId), Mock.ProjectInfo) with { EditAllowed = false },
            users: FieldUserLinksLoader.None);
        var publicField = vm.Field(Mock.PublicFieldInfo);
        _ = publicField.ShouldNotBeNull();
        publicField.CanView.ShouldBeTrue();
    }


    [Fact]
    public void AllowCharactersFieldOnAddClaimTest()
    {
        var vm = new CustomFieldsViewModel(Mock.Character, Mock.ProjectInfo, AccessArgumentsFactory.CreateForAdd(Mock.GetCharacterInfo(Mock.Character), new(Mock.Player.UserId)), FieldUserLinksLoader.None, []);
        var characterField = vm.Field(Mock.CharacterFieldInfo);

        _ = characterField.ShouldNotBeNull();
        characterField.CanView.ShouldBeFalse();
        characterField.Value.ShouldBeNull();

        characterField.CanEdit.ShouldBeTrue();
    }

    [Fact]
    public void AllowShadowCharacterFieldsTest()
    {
        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        MockedProject.AssignFieldValues(claim, new FieldWithValue(mock.CharacterFieldInfo, "test"));

        var vm = new CustomFieldsViewModel(mock.Player.UserId, claim, mock.ProjectInfo, FieldUserLinksLoader.None);

        var characterField = vm.Field(mock.CharacterFieldInfo);

        _ = characterField.ShouldNotBeNull();
        characterField.ShouldBeHidden();
        characterField.Value.ShouldBe("test");

        characterField.ShouldBeEditable();
    }

    [Fact]
    public void DoNotDiscloseOriginalFieldValuesTest()
    {
        var mock = new MockedProject();
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        var vm = new CustomFieldsViewModel(mock.Player.UserId, claim, mock.ProjectInfo, FieldUserLinksLoader.None);

        var characterField = vm.Field(mock.CharacterFieldInfo);

        _ = characterField.ShouldNotBeNull();
        characterField.ShouldBeHidden();
        characterField.ShouldNotHaveValue();

        characterField.ShouldBeEditable();
    }

    [Fact]
    public void DoNotDiscloseOriginalFieldValuesOnAddTest()
    {
        var mock = new MockedProject();

        // Права считаем по тому же моку, что и вьюмодель: раньше тут стоял мок из поля класса.
        var vm = new CustomFieldsViewModel(mock.Character, mock.ProjectInfo, AccessArgumentsFactory.CreateForAdd(mock.GetCharacterInfo(mock.Character), new(mock.Player.UserId)), FieldUserLinksLoader.None, []);

        var characterField = vm.Field(mock.CharacterFieldInfo);

        _ = characterField.ShouldNotBeNull();
        characterField.ShouldBeHidden();
        characterField.ShouldNotHaveValue();

        characterField.ShouldBeEditable();
    }


    [Fact]
    public void AllowCharactersFieldOnAddClaimForCharacterTest()
    {
        var vm = new CustomFieldsViewModel(Mock.Character, Mock.ProjectInfo, AccessArgumentsFactory.CreateForAdd(Mock.GetCharacterInfo(Mock.Character), new(Mock.Player.UserId)), FieldUserLinksLoader.None);
        var characterField = vm.Field(Mock.CharacterFieldInfo);
        _ = characterField.ShouldNotBeNull();

        characterField.ShouldBeHidden();
        characterField.ShouldNotHaveValue();

        characterField.ShouldBeEditable();
    }

    [Fact]
    public void ProperlyHideConditionalHeader()
    {
        var conditionalHeader = Mock.CreateConditionalHeader(Mock.Group);
        var claim = Mock.CreateApprovedClaim(Mock.CreateCharacter("another"), Mock.Player);

        var vm = new CustomFieldsViewModel(Mock.Player.UserId, claim, Mock.ProjectInfo, FieldUserLinksLoader.None);
        var characterField = vm.Field(conditionalHeader);

        _ = characterField.ShouldNotBeNull();
        characterField.ShouldBeHidden();
        characterField.ShouldNotHaveValue();

        characterField.ShouldBeReadonly();
    }

    [Fact]
    public void ProperlyShowConditionalHeaderTest()
    {
        var conditionalHeader = Mock.CreateConditionalHeader(Mock.Group);
        MockedProject.AddCharToGroup(Mock.Character, Mock.Group);

        var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

        var vm = new CustomFieldsViewModel(Mock.Player.UserId, claim, Mock.ProjectInfo, FieldUserLinksLoader.None);
        var characterField = vm.Field(conditionalHeader);

        _ = characterField.ShouldNotBeNull();
        characterField.ShouldBeVisible();
        characterField.ShouldNotHaveValue();

        characterField.ShouldBeEditable();
    }

    [Fact]
    public void AllowCharactersFieldOnAddClaimForGroupTest()
    {
        var vm = new CustomFieldsViewModel(Mock.Character, Mock.ProjectInfo, AccessArgumentsFactory.CreateForAdd(Mock.GetCharacterInfo(Mock.Character), new(Mock.Player.UserId)), FieldUserLinksLoader.None);
        var characterField = vm.Field(Mock.CharacterFieldInfo);
        _ = characterField.ShouldNotBeNull();
        characterField.ShouldBeHidden();

        characterField.ShouldNotHaveValue();

        characterField.ShouldBeEditable();
    }

    [Fact]
    public void UserLinkFieldShowsUserNameAndLink()
    {
        var (vm, field) = CreateUserLinkFieldViewModel("7");

        var userLink = vm.Field(field).ShouldNotBeNull().UserLinks.ShouldHaveSingleItem();
        userLink.DisplayName.ShouldBe("Седьмой");
        userLink.UserId.ShouldBe(new UserIdentification(7));
    }

    [Fact]
    public void UserLinkFieldShowsDeletedUserWithoutLink()
    {
        var (vm, field) = CreateUserLinkFieldViewModel("8");

        var userLink = vm.Field(field).ShouldNotBeNull().UserLinks.ShouldHaveSingleItem();
        userLink.DisplayName.ShouldBe("пользователь удалён");
        userLink.UserId.ShouldBeNull();
    }

    /// <summary>
    /// Мусор в уже сохранённом значении не должен ронять показ страницы (ADR017).
    /// </summary>
    [Fact]
    public void UserLinkFieldWithGarbageValueDoesNotThrow()
    {
        var (vm, field) = CreateUserLinkFieldViewModel("abc");

        vm.Field(field).ShouldNotBeNull().UserLinks.ShouldBeEmpty();
    }

    /// <param name="value">Сырое значение поля, как оно лежит в базе</param>
    private static (CustomFieldsViewModel vm, ProjectFieldInfo field) CreateUserLinkFieldViewModel(string value)
    {
        var mock = new MockedProject();
        var field = mock.CreateField("Ответственный мастер", fieldType: ProjectFieldType.UserLink);
        MockedProject.AssignFieldValues(mock.Character, new FieldWithValue(field, value));

        // В словаре только седьмой: всё остальное на экране считается удалённым пользователем.
        IReadOnlyDictionary<UserIdentification, UserInfoHeader> users = new Dictionary<UserIdentification, UserInfoHeader>
        {
            [new UserIdentification(7)] = new(new UserIdentification(7), new UserDisplayName("Седьмой", null)),
        };

        var vm = new CustomFieldsViewModel(
            mock.Character,
            mock.ProjectInfo,
            AccessArgumentsFactory.Create(mock.Character, new UserIdentification(mock.Master.UserId), mock.ProjectInfo),
            users);
        return (vm, field);
    }

    //[Fact]
    //public void ShowPublicCharactersFieldValueOnAddClaim()
    //{
    //    var field = Mock.CreateField(new ProjectField() { IsPublic = true, CanPlayerEdit = false });
    //    var value = new FieldWithValue(field, "xxx");
    //    Mock.Character.JsonData = new[] { value }.SerializeFields();

    //    var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource)Mock.Character, Mock.ProjectInfo);
    //    var characterField = vm.Field(field);
    //    _ = characterField.ShouldNotBeNull();
    //    characterField.ShouldBeVisible();

    //    characterField.Value.ShouldBe("xxx");

    //    characterField.ShouldBeReadonly();
    //}

    //[Fact]
    //public void PublicCharactersFieldValueOnAddClaimAreReadonlyIfNotShowForUnApprovedClaims()
    //{
    //    var field = Mock.CreateField(new ProjectField() { IsPublic = true, CanPlayerEdit = true, ShowOnUnApprovedClaims = false });
    //    var value = new FieldWithValue(field, "xxx");
    //    Mock.Character.JsonData = new[] { value }.SerializeFields();

    //    var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource)Mock.Character, Mock.ProjectInfo);
    //    var characterField = vm.Field(field);
    //    _ = characterField.ShouldNotBeNull();
    //    characterField.ShouldBeVisible();

    //    characterField.Value.ShouldBe("xxx");

    //    characterField.ShouldBeReadonly();
    //}

    //[Fact]
    //public void PublicCharactersFieldValueOnAddClaimShouldBeEditableIfNotShowForUnApprovedClaims()
    //{
    //    var field = Mock.CreateField(new ProjectField()
    //    {
    //        IsPublic = true,
    //        CanPlayerEdit = true,
    //        ShowOnUnApprovedClaims = true,
    //    });
    //    var value = new FieldWithValue(field, "xxx");
    //    Mock.Character.JsonData = new[] { value }.SerializeFields();

    //    var vm = new CustomFieldsViewModel(Mock.Player.UserId, (IClaimSource)Mock.Character, Mock.ProjectInfo);
    //    var characterField = vm.Field(field);
    //    _ = characterField.ShouldNotBeNull();
    //    characterField.ShouldBeVisible();

    //    characterField.Value.ShouldBe("xxx");

    //    characterField.ShouldBeEditable();
    //}
}
