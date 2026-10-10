using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;

namespace JoinRpg.Domain.Test;

/// <summary>
/// Фиксация взноса по полностью оплаченной заявке: решение принимается по доменному снимку
/// (ADR022), снятому до операции, плюс сумма, которую добавила сама операция.
/// </summary>
public class UpdateClaimFeeIfRequiredTest
{
    private static readonly DateTime FeeStart = new(2026, 3, 1);
    private static readonly DateTime FeeRise = new(2026, 4, 1);

    private readonly MockedProject mock = new();
    private readonly Claim claim;

    public UpdateClaimFeeIfRequiredTest()
    {
        mock.Project.ProjectFeeSettings.Add(
            new ProjectFeeSetting { StartDate = FeeStart, Fee = 1000, PreferentialFee = 400 });
        mock.Project.ProjectFeeSettings.Add(
            new ProjectFeeSetting { StartDate = FeeRise, Fee = 1500, PreferentialFee = 600 });
        var tent = mock.CreateAccommodationType(cost: 300);
        mock.ReInitProjectInfo();

        var character = mock.CreateCharacter("Платящий");
        claim = mock.CreateApprovedClaim(character, mock.Player);
        claim.FinanceOperations = [];
        _ = mock.CreateAccommodationRequest(tent, claim);
    }

    private ClaimInCharacter Snapshot() => new(mock.GetCharacterInfo(claim.Character), claim.GetId());

    [Fact]
    public void AccommodationCostIsPartOfFullPayment()
    {
        claim.UpdateClaimFeeIfRequired(Snapshot(), new DateTime(2026, 3, 15), paymentAdded: 1000);

        claim.CurrentFee.ShouldBeNull();
    }

    [Fact]
    public void PaymentOfOperationFixesFee()
    {
        claim.UpdateClaimFeeIfRequired(Snapshot(), new DateTime(2026, 3, 15), paymentAdded: 1000 + 300);

        claim.CurrentFee.ShouldBe(1000);
    }

    [Fact]
    public void PaymentAlreadyInSnapshotCounts()
    {
        claim.FinanceOperations.Add(new FinanceOperation
        {
            MoneyAmount = 1000,
            State = FinanceOperationState.Approved,
            OperationType = FinanceOperationType.Submit,
        });

        claim.UpdateClaimFeeIfRequired(Snapshot(), new DateTime(2026, 3, 15), paymentAdded: 300);

        claim.CurrentFee.ShouldBe(1000);
    }

    [Fact]
    public void PaymentOnRiseDayIsCheckedAgainstOldFee()
    {
        claim.UpdateClaimFeeIfRequired(Snapshot(), FeeRise, paymentAdded: 1000 + 300);

        // Полнота оплаты — по цене накануне, фиксируется цена на дату операции.
        claim.CurrentFee.ShouldBe(1500);
    }

    [Fact]
    public void PreferentialFeeApprovedInSameOperationIsTakenIntoAccount()
    {
        var snapshot = Snapshot();
        claim.PreferentialFeeUser = true;

        claim.UpdateClaimFeeIfRequired(snapshot, new DateTime(2026, 3, 15), paymentAdded: 400 + 300);

        claim.CurrentFee.ShouldBe(400);
    }

    [Fact]
    public void AlreadyFixedFeeIsNotChanged()
    {
        claim.CurrentFee = 700;

        claim.UpdateClaimFeeIfRequired(Snapshot(), new DateTime(2026, 3, 15), paymentAdded: 5000);

        claim.CurrentFee.ShouldBe(700);
    }

    [Fact]
    public void SnapshotOfAnotherClaimThrows()
    {
        var other = mock.CreateApprovedClaim(mock.CreateCharacter("Другой"), mock.Player);
        var foreign = new ClaimInCharacter(mock.GetCharacterInfo(other.Character), other.GetId());

        _ = Should.Throw<ArgumentException>(
            () => claim.UpdateClaimFeeIfRequired(foreign, new DateTime(2026, 3, 15), paymentAdded: 0));
    }
}
