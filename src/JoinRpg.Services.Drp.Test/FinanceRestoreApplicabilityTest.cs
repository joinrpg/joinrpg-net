using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Services.Drp.ManualFinance;
using static JoinRpg.Services.Drp.Test.ClaimNotificationFactory;

namespace JoinRpg.Services.Drp.Test;

public class FinanceRestoreApplicabilityTest
{
    private static readonly DateTimeOffset At = Day(10);

    private static FinanceOperationToRestore Operation(
        FinanceRestoreStatus status = FinanceRestoreStatus.Restore,
        FinanceOperationType type = FinanceOperationType.Submit,
        FinanceOperationState state = FinanceOperationState.Approved,
        int? money = 1500)
        => new("op:a", OperationId: null, Master.UserId, IsByPlayer: false, type, money, At, "Текст",
            type == FinanceOperationType.Submit ? CommentExtraAction.PaidFee : CommentExtraAction.RefundFee,
            state, ModeratedAt: null,
            new PaymentTypeSuggestion(7, CreateCashFor: null, PaymentTypeGuess.Initiator, Candidates: []),
            status);

    private static FinanceModerationToRestore Moderation(FinanceRestoreStatus status)
        => new("mod:a", CommentId: null, Master.UserId, Approved: true, At, "Текст", FinanceModerationLink.NotFound,
            TargetOperationKey: null, TargetOperationId: null, status);

    [Theory]
    [InlineData(FinanceRestoreStatus.AlreadyExists, FinanceRestoreSkipReason.AlreadyExists)]
    [InlineData(FinanceRestoreStatus.Conflict, FinanceRestoreSkipReason.Conflict)]
    [InlineData(FinanceRestoreStatus.InvalidMoney, FinanceRestoreSkipReason.InvalidMoney)]
    public void NotRestoredStatusIsReason(FinanceRestoreStatus status, FinanceRestoreSkipReason expected)
    {
        Operation(status).NotApplicableReason().ShouldBe(expected);
        Moderation(status).NotApplicableReason().ShouldBe(expected);
    }

    [Fact]
    public void RestoredOperationAndModerationAreApplicable()
    {
        Operation().NotApplicableReason().ShouldBeNull();
        Moderation(FinanceRestoreStatus.Restore).NotApplicableReason().ShouldBeNull();
    }

    [Fact]
    public void OperationWithoutMoneyIsNotApplicable()
        => Operation(money: null).NotApplicableReason().ShouldBe(FinanceRestoreSkipReason.InvalidMoney);

    [Theory]
    [InlineData(FinanceOperationState.Proposed)]
    [InlineData(FinanceOperationState.Declined)]
    public void NotApprovedRefundIsNotApplicable(FinanceOperationState state)
        => Operation(type: FinanceOperationType.Refund, state: state, money: -100).NotApplicableReason()
            .ShouldBe(FinanceRestoreSkipReason.RefundNotApproved);

    [Fact]
    public void ApprovedRefundAndProposedPaymentAreApplicable()
    {
        Operation(type: FinanceOperationType.Refund, money: -100).NotApplicableReason().ShouldBeNull();
        Operation(state: FinanceOperationState.Proposed).NotApplicableReason().ShouldBeNull();
    }
}
