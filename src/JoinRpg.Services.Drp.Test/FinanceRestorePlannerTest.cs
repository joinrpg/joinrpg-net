using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ManualFinance;
using static JoinRpg.Services.Drp.Test.ClaimNotificationFactory;

namespace JoinRpg.Services.Drp.Test;

public class FinanceRestorePlannerTest
{
    private static readonly DateTimeOffset BackupAt = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);

    private static readonly ClaimIdentification OtherClaim = new(Project, 5000);

    private static readonly UserInfoHeader Cashier = User(30, "Казначей Казначеев");

    private static ClaimCommentIdentification CommentRef(int id) => new(Claim, id);

    private static ProjectPaymentTypeInfo Cash(int id, UserInfoHeader owner, bool active = true) => new(id, PaymentTypeKind.Cash, owner.UserId, active);

    private static ProjectPaymentTypeInfo Custom(int id, UserInfoHeader owner, bool active = true) => new(id, PaymentTypeKind.Custom, owner.UserId, active);

    private static ProjectPaymentTypeInfo Online(int id, UserInfoHeader owner) => new(id, PaymentTypeKind.Online, owner.UserId, true);

    private static ExistingFinanceOperationInfo OperationInDb(int id, FinanceOperationType type, FinanceOperationState state, int money, DateTimeOffset at, ClaimIdentification? claim = null)
        => new(id, claim ?? Claim, type, state, money, at);

    private static ExistingFinanceCommentInfo CommentInDb(int id, UserInfoHeader author, DateTimeOffset at, CommentExtraAction action, ClaimIdentification? claim = null)
        => new(id, claim ?? Claim, author.UserId, at, action);

    private static IReadOnlyList<ClaimNotificationEvent> Events(params IEnumerable<NotificationMessageForRecipient>[] rows)
        => ClaimNotificationParser.Parse(rows.SelectMany(r => r));

    private static IReadOnlyList<FinanceRestorePlanRow> Plan(
        IReadOnlyCollection<ClaimNotificationEvent> events,
        IReadOnlyCollection<ClaimIdentification>? claims = null,
        IReadOnlyCollection<ExistingFinanceOperationInfo>? operations = null,
        IReadOnlyCollection<ExistingFinanceCommentInfo>? comments = null,
        IReadOnlyCollection<int>? taken = null,
        IReadOnlyCollection<ProjectPaymentTypeInfo>? paymentTypes = null)
        => FinanceRestorePlanner.Plan(new FinanceRestoreSource(
            events, claims ?? [Claim], operations ?? [], comments ?? [], taken ?? [], paymentTypes ?? [Cash(1, Master)], BackupAt));

    private static IReadOnlyList<FinanceRestorePlanRow> Plan(params IEnumerable<NotificationMessageForRecipient>[] rows) => Plan(Events(rows));

    private static FinanceRestorePlanRow SingleRow(IReadOnlyList<FinanceRestorePlanRow> plan)
    {
        var row = plan.ShouldHaveSingleItem();
        row.ClaimExists.ShouldBeTrue();
        return row;
    }

    private static FinanceOperationToRestore SingleOperation(IReadOnlyList<FinanceRestorePlanRow> plan)
    {
        var row = SingleRow(plan);
        row.Warnings.ShouldBeEmpty();
        return row.Operations.ShouldHaveSingleItem();
    }

    private static IEnumerable<NotificationMessageForRecipient> MasterPaid(DateTimeOffset at, int money = 1500, IProjectEntityId? reference = null, UserInfoHeader? master = null, IEnumerable<UserInfoHeader>? recipients = null)
        => ClaimChange(at, CommentExtraAction.PaidFee, master ?? Master, recipients ?? [Player], text: "Принято", money: money, reference: reference);

    private static IEnumerable<NotificationMessageForRecipient> PlayerPaid(DateTimeOffset at, int money = 1500, IProjectEntityId? reference = null, IEnumerable<UserInfoHeader>? recipients = null)
        => ClaimChange(at, CommentExtraAction.PaidFee, Player, recipients ?? [Master], text: "Перевёл", money: money, reference: reference);

    private static IEnumerable<NotificationMessageForRecipient> Moderation(DateTimeOffset at, bool approved = true, IProjectEntityId? reference = null)
        => ClaimChange(at, approved ? CommentExtraAction.ApproveFinance : CommentExtraAction.RejectFinance, Master, [Player], text: "", reference: reference);

    private static NotificationMessageForRecipient OnlinePayment(DateTimeOffset at)
        => Row($"{ProjectTitle}: Персонаж, игрок {Player.DisplayName.DisplayName}", "Онлайн-оплата на сумму 1500.00₽ подтверждена.",
            User(1, "Оплаты"), Master, new FinanceOperationIdentification(Project, Claim.ClaimId, 55), at);

    [Fact]
    public void EmptyInputGivesEmptyPlan() => Plan([]).ShouldBeEmpty();

    [Fact]
    public void MasterPaymentWithIdIsRestoredApproved()
    {
        var operation = SingleOperation(Plan(MasterPaid(Day(1), reference: CommentRef(555))));

        operation.ShouldBe(new FinanceOperationToRestore("id:555", 555, Master.UserId, IsByPlayer: false, FinanceOperationType.Submit, 1500, Day(1),
            "Принято", CommentExtraAction.PaidFee, FinanceOperationState.Approved, ModeratedAt: null,
            operation.PaymentType, FinanceRestoreStatus.Restore));
        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(1, null, PaymentTypeGuess.Initiator, []));
    }

    [Fact]
    public void PaymentWithoutIdGetsKeyFromClaimTimeAndInitiator()
    {
        var operation = SingleOperation(Plan(MasterPaid(Day(1), reference: Claim)));

        operation.OperationId.ShouldBeNull();
        operation.Key.ShouldBe($"op:{Claim.ClaimId}:{Day(1).UtcTicks}:{Master.UserId.Value}");
    }

    [Fact]
    public void PlayerPaymentIsProposed()
    {
        var operation = SingleOperation(Plan(PlayerPaid(Day(1))));

        operation.IsByPlayer.ShouldBeTrue();
        operation.State.ShouldBe(FinanceOperationState.Proposed);
        operation.Initiator.ShouldBe(Player.UserId);
    }

    [Fact]
    public void RefundIsRestoredWithNegativeMoney()
    {
        var operation = SingleOperation(Plan(ClaimChange(Day(1), CommentExtraAction.RefundFee, Master, [Player], money: -700)));

        operation.Type.ShouldBe(FinanceOperationType.Refund);
        operation.Money.ShouldBe(-700);
        operation.ExtraAction.ShouldBe(CommentExtraAction.RefundFee);
        operation.Status.ShouldBe(FinanceRestoreStatus.Restore);
    }

    [Theory]
    [InlineData(CommentExtraAction.PaidFee, -1500)]
    [InlineData(CommentExtraAction.RefundFee, 700)]
    [InlineData(CommentExtraAction.PaidFee, 0)]
    public void MoneySignMismatchIsNotRestored(CommentExtraAction action, int money)
    {
        var row = SingleRow(Plan(ClaimChange(Day(1), action, Master, [Player], money: money)));

        row.Operations.ShouldHaveSingleItem().Status.ShouldBe(FinanceRestoreStatus.InvalidMoney);
        row.Warnings.ShouldBe([FinanceRestoreWarning.InvalidMoney]);
    }

    [Fact]
    public void PaymentWithoutMoneyIsNotRestored()
    {
        var row = SingleRow(Plan(ClaimChange(Day(1), CommentExtraAction.PaidFee, Master, [Player])));

        var operation = row.Operations.ShouldHaveSingleItem();
        operation.Money.ShouldBeNull();
        operation.Status.ShouldBe(FinanceRestoreStatus.InvalidMoney);
    }

    [Fact]
    public void CopiesForRecipientsGiveOneOperation()
        => SingleOperation(Plan(MasterPaid(Day(1), reference: CommentRef(555), recipients: [Player, OtherMaster])));

    [Fact]
    public void CopiesOfOneEventMergeRecipients()
    {
        // Копии, которые парсер не склеил: владелец типа оплаты — только во второй.
        var toPlayer = Events(PlayerPaid(Day(1), reference: Claim, recipients: [Master])).ShouldHaveSingleItem();
        var toCashier = Events(PlayerPaid(Day(1), reference: Claim, recipients: [Cashier])).ShouldHaveSingleItem();

        var operation = SingleOperation(Plan([toPlayer, toCashier, toPlayer], paymentTypes: [Cash(3, Cashier)]));

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(3, null, PaymentTypeGuess.Recipient, []));
    }

    [Fact]
    public void EventsBeforeBackupAreIgnored()
        => Plan(MasterPaid(BackupAt.AddTicks(-1)), Moderation(BackupAt.AddTicks(-1))).ShouldBeEmpty();

    [Fact]
    public void EventAtBackupMomentIsRestored()
        => SingleOperation(Plan(MasterPaid(BackupAt))).CreatedAt.ShouldBe(BackupAt);

    [Fact]
    public void NonFinanceEventsGiveNoRow()
        => Plan(
            [OnlinePayment(Day(1))],
            ClaimChange(Day(2), action: null, Master, [Player]),
            ClaimChange(Day(3), CommentExtraAction.FeeChanged, Master, [Player], text: "2000"))
        .ShouldBeEmpty();

    [Fact]
    public void TransfersPreferentialAndOnlineAreOnlyCounted()
    {
        var row = SingleRow(Plan(
            [OnlinePayment(Day(1))],
            ClaimChange(Day(2), CommentExtraAction.TransferTo, Master, [Player]),
            ClaimChange(Day(3), CommentExtraAction.TransferFrom, Master, [Player]),
            ClaimChange(Day(4), CommentExtraAction.RequestPreferential, Player, [Master])));

        row.Operations.ShouldBeEmpty();
        row.Moderations.ShouldBeEmpty();
        row.Transfers.ShouldBe(2);
        row.PreferentialRequests.ShouldBe(1);
        row.OnlinePayments.ShouldBe(1);
        row.Warnings.ShouldBe([FinanceRestoreWarning.TransfersNotRestored]);
    }

    [Fact]
    public void UnparsedEventsAreWarned()
    {
        var invitation = Row($"{ProjectTitle}: приглашение на поселение", "Добрый день, Игрок Игроков\n\nВас пригласили жить вместе",
            Master, Player, CommentRef(556), Day(1));

        var row = SingleRow(Plan([invitation], MasterPaid(Day(2))));

        row.Unparsed.ShouldBe(1);
        row.Warnings.ShouldBe([FinanceRestoreWarning.UnparsedEvents]);
    }

    [Fact]
    public void MissingClaimIsWarnedAndCounted()
    {
        var row = Plan(Events(MasterPaid(Day(1)), PlayerPaid(Day(2)), Moderation(Day(3)),
                ClaimChange(Day(4), CommentExtraAction.TransferTo, Master, [Player])),
            claims: [])
            .ShouldHaveSingleItem();

        row.ClaimExists.ShouldBeFalse();
        row.Operations.ShouldBeEmpty();
        row.Moderations.ShouldBeEmpty();
        row.SkippedForMissingClaim.ShouldBe(3);
        row.Transfers.ShouldBe(1);
        row.Warnings.ShouldBe([FinanceRestoreWarning.ClaimNotFound, FinanceRestoreWarning.TransfersNotRestored]);
    }

    [Fact]
    public void RowsByClaimIdOperationsByTime()
    {
        var rows = Plan(Events(
                MasterPaid(Day(4), money: 2),
                ClaimChange(Day(1), CommentExtraAction.PaidFee, Master, [Player], money: 3, claimId: OtherClaim),
                MasterPaid(Day(3), money: 1)),
            claims: [Claim, OtherClaim]);

        rows.Select(r => (r.ClaimId, string.Join(",", r.Operations.Select(o => o.Money)))).ShouldBe([
            (Claim, "1,2"),
            (OtherClaim, "3"),
        ]);
    }

    [Fact]
    public void OperationIdAlreadyInClaimIsExisting()
    {
        var row = SingleRow(Plan(Events(MasterPaid(Day(1), reference: CommentRef(555)), MasterPaid(Day(2), reference: CommentRef(556))),
            operations: [OperationInDb(555, FinanceOperationType.Submit, FinanceOperationState.Approved, 1500, Day(1))],
            comments: [CommentInDb(556, Master, Day(2), CommentExtraAction.PaidFee)]));

        row.Operations.Select(o => o.Status).ShouldBe([FinanceRestoreStatus.AlreadyExists, FinanceRestoreStatus.AlreadyExists]);
        row.AlreadyExisting.ShouldBe(2);
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void TakenIdIsConflict()
    {
        var row = SingleRow(Plan(Events(MasterPaid(Day(1), reference: CommentRef(555))), taken: [555]));

        row.Operations.ShouldHaveSingleItem().Status.ShouldBe(FinanceRestoreStatus.Conflict);
        row.Conflicts.ShouldBe([555]);
        row.Warnings.ShouldBe([FinanceRestoreWarning.IdConflict]);
    }

    [Fact]
    public void IdOfAnotherClaimIsConflict()
    {
        var row = Plan(Events(MasterPaid(Day(1), reference: CommentRef(555)), Moderation(Day(2), reference: CommentRef(556))),
                claims: [Claim, OtherClaim],
                operations: [OperationInDb(555, FinanceOperationType.Submit, FinanceOperationState.Approved, 1500, Day(1), OtherClaim)],
                comments: [CommentInDb(556, Master, Day(2), CommentExtraAction.ApproveFinance, OtherClaim)])
            .ShouldHaveSingleItem();

        row.ClaimId.ShouldBe(Claim);
        row.Conflicts.ShouldBe([555, 556]);
        row.Warnings.ShouldContain(FinanceRestoreWarning.IdConflict);
    }

    [Fact]
    public void PaymentWithoutIdMatchesDbByTypeMoneyAndTime()
    {
        var row = SingleRow(Plan(Events(
                MasterPaid(Day(1), money: 1500, reference: Claim),
                MasterPaid(Day(1).AddSeconds(30), money: 1500, reference: Claim),
                MasterPaid(Day(2), money: 1000, reference: Claim),
                MasterPaid(Day(3).AddMinutes(2), money: 500, reference: Claim)),
            operations: [
                OperationInDb(50, FinanceOperationType.Submit, FinanceOperationState.Approved, 1500, Day(1).AddSeconds(40)),
                OperationInDb(51, FinanceOperationType.Submit, FinanceOperationState.Approved, 900, Day(2)),
                OperationInDb(52, FinanceOperationType.Submit, FinanceOperationState.Approved, 500, Day(3)),
            ]));

        // Одна операция из базы совпадает только с одним событием.
        row.Operations.Select(o => (o.Money, o.Status)).ShouldBe([
            (1500, FinanceRestoreStatus.AlreadyExists),
            (1500, FinanceRestoreStatus.Restore),
            (1000, FinanceRestoreStatus.Restore),
            (500, FinanceRestoreStatus.Restore),
        ]);
    }

    [Fact]
    public void RefundWithoutIdDoesNotMatchSubmit()
    {
        var row = SingleRow(Plan(Events(ClaimChange(Day(1), CommentExtraAction.RefundFee, Master, [Player], money: -1500, reference: Claim)),
            operations: [OperationInDb(50, FinanceOperationType.Submit, FinanceOperationState.Approved, -1500, Day(1))]));

        row.Operations.ShouldHaveSingleItem().Status.ShouldBe(FinanceRestoreStatus.Restore);
    }

    [Fact]
    public void ModerationApprovesPlayerPayment()
    {
        var row = SingleRow(Plan(PlayerPaid(Day(1), reference: CommentRef(555)), Moderation(Day(2), reference: CommentRef(556))));

        row.Warnings.ShouldBeEmpty();
        var operation = row.Operations.ShouldHaveSingleItem();
        operation.State.ShouldBe(FinanceOperationState.Approved);
        operation.ModeratedAt.ShouldBe(Day(2));
        row.Moderations.ShouldHaveSingleItem().ShouldBe(new FinanceModerationToRestore("id:556", 556, Master.UserId, Approved: true, Day(2), "",
            FinanceModerationLink.RestoredOperation, TargetOperationKey: "id:555", TargetOperationId: null, FinanceRestoreStatus.Restore));
    }

    [Fact]
    public void RejectionDeclinesPlayerPayment()
    {
        var row = SingleRow(Plan(PlayerPaid(Day(1)), Moderation(Day(2), approved: false, reference: Claim)));

        row.Operations.ShouldHaveSingleItem().State.ShouldBe(FinanceOperationState.Declined);
        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Approved.ShouldBeFalse();
        moderation.Key.ShouldBe($"mod:{Claim.ClaimId}:{Day(2).UtcTicks}:{Master.UserId.Value}");
    }

    [Fact]
    public void ModerationsLinkInTimeOrder()
    {
        var row = SingleRow(Plan(PlayerPaid(Day(1), money: 100), Moderation(Day(2)), PlayerPaid(Day(3), money: 200), Moderation(Day(4), approved: false)));

        row.Warnings.ShouldBeEmpty();
        row.Operations.Select(o => (o.Money, o.State, o.ModeratedAt)).ShouldBe([
            (100, FinanceOperationState.Approved, Day(2)),
            (200, FinanceOperationState.Declined, Day(4)),
        ]);
    }

    [Fact]
    public void MasterPaymentIsNotModerationTarget()
    {
        var row = SingleRow(Plan(MasterPaid(Day(1)), Moderation(Day(2))));

        row.Operations.ShouldHaveSingleItem().State.ShouldBe(FinanceOperationState.Approved);
        row.Moderations.ShouldHaveSingleItem().Link.ShouldBe(FinanceModerationLink.NotFound);
        row.Warnings.ShouldBe([FinanceRestoreWarning.ModerationTargetNotFound]);
    }

    [Fact]
    public void ModerationBeforePaymentDoesNotLink()
    {
        var row = SingleRow(Plan(Moderation(Day(1)), PlayerPaid(Day(2))));

        row.Operations.ShouldHaveSingleItem().State.ShouldBe(FinanceOperationState.Proposed);
        row.Moderations.ShouldHaveSingleItem().Link.ShouldBe(FinanceModerationLink.NotFound);
    }

    [Fact]
    public void TwoProposedAndOneModerationIsAmbiguous()
    {
        var row = SingleRow(Plan(PlayerPaid(Day(1), money: 100), PlayerPaid(Day(2), money: 200), Moderation(Day(3))));

        row.Operations.Select(o => o.State).ShouldBe([FinanceOperationState.Proposed, FinanceOperationState.Proposed]);
        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Link.ShouldBe(FinanceModerationLink.Ambiguous);
        moderation.Status.ShouldBe(FinanceRestoreStatus.Restore);
        row.Warnings.ShouldBe([FinanceRestoreWarning.ModerationAmbiguous]);
    }

    [Fact]
    public void ModerationOfExistingOperationRefersToItsId()
    {
        var row = SingleRow(Plan(Events(Moderation(Day(2))),
            operations: [
                OperationInDb(50, FinanceOperationType.Submit, FinanceOperationState.Proposed, 1500, BackupAt.AddDays(-3)),
                OperationInDb(51, FinanceOperationType.Submit, FinanceOperationState.Approved, 1500, BackupAt.AddDays(-2)),
            ]));

        row.Warnings.ShouldBeEmpty();
        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Link.ShouldBe(FinanceModerationLink.ExistingOperation);
        moderation.TargetOperationId.ShouldBe(50);
        moderation.TargetOperationKey.ShouldBeNull();
    }

    [Fact]
    public void ExistingProposedAndRestoredProposedAreAmbiguous()
    {
        var row = SingleRow(Plan(Events(PlayerPaid(Day(1)), Moderation(Day(2))),
            operations: [OperationInDb(50, FinanceOperationType.Submit, FinanceOperationState.Proposed, 1500, BackupAt.AddDays(-3))]));

        row.Moderations.ShouldHaveSingleItem().Link.ShouldBe(FinanceModerationLink.Ambiguous);
        row.Operations.ShouldHaveSingleItem().State.ShouldBe(FinanceOperationState.Proposed);
    }

    [Fact]
    public void ModerationOfExistingPreferentialRequestIsWarned()
    {
        var row = SingleRow(Plan(Events(Moderation(Day(2))),
            operations: [OperationInDb(50, FinanceOperationType.PreferentialFeeRequest, FinanceOperationState.Proposed, 0, BackupAt.AddDays(-3))]));

        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Link.ShouldBe(FinanceModerationLink.Preferential);
        moderation.TargetOperationId.ShouldBe(50);
        moderation.Status.ShouldBe(FinanceRestoreStatus.Restore);
        row.Warnings.ShouldBe([FinanceRestoreWarning.PreferentialModeration]);
    }

    [Fact]
    public void ModerationOfPreferentialRequestFromNotificationIsWarned()
    {
        var row = SingleRow(Plan(ClaimChange(Day(1), CommentExtraAction.RequestPreferential, Player, [Master]), Moderation(Day(2))));

        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Link.ShouldBe(FinanceModerationLink.Preferential);
        moderation.TargetOperationId.ShouldBeNull();
        moderation.TargetOperationKey.ShouldBeNull();
        row.PreferentialRequests.ShouldBe(1);
        row.Warnings.ShouldBe([FinanceRestoreWarning.PreferentialModeration]);
    }

    [Fact]
    public void ExistingModerationTakesItsOperation()
    {
        // Первая модерация уже в базе, но связывается: иначе вторая увела бы первую операцию.
        var row = SingleRow(Plan(Events(
                PlayerPaid(Day(1), money: 100),
                Moderation(Day(2), reference: CommentRef(600)),
                PlayerPaid(Day(3), money: 200),
                Moderation(Day(4), approved: false, reference: CommentRef(601))),
            comments: [CommentInDb(600, Master, Day(2), CommentExtraAction.ApproveFinance)]));

        row.Moderations.Select(m => (m.Status, m.Link)).ShouldBe([
            (FinanceRestoreStatus.AlreadyExists, FinanceModerationLink.RestoredOperation),
            (FinanceRestoreStatus.Restore, FinanceModerationLink.RestoredOperation),
        ]);
        row.Operations.Select(o => o.State).ShouldBe([FinanceOperationState.Approved, FinanceOperationState.Declined]);
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ModerationWithoutIdMatchesDbByAuthorActionAndTime()
    {
        var row = SingleRow(Plan(Events(
                Moderation(Day(1), reference: Claim),
                Moderation(Day(2), reference: Claim),
                Moderation(Day(3), approved: false, reference: Claim)),
            comments: [
                CommentInDb(50, Master, Day(1).AddSeconds(50), CommentExtraAction.ApproveFinance),
                CommentInDb(51, OtherMaster, Day(2), CommentExtraAction.ApproveFinance),
                CommentInDb(52, Master, Day(3), CommentExtraAction.ApproveFinance),
            ]));

        row.Moderations.Select(m => m.Status).ShouldBe([
            FinanceRestoreStatus.AlreadyExists,
            FinanceRestoreStatus.Restore,
            FinanceRestoreStatus.Restore,
        ]);
    }

    [Fact]
    public void ModerationIdConflict()
    {
        var row = SingleRow(Plan(Events(Moderation(Day(1), reference: CommentRef(556))), taken: [556]));

        row.Moderations.ShouldHaveSingleItem().Status.ShouldBe(FinanceRestoreStatus.Conflict);
        row.Conflicts.ShouldBe([556]);
        row.Warnings.ShouldContain(FinanceRestoreWarning.IdConflict);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SecondPlanAfterRestoreFindsEverything(bool withIds)
    {
        IProjectEntityId? Ref(int id) => withIds ? CommentRef(id) : Claim;
        var events = Events(
            PlayerPaid(Day(1), money: 100, reference: Ref(700)),
            Moderation(Day(2), reference: Ref(701)),
            MasterPaid(Day(3), money: 300, reference: Ref(702)),
            ClaimChange(Day(4), CommentExtraAction.RefundFee, Master, [Player], money: -50, reference: Ref(703)));
        var first = SingleRow(Plan(events));
        first.Warnings.ShouldBeEmpty();

        // Как будто план применён: операции и комментарии вставлены, с прежними id или новыми (id операции = id комментария).
        var newId = 1000;
        var operationIds = first.Operations.Select(o => o.OperationId ?? newId++).ToList();
        var operations = first.Operations
            .Select((o, i) => OperationInDb(operationIds[i], o.Type, o.State, o.Money!.Value, o.CreatedAt))
            .ToList();
        var comments = first.Operations.Select((o, i) => CommentInDb(operationIds[i], User(o.Initiator.Value, ""), o.CreatedAt, o.ExtraAction))
            .Concat(first.Moderations.Select(m => CommentInDb(m.CommentId ?? newId++, User(m.Initiator.Value, ""), m.CreatedAt,
                m.Approved ? CommentExtraAction.ApproveFinance : CommentExtraAction.RejectFinance)))
            .ToList();

        var second = SingleRow(Plan(events, operations: operations, comments: comments));

        second.Operations.ShouldAllBe(o => o.Status == FinanceRestoreStatus.AlreadyExists);
        second.Moderations.ShouldAllBe(m => m.Status == FinanceRestoreStatus.AlreadyExists);
        second.Operations.Select(o => (o.Key, o.State, o.ModeratedAt)).ShouldBe(first.Operations.Select(o => (o.Key, o.State, o.ModeratedAt)));
        second.Moderations.Select(m => (m.Key, m.TargetOperationKey)).ShouldBe(first.Moderations.Select(m => (m.Key, m.TargetOperationKey)));
        // Связь та же, только операция теперь в базе: вместо «восстанавливаемой» — «из базы» с её id.
        first.Moderations.ShouldHaveSingleItem().Link.ShouldBe(FinanceModerationLink.RestoredOperation);
        var moderation = second.Moderations.ShouldHaveSingleItem();
        moderation.Link.ShouldBe(FinanceModerationLink.ExistingOperation);
        moderation.TargetOperationId.ShouldBe(operationIds[0]);
        second.AlreadyExisting.ShouldBe(4);
        second.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void AppliedModerationKeepsItsOperationOnSecondPlan()
    {
        // Первый прогон применил B (Proposed → Approved) и её модерацию M, а более раннюю A — нет (тип оплаты не определён).
        var events = Events(
            PlayerPaid(Day(1), money: 100, reference: CommentRef(700)),
            PlayerPaid(Day(2), money: 200, reference: CommentRef(702)),
            Moderation(Day(3), reference: CommentRef(703)));

        var row = SingleRow(Plan(events,
            operations: [OperationInDb(702, FinanceOperationType.Submit, FinanceOperationState.Approved, 200, Day(2))],
            comments: [CommentInDb(702, Player, Day(2), CommentExtraAction.PaidFee), CommentInDb(703, Master, Day(3), CommentExtraAction.ApproveFinance)]));

        row.Operations.Select(o => (o.Status, o.State)).ShouldBe([
            (FinanceRestoreStatus.Restore, FinanceOperationState.Proposed),
            (FinanceRestoreStatus.AlreadyExists, FinanceOperationState.Proposed),
        ]);
        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Status.ShouldBe(FinanceRestoreStatus.AlreadyExists);
        moderation.Link.ShouldNotBe(FinanceModerationLink.RestoredOperation);
    }

    [Fact]
    public void AppliedModerationWithoutIdsKeepsItsOperationOnSecondPlan()
    {
        // То же без id: B совпала с базой по сумме и времени, её строка базы не становится отдельной целью.
        var events = Events(
            PlayerPaid(Day(1), money: 100, reference: Claim),
            Moderation(Day(2), reference: Claim),
            PlayerPaid(Day(3), money: 200, reference: Claim),
            Moderation(Day(4), reference: Claim));

        var row = SingleRow(Plan(events,
            operations: [OperationInDb(1001, FinanceOperationType.Submit, FinanceOperationState.Approved, 100, Day(1))],
            comments: [CommentInDb(1001, Player, Day(1), CommentExtraAction.PaidFee), CommentInDb(1002, Master, Day(2), CommentExtraAction.ApproveFinance)]));

        row.Moderations.Select(m => (m.Status, m.Link, m.TargetOperationId)).ShouldBe([
            (FinanceRestoreStatus.AlreadyExists, FinanceModerationLink.ExistingOperation, (int?)1001),
            (FinanceRestoreStatus.Restore, FinanceModerationLink.RestoredOperation, null),
        ]);
        row.Operations.Select(o => o.State).ShouldBe([FinanceOperationState.Approved, FinanceOperationState.Approved]);
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ExistingProposedMatchedByEventIsSingleTarget()
    {
        // Строка базы, совпавшая с событием, — одна цель, а не две.
        var row = SingleRow(Plan(Events(PlayerPaid(Day(1), reference: Claim), Moderation(Day(2))),
            operations: [OperationInDb(50, FinanceOperationType.Submit, FinanceOperationState.Proposed, 1500, Day(1))]));

        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Link.ShouldBe(FinanceModerationLink.ExistingOperation);
        moderation.TargetOperationId.ShouldBe(50);
        row.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void ModerationOfNotRestoredOperationIsWarned()
    {
        var row = SingleRow(Plan(Events(
                PlayerPaid(Day(1), money: 100, reference: CommentRef(700)),
                Moderation(Day(2), reference: Claim),
                PlayerPaid(Day(3), money: 200, reference: Claim),
                Moderation(Day(4), reference: Claim)),
            taken: [700]));

        // Конфликтная операция занимает модерацию, поэтому вторая модерация не уходит к ней.
        row.Moderations.Select(m => (m.Link, m.TargetOperationKey)).ShouldBe([
            (FinanceModerationLink.NotRestoredOperation, "id:700"),
            (FinanceModerationLink.RestoredOperation, row.Operations[1].Key),
        ]);
        row.Operations.Select(o => o.State).ShouldBe([FinanceOperationState.Proposed, FinanceOperationState.Approved]);
        row.Warnings.ShouldBe([FinanceRestoreWarning.IdConflict, FinanceRestoreWarning.ModerationOfNotRestoredOperation]);
    }

    [Fact]
    public void ModerationOfInvalidMoneyOperationDoesNotChangeState()
    {
        var row = SingleRow(Plan(PlayerPaid(Day(1), money: -100), Moderation(Day(2))));

        row.Operations.ShouldHaveSingleItem().State.ShouldBe(FinanceOperationState.Proposed);
        row.Moderations.ShouldHaveSingleItem().Link.ShouldBe(FinanceModerationLink.NotRestoredOperation);
        row.Warnings.ShouldBe([FinanceRestoreWarning.InvalidMoney, FinanceRestoreWarning.ModerationOfNotRestoredOperation]);
    }

    [Fact]
    public void ConflictingModerationDoesNotChangeState()
    {
        var row = SingleRow(Plan(Events(PlayerPaid(Day(1), reference: Claim), Moderation(Day(2), reference: CommentRef(800))), taken: [800]));

        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Status.ShouldBe(FinanceRestoreStatus.Conflict);
        var operation = row.Operations.ShouldHaveSingleItem();
        operation.State.ShouldBe(FinanceOperationState.Proposed);
        operation.ModeratedAt.ShouldBeNull();
    }

    [Fact]
    public void PreferentialEventMatchingDbRequestIsSingleTarget()
    {
        var row = SingleRow(Plan(Events(ClaimChange(Day(1), CommentExtraAction.RequestPreferential, Player, [Master]), Moderation(Day(2))),
            operations: [OperationInDb(50, FinanceOperationType.PreferentialFeeRequest, FinanceOperationState.Proposed, 0, Day(1).AddSeconds(20))]));

        var moderation = row.Moderations.ShouldHaveSingleItem();
        moderation.Link.ShouldBe(FinanceModerationLink.Preferential);
        moderation.TargetOperationId.ShouldBe(50);
    }

    [Fact]
    public void DbOperationExplainedByEventBeforeBackupIsNotMatchedAgain()
    {
        // Событие за 30 с до бэкапа уже в базе (строка 50); такое же событие через 20 с после бэкапа — новая операция.
        var row = SingleRow(Plan(Events(
                MasterPaid(BackupAt.AddSeconds(-30), reference: Claim),
                MasterPaid(BackupAt.AddSeconds(20), reference: Claim)),
            operations: [OperationInDb(50, FinanceOperationType.Submit, FinanceOperationState.Approved, 1500, BackupAt.AddSeconds(-30))]));

        row.Operations.ShouldHaveSingleItem().Status.ShouldBe(FinanceRestoreStatus.Restore);
    }

    [Fact]
    public void DbOperationWithIdBeforeBackupIsNotMatchedAgain()
    {
        var row = SingleRow(Plan(Events(
                MasterPaid(BackupAt.AddSeconds(-30), reference: CommentRef(50)),
                MasterPaid(BackupAt.AddSeconds(20), reference: Claim)),
            operations: [OperationInDb(50, FinanceOperationType.Submit, FinanceOperationState.Approved, 1500, BackupAt.AddSeconds(-30))]));

        row.Operations.ShouldHaveSingleItem().Status.ShouldBe(FinanceRestoreStatus.Restore);
    }

    [Fact]
    public void DbModerationExplainedByEventBeforeBackupIsNotMatchedAgain()
    {
        var row = SingleRow(Plan(Events(
                Moderation(BackupAt.AddSeconds(-30), reference: Claim),
                Moderation(BackupAt.AddSeconds(20), reference: Claim)),
            comments: [CommentInDb(50, Master, BackupAt.AddSeconds(-30), CommentExtraAction.ApproveFinance)]));

        row.Moderations.ShouldHaveSingleItem().Status.ShouldBe(FinanceRestoreStatus.Restore);
    }

    [Fact]
    public void OnlyEventsBeforeBackupGiveNoRow()
        => Plan(MasterPaid(BackupAt.AddSeconds(-30))).ShouldBeEmpty();

    [Fact]
    public void ModerationWithoutIdMatchesDbOnlyOnce()
    {
        var row = SingleRow(Plan(Events(
                Moderation(Day(1), reference: Claim),
                ClaimChange(Day(1).AddSeconds(10), CommentExtraAction.ApproveFinance, Master, [Player], text: "ещё раз", reference: Claim)),
            comments: [CommentInDb(50, Master, Day(1), CommentExtraAction.ApproveFinance)]));

        row.Moderations.Select(m => m.Status).ShouldBe([FinanceRestoreStatus.AlreadyExists, FinanceRestoreStatus.Restore]);
    }

    [Fact]
    public void PlayerOwnTypeIsNotSuggested()
    {
        // Заявленная оплата бывает только на чужой тип: тип игрока пропускается, остаётся тип получателя.
        var operation = SingleOperation(Plan(Events(PlayerPaid(Day(1), recipients: [Master, OtherMaster])),
            paymentTypes: [Cash(1, Player), Cash(2, Master)]));

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(2, null, PaymentTypeGuess.Recipient, []));
    }

    [Fact]
    public void PlayerOwnTypeIsNotSingleInProject()
    {
        var row = SingleRow(Plan(Events(PlayerPaid(Day(1), recipients: [OtherMaster])),
            paymentTypes: [Cash(1, Player), Custom(2, Cashier)]));

        row.Operations.ShouldHaveSingleItem().PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(2, null, PaymentTypeGuess.SingleInProject, []));
    }

    [Fact]
    public void OnlyPlayerOwnTypeIsNotDetermined()
    {
        var row = SingleRow(Plan(Events(PlayerPaid(Day(1))), paymentTypes: [Cash(1, Player)]));

        row.Operations.ShouldHaveSingleItem().PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(null, null, PaymentTypeGuess.NotFound, []));
    }

    [Fact]
    public void PaymentTypeOfInitiator()
    {
        var operation = SingleOperation(Plan(Events(MasterPaid(Day(1), recipients: [Player, Cashier])),
            paymentTypes: [Cash(1, Cashier), Custom(2, Master), Online(3, Master)]));

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(2, null, PaymentTypeGuess.Initiator, []));
    }

    [Fact]
    public void PaymentTypeOfRecipientWhenPlayerReports()
    {
        var operation = SingleOperation(Plan(Events(PlayerPaid(Day(1), recipients: [Master, Cashier])),
            paymentTypes: [Cash(1, Cashier), Online(3, Master)]));

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(1, null, PaymentTypeGuess.Recipient, []));
    }

    [Fact]
    public void SinglePaymentTypeInProject()
    {
        // Владелец типа уведомления не получил (например, тип чужого мастера без доступа) — единственный тип проекта.
        var operation = SingleOperation(Plan(Events(MasterPaid(Day(1), recipients: [Player])),
            paymentTypes: [Custom(1, Cashier), Online(3, Cashier)]));

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(1, null, PaymentTypeGuess.SingleInProject, []));
    }

    [Fact]
    public void SingleActiveTypeWins()
    {
        var operation = SingleOperation(Plan(Events(MasterPaid(Day(1))),
            paymentTypes: [Cash(1, Master, active: false), Custom(2, Master)]));

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(2, null, PaymentTypeGuess.Initiator, []));
    }

    [Fact]
    public void SingleCashAmongActiveWins()
    {
        var operation = SingleOperation(Plan(Events(MasterPaid(Day(1))),
            paymentTypes: [Custom(1, Master), Cash(2, Master), Cash(3, Master, active: false)]));

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(2, null, PaymentTypeGuess.Initiator, []));
    }

    [Fact]
    public void AmbiguousStepDoesNotFallThrough()
    {
        // У инициатора два активных пользовательских типа — неоднозначно, хотя в получателях есть владелец «наличных».
        var row = SingleRow(Plan(Events(MasterPaid(Day(1), recipients: [Player, Cashier])),
            paymentTypes: [Custom(2, Master), Custom(1, Master), Cash(3, Cashier)]));

        row.Operations.ShouldHaveSingleItem().PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(null, null, PaymentTypeGuess.Ambiguous, [1, 2]));
        row.Warnings.ShouldBe([FinanceRestoreWarning.PaymentTypeNotDetermined]);
    }

    [Fact]
    public void SeveralRecipientOwnersAreAmbiguous()
    {
        var operation = SingleRow(Plan(Events(PlayerPaid(Day(1), recipients: [Master, Cashier])),
            paymentTypes: [Cash(1, Master), Cash(2, Cashier)])).Operations.ShouldHaveSingleItem();

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(null, null, PaymentTypeGuess.Ambiguous, [1, 2]));
    }

    [Fact]
    public void SeveralTypesInProjectAreAmbiguous()
    {
        var operation = SingleRow(Plan(Events(MasterPaid(Day(1), recipients: [Player])),
            paymentTypes: [Cash(1, Cashier), Cash(2, OtherMaster)])).Operations.ShouldHaveSingleItem();

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(null, null, PaymentTypeGuess.Ambiguous, [1, 2]));
    }

    [Fact]
    public void NoManualTypesMasterPaymentSuggestsCash()
    {
        var operation = SingleOperation(Plan(Events(MasterPaid(Day(1))), paymentTypes: [Online(3, Master)]));

        operation.PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(null, Master.UserId, PaymentTypeGuess.CreateCash, []));
    }

    [Fact]
    public void NoManualTypesPlayerPaymentIsNotDetermined()
    {
        var row = SingleRow(Plan(Events(PlayerPaid(Day(1))), paymentTypes: []));

        row.Operations.ShouldHaveSingleItem().PaymentType.ShouldBeSuggestion(new PaymentTypeSuggestion(null, null, PaymentTypeGuess.NotFound, []));
        row.Warnings.ShouldBe([FinanceRestoreWarning.PaymentTypeNotDetermined]);
    }
}

internal static class PaymentTypeSuggestionAssertions
{
    /// <summary>Сравнение с кандидатами по содержимому: record сравнивает коллекции по ссылке.</summary>
    public static void ShouldBeSuggestion(this PaymentTypeSuggestion actual, PaymentTypeSuggestion expected)
    {
        (actual.PaymentTypeId, actual.CreateCashFor, actual.How).ShouldBe((expected.PaymentTypeId, expected.CreateCashFor, expected.How));
        actual.Candidates.ShouldBe(expected.Candidates);
    }
}
