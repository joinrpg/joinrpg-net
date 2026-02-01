using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Test.AddClaim;

public class AddClaimValidationRulesTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void AddClaimAllowedCharacter() => ShouldBeAllowed(Mock.Character, Mock.ProjectInfo);

    [Fact]
    public void CantSentClaimToInactiveCharacter()
    {
        var inactive = Mock.CreateCharacter("inactive");
        inactive.IsActive = false;
        ShouldBeNotAllowed(inactive, AddClaimForbideReason.CharacterInactive, Mock.ProjectInfo);
    }

    [Fact]
    public void AddClaimAllowedCharacterWithoutUser() => Mock.Character.ValidateIfCanAddClaim(userInfo: null, Mock.ProjectInfo).ShouldBeEmpty();

    [Fact]
    public void CantSendClaimIfProjectClaimsClosed()
    {
        var projectInfo = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.ProjectClaimsClosed, projectInfo);
    }

    [Fact]
    public void CantSendClaimIfProjectClosed()
    {
        var projectInfo = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.Archived);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.ProjectNotActive, projectInfo);
    }

    [Fact]
    public void CantSendClaimIfNoSlotsChar()
    {
        Mock.Character.CharacterType = PrimitiveTypes.CharacterType.Slot;
        Mock.Character.CharacterSlotLimit = 0;
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.SlotsExhausted, Mock.ProjectInfo);
    }


    [Fact]
    public void CantSendClaimIfCharacterIsNpc()
    {
        Mock.Character.CharacterType = PrimitiveTypes.CharacterType.NonPlayer;
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Npc, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimIfCharacterHasApprovedClaim()
    {
        Mock.Character.ApprovedClaim = new Claim();
        Mock.Character.ApprovedClaimId = -1;
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Busy, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimIfCharacterHasCheckedInClaim()
    {
        _ = Mock.CreateCheckedInClaim(Mock.Character, Mock.Master);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.Busy, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimToSameCharacter()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.AlreadySent, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimToSameCharacterEvenProjectSettingsAllowsMultiple()
    {
        Mock.Project.Details.EnableManyCharacters = true;
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        ShouldBeNotAllowed(Mock.Character, AddClaimForbideReason.AlreadySent, Mock.ProjectInfo);
    }

    [Fact]
    public void CantSendClaimIfHasApproved()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldBeNotAllowed(another, AddClaimForbideReason.OnlyOneCharacter, Mock.ProjectInfo);
    }

    [Fact]
    public void AllowSendClaimEvenIfHasApprovedAccordingToSettings()
    {
        var projectInfo = Mock.ProjectInfo.WithAllowManyClaims(strictlyOneCharacter: false);
        var
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldBeAllowed(another, projectInfo);
    }

    [Fact]
    public void AllowSendClaimEvenIfHasAnotherNotApproved()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldBeAllowed(another, Mock.ProjectInfo);
    }

    private void ShouldBeAllowed(Character mockCharacter, ProjectInfo projectInfo)
        => mockCharacter.ValidateIfCanAddClaim(Mock.PlayerInfo, projectInfo).ShouldBeEmpty();

    private void ShouldBeNotAllowed(Character claimSource, AddClaimForbideReason reason, ProjectInfo projectInfo)
    {
        claimSource.ValidateIfCanAddClaim(Mock.PlayerInfo, projectInfo).ShouldContain(reason);
    }
}
