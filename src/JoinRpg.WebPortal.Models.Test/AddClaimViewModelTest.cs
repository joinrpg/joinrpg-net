using JoinRpg.CommonUI.Models;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;

namespace JoinRpg.WebPortal.Models.Test;

public class AddClaimViewModelTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void AddClaimAllowedCharacter()
    {
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeTrue();
    }

    [Fact]
    public void AddClaimAllowedGroup()
    {
        var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeTrue();
    }


    [Fact]
    public void CantSendClaimIfProjectDisabled()
    {
        Mock.Project.IsAcceptingClaims = false;
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeTrue();

    }

    [Fact]
    public void CantSendClaimToNPC()
    {
        Mock.Character.CharacterType = PrimitiveTypes.CharacterType.NonPlayer;
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CanSendClaimToSlot()
    {
        Mock.Character.CharacterType = PrimitiveTypes.CharacterType.Slot;
        Mock.Character.CharacterSlotLimit = null;
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeTrue();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimToNotAvailGroup()
    {
        Mock.Group.HaveDirectSlots = false;
        var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimIfNoSlots()
    {
        Mock.Group.HaveDirectSlots = true;
        Mock.Group.AvaiableDirectSlots = 0;
        var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimIfNoSlotsChar()
    {
        Mock.Character.CharacterType = PrimitiveTypes.CharacterType.Slot;
        Mock.Character.CharacterSlotLimit = 0;
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimToSameGroup()
    {
        _ = Mock.CreateClaim(Mock.Group, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void AllowSendClaimToSameGroupIfProjectSettingsAllows()
    {
        Mock.Project.Details.EnableManyCharacters = true;
        _ = Mock.CreateClaim(Mock.Group, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeTrue();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimToSameCharacter()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimToSameCharacterEvenProjectSettingsAllowsMultiple()
    {
        Mock.Project.Details.EnableManyCharacters = true;
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void CantSendClaimIfHasApproved()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeFalse();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }

    [Fact]
    public void AllowSendClaimEvenIfHasApprovedAccordingToSettings()
    {
        Mock.Project.Details.EnableManyCharacters = true;
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeTrue();
        vm.IsProjectRelatedReason.ShouldBeFalse();
    }


    [Fact]
    public void AllowSendClaimEvenIfHasAnotherNotApproved()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId, Mock.ProjectInfo);
        vm.CanSendClaim.ShouldBeTrue();
        vm.IsProjectRelatedReason.ShouldBeFalse();
        vm.ValidationStatus.ShouldContain(AddClaimForbideReasonViewModel.AlredySentNotApprovedClaimToAnotherPlace);
    }

    [Fact]
    public void PublicFieldsShouldBeShownOnCharacters()
    {
        //var field = Mock.CreateField(new ProjectField() { IsPublic = true, CanPlayerEdit = false });
        var value = new FieldWithValue(Mock.PublicFieldInfo, "xxx");
        Mock.Character.JsonData = new[] { value }.SerializeFields();

        var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId, Mock.ProjectInfo);
        var fieldView = vm.Fields.FieldById(Mock.PublicFieldInfo.Id.ProjectFieldId);
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
