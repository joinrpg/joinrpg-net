using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test.AddClaim
{
    public class AddClaimValidationRulesTest
    {
        private MockedProject Mock { get; } = new MockedProject();

        [Fact]
        public void AddClaimAllowedCharacter() => ShouldBeAllowed(Mock.Character);

        [Fact]
        public void AddClaimAllowedGroup() => ShouldBeAllowed(Mock.Group);

        [Fact]
        public void AddClaimAllowedCharacterWithoutUser() => Mock.Character.ValidateIfCanAddClaim(playerUserId: null).ShouldBeEmpty();

        [Fact]
        public void AddClaimAllowedGroupWithoutUser() => Mock.Group.ValidateIfCanAddClaim(playerUserId: null).ShouldBeEmpty();

        [Fact]
        public void CantSendClaimIfProjectClaimsClosed()
        {
            Mock.Project.IsAcceptingClaims = false;
            ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.ProjectClaimsClosed);
        }

        [Fact]
        public void CantSendClaimIfProjectClosed()
        {
            Mock.Project.Active = false;
            ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.ProjectNotActive);
        }

        [Fact]
        public void CantSendClaimIfNoSlots()
        {
            Mock.Group.HaveDirectSlots = true;
            Mock.Group.AvaiableDirectSlots = 0;
            ShouldBeNotAllowed(Mock.Group, AddClaimForbideReason.SlotsExhausted);
        }

        [Fact]
        public void CantSendClaimIfNoSlotsChar()
        {
            Mock.Character.CharacterType = PrimitiveTypes.CharacterType.Slot;
            Mock.Character.CharacterSlotLimit = 0;
            ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.SlotsExhausted);
        }


        [Fact]
        public void CantSendClaimIfCharacterIsNpc()
        {
            Mock.Character.CharacterType = PrimitiveTypes.CharacterType.NonPlayer;
            ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Npc);
        }

        [Fact]
        public void CantSendClaimIfCharacterHasApprovedClaim()
        {
            Mock.Character.ApprovedClaim = new Claim();
            Mock.Character.ApprovedClaimId = -1;
            ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Busy);
        }

        [Fact]
        public void CantSendClaimIfCharacterHasCheckedInClaim()
        {
            _ = Mock.CreateCheckedInClaim(Mock.Character, Mock.Master);
            ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Busy);
        }

        [Fact]
        public void CantSendClaimToSameGroup()
        {
            _ = Mock.CreateClaim(Mock.Group, Mock.Player);
            ShouldBeNotAllowed(Mock.Group, AddClaimForbideReason.AlreadySent);
        }

        [Fact]
        public void AllowSendClaimToSameGroupIfProjectSettingsAllows()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            _ = Mock.CreateClaim(Mock.Group, Mock.Player);
            ShouldBeAllowed(Mock.Group);
        }

        [Fact]
        public void CantSendClaimToSameCharacter()
        {
            _ = Mock.CreateClaim(Mock.Character, Mock.Player);
            ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.AlreadySent);
        }

        [Fact]
        public void CantSendClaimToSameCharacterEvenProjectSettingsAllowsMultiple()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            _ = Mock.CreateClaim(Mock.Character, Mock.Player);
            ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.AlreadySent);
        }

        [Fact]
        public void CantSendClaimIfHasApproved()
        {
            _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
            ShouldBeNotAllowed(Mock.Group, AddClaimForbideReason.OnlyOneCharacter);
        }

        [Fact]
        public void AllowSendClaimEvenIfHasApprovedAccordingToSettings()
        {
            Mock.Project.Details.EnableManyCharacters = true;
            _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
            ShouldBeAllowed(Mock.Group);
        }

        [Fact]
        public void AllowSendClaimEvenIfHasAnotherNotApproved()
        {
            _ = Mock.CreateClaim(Mock.Character, Mock.Player);
            ShouldBeAllowed(Mock.Group);
        }

        private void ShouldBeAllowed(IClaimSource mockCharacter)
            => mockCharacter.ValidateIfCanAddClaim(Mock.Player.UserId).ShouldBeEmpty();

        private void ShouldBeNotAllowed(IClaimSource claimSource, AddClaimForbideReason reason)
            => claimSource.ValidateIfCanAddClaim(Mock.Player.UserId).ShouldContain(reason);
    }
}
