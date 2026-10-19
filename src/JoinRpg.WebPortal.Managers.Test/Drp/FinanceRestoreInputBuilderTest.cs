using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.WebPortal.Managers.Drp;

namespace JoinRpg.WebPortal.Managers.Test.Drp;

public class FinanceRestoreInputBuilderTest
{
    private static readonly ProjectIdentification Project = new(1600);
    private static readonly ClaimIdentification Claim = new(Project, 100);
    private static readonly UserIdentification Master = new(10);
    private static readonly UserIdentification Player = new(20);
    private static readonly DateTimeOffset At = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly PaymentTypeIdentification Cash = new(Project, 7);

    private static FinanceOperationToRestore Operation(
        string key,
        int? id = null,
        bool byPlayer = false,
        FinanceOperationType type = FinanceOperationType.Submit,
        int? money = 1500,
        FinanceOperationState? state = null,
        DateTimeOffset? moderatedAt = null,
        FinanceRestoreStatus status = FinanceRestoreStatus.Restore)
        => new(key, id, byPlayer ? Player : Master, byPlayer, type, money, At, "Текст операции",
            type == FinanceOperationType.Submit ? CommentExtraAction.PaidFee : CommentExtraAction.RefundFee,
            state ?? (byPlayer ? FinanceOperationState.Proposed : FinanceOperationState.Approved),
            moderatedAt,
            new PaymentTypeSuggestion(7, CreateCashFor: null, PaymentTypeGuess.Initiator, Candidates: []),
            status);

    private static FinanceModerationToRestore Moderation(
        string key,
        FinanceModerationLink link,
        int? id = null,
        bool approved = true,
        string? targetKey = null,
        int? targetId = null,
        FinanceRestoreStatus status = FinanceRestoreStatus.Restore)
        => new(key, id, Master, approved, At.AddDays(1), "Текст решения", link, targetKey, targetId, status);

    private static FinanceRestorePlanRow Row(
        IReadOnlyList<FinanceOperationToRestore>? operations = null,
        IReadOnlyList<FinanceModerationToRestore>? moderations = null)
        => new(Claim, ClaimExists: true, operations ?? [], moderations ?? [], SkippedForMissingClaim: 0, Transfers: 0,
            PreferentialRequests: 0, OnlinePayments: 0, Unparsed: 0, Warnings: []);

    private static ClaimFinanceRestoreInput Build(FinanceRestorePlanRow row, params string[] withoutType)
        => FinanceRestoreInputBuilder.BuildClaim(row, o => withoutType.Contains(o.Key) ? null : Cash);

    [Fact]
    public void OperationWithIdAndItsModerationArePassedWithParentId()
    {
        var row = Row(
            [Operation("id:500", id: 500, byPlayer: true, state: FinanceOperationState.Approved, moderatedAt: At.AddDays(1))],
            [Moderation("id:501", FinanceModerationLink.RestoredOperation, id: 501, targetKey: "id:500")]);

        var input = Build(row);

        input.ClaimId.ShouldBe(Claim);
        var operation = input.Operations.ShouldHaveSingleItem();
        operation.OperationId.ShouldBe(500);
        operation.Author.ShouldBe(Player);
        operation.IsCommentByPlayer.ShouldBeTrue();
        operation.PaymentType.ShouldBe(Cash);
        operation.State.ShouldBe(FinanceOperationState.Approved);
        operation.CreatedAt.ShouldBe(At);
        operation.ChangedAt.ShouldBe(At.AddDays(1));
        operation.Money.ShouldBe(1500);
        operation.Text.ShouldBe("Текст операции");
        input.OperationKeys.ShouldBe(["id:500"]);

        var moderation = input.Moderations.ShouldHaveSingleItem();
        moderation.CommentId.ShouldBe(501);
        moderation.Approved.ShouldBeTrue();
        moderation.ParentOperationId.ShouldBe(500);
        moderation.ParentRestoredIndex.ShouldBeNull();
        input.ModerationKeys.ShouldBe(["id:501"]);
        input.NotPassed.ShouldBeEmpty();
        input.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void OperationWithoutModerationChangedAtIsCreatedAt()
        => Build(Row([Operation("op:a")])).Operations.ShouldHaveSingleItem().ChangedAt.ShouldBe(At);

    [Fact]
    public void OperationWithoutIdIsParentByIndexInPassedList()
    {
        // Первая операция не передаётся (тип не выбран) — индекс второй в передаваемом списке 0, а не 1.
        var row = Row(
            [Operation("op:a"), Operation("op:b", byPlayer: true, state: FinanceOperationState.Declined, moderatedAt: At.AddDays(1))],
            [Moderation("mod:c", FinanceModerationLink.RestoredOperation, approved: false, targetKey: "op:b")]);

        var input = Build(row, withoutType: "op:a");

        input.OperationKeys.ShouldBe(["op:b"]);
        var moderation = input.Moderations.ShouldHaveSingleItem();
        moderation.ParentOperationId.ShouldBeNull();
        moderation.ParentRestoredIndex.ShouldBe(0);
        input.NotPassed.ShouldBe(new Dictionary<string, FinanceRestoreSkipReason> { ["op:a"] = FinanceRestoreSkipReason.NoPaymentType });
    }

    [Fact]
    public void NotApplicableOperationsAreNotPassedWithReason()
    {
        var row = Row(
            [
                Operation("id:1", id: 1, status: FinanceRestoreStatus.AlreadyExists),
                Operation("id:2", id: 2, status: FinanceRestoreStatus.Conflict),
                Operation("op:3", money: null, status: FinanceRestoreStatus.InvalidMoney),
                Operation("op:4", byPlayer: true, type: FinanceOperationType.Refund, money: -100),
            ],
            [Moderation("id:5", FinanceModerationLink.ExistingOperation, id: 5, targetId: 1, status: FinanceRestoreStatus.AlreadyExists)]);

        var input = Build(row);

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed.ShouldBe(new Dictionary<string, FinanceRestoreSkipReason>
        {
            ["id:1"] = FinanceRestoreSkipReason.AlreadyExists,
            ["id:2"] = FinanceRestoreSkipReason.Conflict,
            ["op:3"] = FinanceRestoreSkipReason.InvalidMoney,
            ["op:4"] = FinanceRestoreSkipReason.RefundNotApproved,
            ["id:5"] = FinanceRestoreSkipReason.AlreadyExists,
        }, ignoreOrder: true);
    }

    [Fact]
    public void PaymentTypeIsNotAskedForNotApplicableOperation()
    {
        var asked = new List<string>();

        FinanceRestoreInputBuilder.BuildClaim(
            Row([Operation("id:1", id: 1, status: FinanceRestoreStatus.AlreadyExists), Operation("op:2")]),
            o =>
            {
                asked.Add(o.Key);
                return Cash;
            });

        asked.ShouldBe(["op:2"]);
    }

    [Fact]
    public void DuplicateKeyInRowIsNotPassedButOthersAre()
    {
        // Ключ без id совпал у двух операций: какая из них цель решения и какой ответ сервиса чей — не понять.
        var row = Row(
            [Operation("op:a"), Operation("op:a", money: 700), Operation("op:b")],
            [Moderation("mod:c", FinanceModerationLink.RestoredOperation, targetKey: "op:a")]);

        var input = Build(row);

        input.OperationKeys.ShouldBe(["op:b"]);
        input.Moderations.ShouldBeEmpty();
        input.NotPassed.ShouldBe(new Dictionary<string, FinanceRestoreSkipReason>
        {
            ["op:a"] = FinanceRestoreSkipReason.AmbiguousKey,
            ["mod:c"] = FinanceRestoreSkipReason.ParentNotRestoredInThisRun,
        }, ignoreOrder: true);
    }

    [Fact]
    public void ModerationOfNotPassedOperationWaitsForNextRun()
    {
        var row = Row(
            [Operation("op:a", byPlayer: true, state: FinanceOperationState.Approved, moderatedAt: At.AddDays(1))],
            [Moderation("mod:b", FinanceModerationLink.RestoredOperation, targetKey: "op:a")]);

        var input = Build(row, withoutType: "op:a");

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["mod:b"].ShouldBe(FinanceRestoreSkipReason.ParentNotRestoredInThisRun);
    }

    [Fact]
    public void ModerationOfExistingOperationOrPreferentialGoesToIt()
    {
        var row = Row(moderations:
        [
            Moderation("id:600", FinanceModerationLink.ExistingOperation, id: 600, targetId: 450),
            Moderation("id:601", FinanceModerationLink.Preferential, id: 601, targetId: 460),
        ]);

        var input = Build(row);

        input.Moderations.Select(m => m.ParentOperationId).ShouldBe([450, 460]);
        input.Moderations.ShouldAllBe(m => m.ParentRestoredIndex == null);
        input.ModerationKeys.ShouldBe(["id:600", "id:601"]);
    }

    [Theory]
    [InlineData(FinanceModerationLink.NotFound)]
    [InlineData(FinanceModerationLink.Ambiguous)]
    [InlineData(FinanceModerationLink.Preferential)]
    public void UnlinkedModerationIsPassedWithoutParent(FinanceModerationLink link)
    {
        var input = Build(Row(moderations: [Moderation("mod:a", link)]));

        var moderation = input.Moderations.ShouldHaveSingleItem();
        moderation.ParentOperationId.ShouldBeNull();
        moderation.ParentRestoredIndex.ShouldBeNull();
        input.NotPassed.ShouldBeEmpty();
    }

    [Fact]
    public void ModerationOfNotInsertedOperationIsPostponed()
    {
        var input = Build(Row(moderations: [Moderation("mod:a", FinanceModerationLink.NotRestoredOperation, targetKey: "id:9")]));

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["mod:a"].ShouldBe(FinanceRestoreSkipReason.ParentNotInserted);
    }

    [Fact]
    public void ModerationOfExistingOperationWithoutIdIsUnknownParent()
    {
        var input = Build(Row(moderations: [Moderation("mod:a", FinanceModerationLink.ExistingOperation)]));

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["mod:a"].ShouldBe(FinanceRestoreSkipReason.ParentUnknown);
    }

    [Fact]
    public void OutcomesFollowDomainResultOrder()
    {
        var row = Row(
            [Operation("op:a"), Operation("id:9", id: 9)],
            [Moderation("id:10", FinanceModerationLink.ExistingOperation, id: 10, targetId: 3)]);
        var input = Build(row);
        var inserted = new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, 1234);
        var taken = new RestoredFinanceEntry(RestoredFinanceEntryStatus.TakenElsewhere, 9);
        var existed = new RestoredFinanceEntry(RestoredFinanceEntryStatus.AlreadyExisted, 10);

        var outcomes = FinanceRestoreInputBuilder.Outcomes(input, new RestoredFinanceResult([inserted, taken], [existed]));

        outcomes.ShouldBe(new Dictionary<string, RestoredFinanceEntry>
        {
            ["op:a"] = inserted,
            ["id:9"] = taken,
            ["id:10"] = existed,
        }, ignoreOrder: true);
    }

    [Fact]
    public void SourceIsBuiltFromLookup()
    {
        var events = new ClaimNotificationEvent[]
        {
            new(Claim, new ClaimCommentIdentification(Claim, 500), At, Master, ClaimNotificationKind.ClaimChange, null, null,
                CommentExtraAction.PaidFee, ClaimEventAuthor.Master, null, 1500, "Принято", []),
        };
        var paymentType = new PaymentTypeForRestore(Cash, PaymentTypeKind.Cash, Master, IsActive: false, "Наличные мастеру");
        var lookup = new ClaimFinanceRestoreLookup(
            [Claim],
            [new ClaimFinanceOperationForRestore(400, Claim, FinanceOperationType.Submit, FinanceOperationState.Approved, 700, At.AddDays(-2))],
            [new ClaimFinanceCommentForRestore(401, Claim, Player, At.AddDays(-1), CommentExtraAction.ApproveFinance)],
            TakenCommentIds: [402],
            [paymentType],
            [new MoneyMasterForRestore(Master, IsOwner: true)]);
        var backupTakenAt = At.AddDays(-3);

        var source = FinanceRestoreInputBuilder.BuildSource(lookup, events, backupTakenAt);

        source.Events.ShouldBe(events);
        source.Claims.ShouldBe([Claim]);
        source.ExistingOperations.ShouldBe([new ExistingFinanceOperationInfo(400, Claim, FinanceOperationType.Submit, FinanceOperationState.Approved, 700, At.AddDays(-2))]);
        source.ExistingComments.ShouldBe([new ExistingFinanceCommentInfo(401, Claim, Player, At.AddDays(-1), CommentExtraAction.ApproveFinance)]);
        source.TakenCommentIds.ShouldBe([402]);
        source.PaymentTypes.ShouldBe([new ProjectPaymentTypeInfo(7, PaymentTypeKind.Cash, Master, IsActive: false)]);
        source.BackupTakenAt.ShouldBe(backupTakenAt);
    }
}
