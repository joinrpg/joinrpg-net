using JoinRpg.CommonUI.Models;
using JoinRpg.DataModel.Mocks;
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
            Mock.CreateClaim(Mock.Group, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void AllowSendClaimToSameGroupIfProjectSettingsAllows()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            Mock.CreateClaim(Mock.Group, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeTrue();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimToSameCharacter()
        {
            Mock.CreateClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimToSameCharacterEvenProjectSettingsAllowsMultiple()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            Mock.CreateClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Character, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void CantSendClaimIfHasApproved()
        {
            Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeFalse();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }

        [Fact]
        public void AllowSendClaimEvenIfHasApprovedAccordingToSettings()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeTrue();
            vm.IsProjectRelatedReason.ShouldBeFalse();
        }


        [Fact]
        public void AllowSendClaimEvenIfHasAnotherNotApproved()
        {
            Mock.CreateClaim(Mock.Character, Mock.Player);
            var vm = AddClaimViewModel.Create(Mock.Group, Mock.Player.UserId);
            vm.CanSendClaim.ShouldBeTrue();
            vm.IsProjectRelatedReason.ShouldBeFalse();
            vm.ValidationStatus.ShouldContain(AddClaimForbideReasonViewModel.AlredySentNotApprovedClaimToAnotherPlace);
        }

    }
}
