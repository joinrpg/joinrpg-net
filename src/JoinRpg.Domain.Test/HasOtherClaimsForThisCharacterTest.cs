using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test;

/// <summary>
/// «Утвердить заявку мешают чужие заявки на того же персонажа» поверх агрегата (ADR021). Флаг
/// предупреждает мастера на странице заявки, поэтому важно и не пропустить конкурента, и не
/// поднять тревогу там, где конкуренции нет.
/// </summary>
public class HasOtherClaimsForThisCharacterTest
{
    private readonly MockedProject mock = new();

    private ClaimInCharacter Load(Claim claim) => new(mock.GetCharacterInfo(claim.Character), claim.GetId());

    [Fact]
    public void ActiveClaimOfAnotherPlayerBlocks()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        _ = mock.CreateClaim(mock.Character, mock.Master);

        Load(claim).HasOtherClaimsForThisCharacter().ShouldBeTrue();
    }

    [Fact]
    public void NoOtherClaimsDoesNotBlock()
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);

        Load(claim).HasOtherClaimsForThisCharacter().ShouldBeFalse();
    }

    [Theory]
    [InlineData(ClaimStatus.OnHold)]
    [InlineData(ClaimStatus.DeclinedByMaster)]
    [InlineData(ClaimStatus.DeclinedByUser)]
    public void InactiveClaimOfAnotherPlayerDoesNotBlock(ClaimStatus otherStatus)
    {
        var claim = mock.CreateClaim(mock.Character, mock.Player);
        var other = mock.CreateClaim(mock.Character, mock.Master);
        other.ClaimStatus = otherStatus;

        Load(claim).HasOtherClaimsForThisCharacter().ShouldBeFalse();
    }

    [Fact]
    public void ApprovedClaimIsNotBlocked()
    {
        var claim = mock.CreateApprovedClaim(mock.Character, mock.Player);
        _ = mock.CreateClaim(mock.Character, mock.Master);

        Load(claim).HasOtherClaimsForThisCharacter().ShouldBeFalse();
    }

    [Fact]
    public void SlotIsNeverBlocked()
    {
        var slot = mock.CreateSlot("Слот");
        var claim = mock.CreateClaim(slot, mock.Player);
        _ = mock.CreateClaim(slot, mock.Master);

        Load(claim).HasOtherClaimsForThisCharacter().ShouldBeFalse();
    }
}
