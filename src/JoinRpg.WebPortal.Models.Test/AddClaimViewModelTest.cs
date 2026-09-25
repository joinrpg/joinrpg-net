using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.Models.Helpers;
// В JoinRpg.DataModel есть своя ProjectDetails (EF-сущность), здесь нужна доменная.
using ProjectDetails = JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails;

namespace JoinRpg.WebPortal.Models.Test;

public class AddClaimViewModelTest
{
    private MockedProject Mock { get; } = new MockedProject();

    /// <summary>
    /// Вьюмодель строится поверх агрегата (ADR013), поэтому собирать его надо после всех правок
    /// персонажа и проекта — он привязан к текущему экземпляру <see cref="ProjectInfo"/>.
    /// </summary>
    private AddClaimViewModel CreateViewModel(Character character)
        => AddClaimViewModel.Create(
            Mock.GetCharacterInfo(character),
            Mock.PlayerInfo,
            new ProjectDetails(Mock.ProjectInfo, new MarkdownString(""), new MarkdownString("правила подачи"), [], false),
            new JoinrpgMarkdownLinkRenderer(Mock.Project, Mock.ProjectInfo));

    [Fact]
    public void AddClaimAllowedCharacter()
    {
        var vm = CreateViewModel(Mock.Character);
        vm.CanSendClaim().ShouldBeTrue();
    }

    /// <summary>
    /// Правила подачи приходят из <see cref="ProjectDetails"/>, а не из EF-проекта.
    /// </summary>
    [Fact]
    public void ClaimApplyRulesAreShown()
        => CreateViewModel(Mock.Character).ClaimApplyRules.ToString().ShouldContain("правила подачи");

    [Fact]
    public void CantSendClaimToInactiveCharacter()
    {
        var inactive = Mock.CreateCharacter("inactive");
        inactive.IsActive = false;
        var vm = CreateViewModel(inactive);
        vm.CanSendClaim().ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimIfProjectDisabled()
    {
        Mock.Project.IsAcceptingClaims = false;
        // Статус проекта считается из самого проекта, поэтому достаточно перестроить метаданные —
        // агрегат персонажа обязан быть привязан к тому же экземпляру ProjectInfo.
        Mock.ReInitProjectInfo();

        var vm = CreateViewModel(Mock.Character);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeTrue();

    }

    [Fact]
    public void CantSendClaimToNPC()
    {
        Mock.Character.CharacterType = CharacterType.NonPlayer;
        var vm = CreateViewModel(Mock.Character);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CanSendClaimToSlot()
    {
        Mock.Character.CharacterType = CharacterType.Slot;
        Mock.Character.CharacterSlotLimit = null;
        var vm = CreateViewModel(Mock.Character);
        vm.CanSendClaim().ShouldBeTrue();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimIfNoSlotsChar()
    {
        Mock.Character.CharacterType = CharacterType.Slot;
        Mock.Character.CharacterSlotLimit = 0;
        var vm = CreateViewModel(Mock.Character);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimToSameCharacter()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var vm = CreateViewModel(Mock.Character);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimToSameCharacterEvenProjectSettingsAllowsMultiple()
    {
        Mock.Project.Details.EnableManyCharacters = true;
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var vm = CreateViewModel(Mock.Character);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void PublicFieldsShouldBeShownOnCharacters()
    {
        //var field = Mock.CreateField(new ProjectField() { IsPublic = true, CanPlayerEdit = false });
        var value = new FieldWithValue(Mock.PublicFieldInfo, "xxx");
        Mock.Character.JsonData = new[] { value }.SerializeFields();

        var vm = CreateViewModel(Mock.Character);
        var fieldView = vm.Fields.Field(Mock.PublicFieldInfo);
        _ = fieldView.ShouldNotBeNull();
        fieldView.ShouldBeVisible();
        fieldView.ShouldBeReadonly();
        fieldView.Value.ShouldBe("xxx");
    }

    //[Fact]
    //public void NonPublicFieldsShouldNotBeShownOnCharacters()
    //{
    //    var field = Mock.CreateField(new ProjectField() { IsPublic = false, CanPlayerEdit = false });
    //    var value = new FieldWithValue(field, "xxx");
    //    Mock.Character.JsonData = new[] { value }.SerializeFields();

    //    var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
    //    var fieldView = vm.Fields.Field(field);
    //    _ = fieldView.ShouldNotBeNull();
    //    fieldView.ShouldBeHidden();
    //    fieldView.ShouldBeReadonly();
    //}
}
