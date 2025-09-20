using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Domain.Test.AddClaim;

public class AddClaimValidationRulesTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void AddClaimAllowedCharacter() => ShouldBeAllowed(Mock.Character);

    [Fact]
    public void CantSentClaimToInactiveCharacter()
    {
        var inactive = Mock.CreateCharacter("inactive");
        inactive.IsActive = false;
        ShouldBeNotAllowed(inactive, AddClaimForbideReason.CharacterInactive);
    }

    [Fact]
    public void AddClaimAllowedCharacterWithoutUser() => Mock.Character.ValidateIfCanAddClaim(userInfo: null, Mock.ProjectInfo).ShouldBeEmpty();

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
        var another = Mock.CreateCharacter("another");
        ShouldBeNotAllowed(another, AddClaimForbideReason.OnlyOneCharacter);
    }

    [Fact]
    public void AllowSendClaimEvenIfHasApprovedAccordingToSettings()
    {
        Mock.Project.Details.EnableManyCharacters = true;
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldBeAllowed(another);
    }

    [Fact]
    public void AllowSendClaimEvenIfHasAnotherNotApproved()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldBeAllowed(another);
    }

    private void ShouldBeAllowed(Character mockCharacter)
        => mockCharacter.ValidateIfCanAddClaim(Mock.PlayerInfo, Mock.ProjectInfo).ShouldBeEmpty();

    private void ShouldBeNotAllowed(Character claimSource, AddClaimForbideReason reason)
        => claimSource.ValidateIfCanAddClaim(Mock.PlayerInfo, Mock.ProjectInfo).ShouldContain(reason);
}
