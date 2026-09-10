using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.AddClaim;

public class MoveClaimValidationRulesTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void DisallowMoveClaimFromCharacterToEmptySlot()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var slot = Mock.CreateCharacter("slot");
        slot.CharacterType = CharacterType.Slot;
        slot.CharacterSlotLimit = 0;
        ShouldDisAllowMove(claim, slot, AddClaimForbideReason.SlotsExhausted);
    }

    [Fact]
    public void AllowMoveClaimFromCharacterToCharacter()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        ShouldAllowMove(claim, another);
    }

    [Fact]
    public void CantMoveApprovedClaimFromCharacterToSlot()
    {
        var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        another.CharacterType = CharacterType.Slot;
        another.CharacterSlotLimit = null;
        ShouldDisAllowMove(claim, another, AddClaimForbideReason.ApprovedClaimMovedToSlot);
    }


    [Fact]
    public void CantMoveCheckedInClaimFromCharacterToCharacter()
    {
        var claim = Mock.CreateCheckedInClaim(Mock.Character, Mock.Player);
        ShouldDisAllowMove(claim, Mock.CreateCharacter("another"), AddClaimForbideReason.CheckedInClaimCantBeMoved);
    }


    [Fact]
    public void AllowMoveApprovedClaimFromCharacterToCharacter()
    {
        var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        ShouldAllowMove(claim, Mock.CreateCharacter("another"));
    }

    // Перенос выполняет мастер, поэтому незаполненные контакты игрока ему не мешают: игрок эту
    // заявку уже подал, требовать от мастера дозаполнить чужой профиль бессмысленно.
    [Fact]
    public void MasterCanMoveClaimOfPlayerWithoutRequiredContacts()
    {
        var projectInfo = Mock.ProjectInfo.WithProfileRequirementSettings(
            ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });
        var playerWithoutPhone = Mock.PlayerInfo with { PhoneNumber = null };
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        Mock.CreateCharacter("another")
            .ValidateIfCanMoveClaim(claim, playerWithoutPhone, projectInfo)
            .ShouldBeEmpty();
    }

    [Fact]
    public void MasterCanMoveClaimWhenClaimsClosed()
    {
        var claimsClosed = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);

        Mock.CreateCharacter("another")
            .ValidateIfCanMoveClaim(claim, Mock.PlayerInfo, claimsClosed)
            .ShouldBeEmpty();
    }

    // ...но целостность данных обход мастером не отменяет.
    [Fact]
    public void MasterStillCantMoveCheckedInClaimWhenClaimsClosed()
    {
        var claimsClosed = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);
        var claim = Mock.CreateCheckedInClaim(Mock.Character, Mock.Player);

        Mock.CreateCharacter("another")
            .ValidateIfCanMoveClaim(claim, Mock.PlayerInfo, claimsClosed).Kinds()
            .ShouldBe([AddClaimForbideReason.CheckedInClaimCantBeMoved]);
    }

    // Правила переноса не должны срабатывать на подаче заявки: они гейтятся операцией, а не
    // тем, передали ли существующую заявку.
    [Fact]
    public void MoveOnlyRulesDoNotApplyToAddingClaim()
    {
        var slot = Mock.CreateCharacter("slot");
        slot.CharacterType = CharacterType.Slot;
        slot.CharacterSlotLimit = null;

        slot.ValidateIfCanAddClaim(Mock.PlayerInfo, Mock.ProjectInfo, ClaimOperation.AddByPlayer).Kinds()
            .ShouldNotContain(AddClaimForbideReason.ApprovedClaimMovedToSlot);
    }

    private void ShouldAllowMove(Claim claim, Character character) => character.ValidateIfCanMoveClaim(claim, Mock.PlayerInfo, Mock.ProjectInfo).ShouldBeEmpty();

    private void ShouldDisAllowMove(Claim claim, Character character, AddClaimForbideReason reason)
        => character.ValidateIfCanMoveClaim(claim, Mock.PlayerInfo, Mock.ProjectInfo).Kinds().ShouldBe([reason]);
}
