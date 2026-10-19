using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.WebPortal.Managers.Drp;

namespace JoinRpg.WebPortal.Managers.Test.Drp;

public class FinanceRestorePlanViewBuilderTest
{
    private static readonly DateTimeOffset At = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly MockedProject mock = new();

    private UserIdentification Master => new(mock.Master.UserId);

    private ClaimIdentification Claim => new(mock.ProjectInfo.ProjectId, 5);

    private FinanceOperationToRestore Operation(
        string key,
        FinanceOperationType type = FinanceOperationType.Submit,
        int money = 500,
        FinanceOperationState state = FinanceOperationState.Approved,
        FinanceRestoreStatus status = FinanceRestoreStatus.Restore)
        => new(key, null, Master, IsByPlayer: true, type, money, At, "Текст",
            type == FinanceOperationType.Submit ? CommentExtraAction.PaidFee : CommentExtraAction.RefundFee,
            state, ModeratedAt: At.AddDays(1), new PaymentTypeSuggestion(null, null, PaymentTypeGuess.NotFound, []), status);

    private FinanceModerationToRestore Moderation(FinanceModerationLink link, string? targetKey = null, int? targetId = null)
        => new("mod", null, Master, Approved: true, At.AddDays(1), "Текст", link, targetKey, targetId, FinanceRestoreStatus.Restore);

    private FinanceRestoreModerationView BuildModeration(FinanceModerationToRestore moderation, params FinanceOperationToRestore[] operations)
    {
        var row = new FinanceRestorePlanRow(Claim, ClaimExists: true, operations, [moderation], SkippedForMissingClaim: 0,
            Transfers: 0, PreferentialRequests: 0, OnlinePayments: 0, Unparsed: 0, Warnings: []);
        var model = FinanceRestorePlanViewBuilder.Build(mock.ProjectInfo, Master, [row], [], []);
        return model.Rows.ShouldHaveSingleItem().Moderations.ShouldHaveSingleItem();
    }

    [Fact]
    public void EveryWarningHasView()
        => Enum.GetValues<FinanceRestoreWarning>().Select(FinanceRestorePlanViewBuilder.ToWarningView).Distinct().Count()
            .ShouldBe(Enum.GetValues<FinanceRestoreWarning>().Length);

    [Fact]
    public void ModerationOfRestoredOperation_NamesIt()
    {
        var moderation = BuildModeration(Moderation(FinanceModerationLink.RestoredOperation, targetKey: "op"), Operation("op"));

        moderation.CanApply.ShouldBeTrue();
        // Текст со временем собирает панель — в поясе браузера, как и таблицу (#5647).
        moderation.Note.ShouldBeNull();
        moderation.TargetOperation.ShouldBe(new FinanceRestoreEntryRef(FinanceRestoreEntryKind.Payment, 500, At));
    }

    [Fact]
    public void ModerationOfUnapprovedRefund_NotApplicable()
    {
        var moderation = BuildModeration(
            Moderation(FinanceModerationLink.RestoredOperation, targetKey: "op"),
            Operation("op", FinanceOperationType.Refund, money: -200, state: FinanceOperationState.Declined));

        moderation.CanApply.ShouldBeFalse();
        moderation.Note.ShouldBe("Относится к операции, которая не восстановится, — проверьте вручную");
        moderation.TargetOperation.ShouldBeNull();
    }

    [Fact]
    public void ModerationOfNotRestoredOperation_NotApplicable()
    {
        var moderation = BuildModeration(
            Moderation(FinanceModerationLink.NotRestoredOperation, targetKey: "op"),
            Operation("op", status: FinanceRestoreStatus.InvalidMoney));

        moderation.CanApply.ShouldBeFalse();
    }

    /// <summary>Выбор типа в панели хранится по ключу: при повторе ключа не понять, к какой операции он относится.</summary>
    [Fact]
    public void RepeatedKey_NotApplicable_AndItsModerationToo()
    {
        var row = new FinanceRestorePlanRow(Claim, ClaimExists: true, [Operation("op"), Operation("op", money: 700)],
            [Moderation(FinanceModerationLink.RestoredOperation, targetKey: "op")], SkippedForMissingClaim: 0,
            Transfers: 0, PreferentialRequests: 0, OnlinePayments: 0, Unparsed: 0, Warnings: []);

        var view = FinanceRestorePlanViewBuilder.Build(mock.ProjectInfo, Master, [row], [], []).Rows.ShouldHaveSingleItem();

        view.Operations.ShouldAllBe(o => !o.CanApply && o.NotApplicableReason == FinanceRestorePlanViewBuilder.AmbiguousKeyText);
        view.Moderations.ShouldHaveSingleItem().CanApply.ShouldBeFalse();
    }

    private int AddCash(bool isActive)
    {
        var paymentType = mock.CreateCashPaymentType();
        paymentType.IsActive = isActive;
        mock.ReInitProjectInfo();
        return paymentType.PaymentTypeId;
    }

    private FinanceRestorePlanViewModel BuildWithOneOperation()
    {
        var operation = Operation("op") with { PaymentType = new PaymentTypeSuggestion(null, Master, PaymentTypeGuess.NotFound, []) };
        var row = new FinanceRestorePlanRow(Claim, ClaimExists: true, [operation], [], SkippedForMissingClaim: 0,
            Transfers: 0, PreferentialRequests: 0, OnlinePayments: 0, Unparsed: 0, Warnings: []);
        return FinanceRestorePlanViewBuilder.Build(mock.ProjectInfo, Master, [row], [], []);
    }

    /// <summary>В старых данных у мастера бывает два наличных типа: план строится, предлагается включённый.</summary>
    [Fact]
    public void TwoOwnCashTypes_PlanBuilds_EnabledSuggested()
    {
        _ = AddCash(isActive: false);
        var enabled = AddCash(isActive: true);

        var model = BuildWithOneOperation();

        model.HasOwnCash.ShouldBeTrue();
        model.Rows.ShouldHaveSingleItem().Operations.ShouldHaveSingleItem().SuggestedPaymentTypeId.ShouldBe(enabled);
    }

    /// <summary>Свой выключенный «наличные» — в списке выбора: создать его панель не предложит, иначе выбрать нечего.</summary>
    [Fact]
    public void OwnDisabledCash_IsListed()
    {
        var cash = AddCash(isActive: false);
        var row = new FinanceRestorePlanRow(Claim, ClaimExists: true, [Operation("op")], [], SkippedForMissingClaim: 0,
            Transfers: 0, PreferentialRequests: 0, OnlinePayments: 0, Unparsed: 0, Warnings: []);

        var model = FinanceRestorePlanViewBuilder.Build(mock.ProjectInfo, Master, [row], [], []);

        model.HasOwnCash.ShouldBeTrue();
        var paymentType = model.PaymentTypes.ShouldHaveSingleItem();
        paymentType.PaymentTypeId.ShouldBe(cash);
        paymentType.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void ModerationOfExistingOperation_ApplicableWithItsNumber()
    {
        var moderation = BuildModeration(Moderation(FinanceModerationLink.ExistingOperation, targetId: 500));

        moderation.CanApply.ShouldBeTrue();
        moderation.Note.ShouldBe("К операции № 500, которая уже есть в базе");
    }
}
