using JoinRpg.CommonUI.Models;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.Web.Models;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Test
{
    public class AddClaimViewModelTest
    {
        private MockedProject Mock { get; } = new MockedProject();

        [Fact]
        public void AddClaimAllowedCharacter()
        {
            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            vm.IsAvailable.ShouldBeTrue();
            vm.CanSendClaim.ShouldBeTrue();
        }

        [Fact]
        public void AddClaimAllowedGroup()
        {
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.IsAvailable.ShouldBeTrue();
            vm.CanSendClaim.ShouldBeTrue();
        }


        [Fact]
        public void CantSendClaimIfProjectDisabled()
        {
            Mock.Project.IsAcceptingClaims = false;
            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            vm.IsAvailable.ShouldBeFalse();
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeTrue();

        }

        [Fact]
        public void CantSendClaimToNotAvailCharacter()
        {
            Mock.Character.IsAcceptingClaims = false;
            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            vm.IsAvailable.ShouldBeFalse();
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimToNotAvailGroup()
        {
            Mock.Group.HaveDirectSlots = false;
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.IsAvailable.ShouldBeFalse();
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimIfNoSlots()
        {
            Mock.Group.HaveDirectSlots = true;
            Mock.Group.AvaiableDirectSlots = 0;
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.IsAvailable.ShouldBeFalse();
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimToSameGroup()
        {
            _ = Mock.CreateClaim(Mock.Group, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void AllowSendClaimToSameGroupIfProjectSettingsAllows()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            _ = Mock.CreateClaim(Mock.Group, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeTrue();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimToSameCharacter()
        {
            _ = Mock.CreateClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimToSameCharacterEvenProjectSettingsAllowsMultiple()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            _ = Mock.CreateClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimIfHasApproved()
        {
            _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void AllowSendClaimEvenIfHasApprovedAccordingToSettings()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeTrue();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }


        [Fact]
        public void AllowSendClaimEvenIfHasAnotherNotApproved()
        {
            _ = Mock.CreateClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeTrue();
            vm.IsProjectRelatedReason.ShouldBeFalse();
            vm.ValidationStatus.ShouldContain(AddClaimForbideReasonViewModel.AlredySentNotApprovedClaimToAnotherPlace);
        }

        [Fact]
        public void PublicFieldsShouldBeShownOnCharacters()
        {
            var field = Mock.CreateField(new ProjectField() { IsPublic = true, CanPlayerEdit = false });
            var value = new FieldWithValue(field, "xxx");
            Mock.Character.JsonData = new[] { value }.SerializeFields();

            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            var fieldView = vm.Fields.Field(field);
            _ = fieldView.ShouldNotBeNull();
            fieldView.ShouldBeVisible();
            fieldView.ShouldBeReadonly();
            fieldView.Value.ShouldBe("xxx");
        }

        [Fact]
        public void NonPublicFieldsShouldNotBeShownOnCharacters()
        {
            var field = Mock.CreateField(new ProjectField() { IsPublic = false, CanPlayerEdit = false });
            var value = new FieldWithValue(field, "xxx");
            Mock.Character.JsonData = new[] { value }.SerializeFields();

            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            var fieldView = vm.Fields.Field(field);
            _ = fieldView.ShouldNotBeNull();
            fieldView.ShouldBeHidden();
            fieldView.ShouldBeReadonly();
        }
    }
}
