using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;

namespace JoinRpg.WebPortal.Models.Test;

public class AddClaimViewModelTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void AddClaimAllowedCharacter()
    {
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.PlayerInfo, Mock.ProjectInfo);
        vm.CanSendClaim().ShouldBeTrue();
    }

    [Fact]
    public void CantSendClaimToInactiveCharacter()
    {
        var inactive = Mock.CreateCharacter("inactive");
        inactive.IsActive = false;
        var vm = AddClaimViewModel.Create(inactive, Mock.PlayerInfo, Mock.ProjectInfo);
        vm.CanSendClaim().ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimIfProjectDisabled()
    {
        Mock.Project.IsAcceptingClaims = false;
        var projectInfo = Mock.ProjectInfo.WithChangedStatus(PrimitiveTypes.ProjectMetadata.ProjectLifecycleStatus.ActiveClaimsClosed);

        var vm = AddClaimViewModel.Create(Mock.Character, Mock.PlayerInfo, projectInfo);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeTrue();

    }

    [Fact]
    public void CantSendClaimToNPC()
    {
        Mock.Character.CharacterType = PrimitiveTypes.CharacterType.NonPlayer;
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.PlayerInfo, Mock.ProjectInfo);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CanSendClaimToSlot()
    {
        Mock.Character.CharacterType = PrimitiveTypes.CharacterType.Slot;
        Mock.Character.CharacterSlotLimit = null;
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.PlayerInfo, Mock.ProjectInfo);
        vm.CanSendClaim().ShouldBeTrue();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimIfNoSlotsChar()
    {
        Mock.Character.CharacterType = PrimitiveTypes.CharacterType.Slot;
        Mock.Character.CharacterSlotLimit = 0;
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.PlayerInfo, Mock.ProjectInfo);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimToSameCharacter()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.PlayerInfo, Mock.ProjectInfo);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimToSameCharacterEvenProjectSettingsAllowsMultiple()
    {
        Mock.Project.Details.EnableManyCharacters = true;
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.PlayerInfo, Mock.ProjectInfo);
        vm.CanSendClaim().ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void PublicFieldsShouldBeShownOnCharacters()
    {
        //var field = Mock.CreateField(new ProjectField() { IsPublic = true, CanPlayerEdit = false });
        var value = new FieldWithValue(Mock.PublicFieldInfo, "xxx");
        Mock.Character.JsonData = new[] { value }.SerializeFields();

        var vm = AddClaimViewModel.Create(Mock.Character, Mock.PlayerInfo, Mock.ProjectInfo);
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
