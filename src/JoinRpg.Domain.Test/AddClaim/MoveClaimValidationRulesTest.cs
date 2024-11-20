using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using Shouldly;
using Xunit;

namespace JoinRpg.Domain.Test.AddClaim;

public class MoveClaimValidationRulesTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void DisallowMoveClaimFromCharacterToEmptySlot()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        var slot = Mock.CreateCharacter("slot");
        slot.CharacterType = PrimitiveTypes.CharacterType.Slot;
        slot.CharacterSlotLimit = 0;
        ShouldDisAllowMove(claim, slot, AddClaimForbideReason.SlotsExhausted);
    }

    [Fact]
    public void AllowMoveClaimFromCharacterToCharacter()
    {
        var claim = Mock.CreateClaim(Mock.Character, Mock.Player);
        ShouldAllowMove(claim, Mock.CharacterWithoutGroup);
    }

    [Fact]
    public void CantMoveApprovedClaimFromCharacterToGroup()
    {
        var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        var another = Mock.CreateCharacter("another");
        another.CharacterType = PrimitiveTypes.CharacterType.Slot;
        another.CharacterSlotLimit = null;
        ShouldDisAllowMove(claim, another, AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot);
    }


    [Fact]
    public void CantMoveCheckedInClaimFromCharacterToCharacter()
    {
        var claim = Mock.CreateCheckedInClaim(Mock.Character, Mock.Player);
        ShouldDisAllowMove(claim, Mock.CharacterWithoutGroup, AddClaimForbideReason.CheckedInClaimCantBeMoved);
    }


    [Fact]
    public void AllowMoveApprovedClaimFromCharacterToCharacter()
    {
        var claim = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);
        ShouldAllowMove(claim, Mock.CharacterWithoutGroup);
    }

    private static void ShouldAllowMove(Claim claim, Character character) => character.ValidateIfCanMoveClaim(claim).ShouldBeEmpty();

    private static void ShouldDisAllowMove(Claim claim, Character character, AddClaimForbideReason reason)
        => character.ValidateIfCanMoveClaim(claim).ShouldBe([reason]);
}
