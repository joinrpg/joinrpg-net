using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Comments;

namespace JoinRpg.DomainTypes.Test.Comments;

/// <summary>
/// Правило «операция требует модерации» — то же, что <c>FinanceOperation.RequireModeration</c> в DataModel.
/// </summary>
public class CommentFinanceInfoTest
{
    private static CommentFinanceInfo Finance(FinanceOperationType type, FinanceOperationState state)
        => new(500, type, state, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), PaymentType: null);

    [Theory]
    [InlineData(FinanceOperationType.Submit)]
    [InlineData(FinanceOperationType.PreferentialFeeRequest)]
    public void ProposedManualOperation_RequiresModeration(FinanceOperationType type)
        => Finance(type, FinanceOperationState.Proposed).RequireModeration.ShouldBeTrue();

    [Fact]
    public void ProposedOnlinePayment_IsNotModerated()
        // Онлайн-оплату в состоянии «предложена» подтверждает банк, а не мастер.
        => Finance(FinanceOperationType.Online, FinanceOperationState.Proposed).RequireModeration.ShouldBeFalse();

    [Theory]
    [InlineData(FinanceOperationType.Refund)]
    [InlineData(FinanceOperationType.TransferTo)]
    [InlineData(FinanceOperationType.TransferFrom)]
    public void ProposedOtherOperation_IsNotModerated(FinanceOperationType type)
        => Finance(type, FinanceOperationState.Proposed).RequireModeration.ShouldBeFalse();

    [Theory]
    [InlineData(FinanceOperationState.Approved)]
    [InlineData(FinanceOperationState.Declined)]
    [InlineData(FinanceOperationState.Invalid)]
    [InlineData(FinanceOperationState.Expired)]
    public void DecidedOperation_IsNotModerated(FinanceOperationState state)
        => Finance(FinanceOperationType.Submit, state).RequireModeration.ShouldBeFalse();
}
