using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Services.Interfaces.Drp;
using static JoinRpg.Tools.RestoreManualFinance.Test.PlanFactory;

namespace JoinRpg.Tools.RestoreManualFinance.Test;

public class ApplyBuilderTest
{
    private static readonly Dictionary<UserIdentification, PaymentTypeIdentification> NoCash = [];

    private static ClaimRestoreInput Build(FinanceRestorePlanRow row, IEnumerable<ManualFinanceCsvRow> csv, IReadOnlyDictionary<UserIdentification, PaymentTypeIdentification>? cash = null)
        => ApplyBuilder.BuildClaim(row, ApplyBuilder.Match(Plan(row), csv), cash ?? NoCash);

    [Fact]
    public void OperationWithIdAndItsModerationArePassedWithParentId()
    {
        var row = Row(
            [Operation("id:500", id: 500, byPlayer: true, state: FinanceOperationState.Approved, moderatedAt: At.AddDays(1))],
            [Moderation("id:501", FinanceModerationLink.RestoredOperation, id: 501, targetKey: "id:500")]);

        var input = Build(row, [Csv("id:500", "7"), Csv("id:501")]);

        var operation = input.Operations.ShouldHaveSingleItem();
        operation.OperationId.ShouldBe(500);
        operation.Author.ShouldBe(Player);
        operation.IsCommentByPlayer.ShouldBeTrue();
        operation.PaymentType.ShouldBe(new PaymentTypeIdentification(Project, 7));
        // Состояние и время решения — из плана, без искажений.
        operation.State.ShouldBe(FinanceOperationState.Approved);
        operation.CreatedAt.ShouldBe(At);
        operation.ChangedAt.ShouldBe(At.AddDays(1));
        operation.Money.ShouldBe(1500);
        input.OperationKeys.ShouldBe(["id:500"]);

        var moderation = input.Moderations.ShouldHaveSingleItem();
        moderation.CommentId.ShouldBe(501);
        moderation.Approved.ShouldBeTrue();
        moderation.ParentOperationId.ShouldBe(500);
        moderation.ParentRestoredIndex.ShouldBeNull();
        input.NotPassed.ShouldBeEmpty();
    }

    [Fact]
    public void OperationWithoutIdIsParentByIndexInPassedList()
    {
        // Первая операция не передаётся (тип не задан) — индекс второй в передаваемом списке 0, а не 1.
        var row = Row(
            [
                Operation("op:a", suggestion: NotFound()),
                Operation("op:b", byPlayer: true, state: FinanceOperationState.Declined, moderatedAt: At.AddDays(1)),
            ],
            [Moderation("mod:c", FinanceModerationLink.RestoredOperation, approved: false, targetKey: "op:b")]);

        var input = Build(row, [Csv("op:a"), Csv("op:b", "7"), Csv("mod:c")]);

        input.OperationKeys.ShouldBe(["op:b"]);
        input.Operations[0].OperationId.ShouldBeNull();
        input.Operations[0].State.ShouldBe(FinanceOperationState.Declined);
        var moderation = input.Moderations.ShouldHaveSingleItem();
        moderation.Approved.ShouldBeFalse();
        moderation.ParentOperationId.ShouldBeNull();
        moderation.ParentRestoredIndex.ShouldBe(0);
        input.NotPassed["op:a"].Text.ShouldBe("пропущено: не задан тип оплаты");
        input.NotPassed["op:a"].IsError.ShouldBeFalse();
    }

    [Fact]
    public void ModerationOfSkippedOperationWaitsForNextRun()
    {
        var row = Row(
            [Operation("op:a", byPlayer: true, state: FinanceOperationState.Approved, moderatedAt: At.AddDays(1))],
            [Moderation("mod:b", FinanceModerationLink.RestoredOperation, targetKey: "op:a")]);

        var input = Build(row, [Csv("op:a"), Csv("mod:b")]);

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["mod:b"].Text.ShouldContain("вставится вместе с ней");
    }

    [Fact]
    public void ModerationOfExistingOperationOrPreferentialGoesToIt()
    {
        var row = Row(
            moderations:
            [
                Moderation("id:600", FinanceModerationLink.ExistingOperation, id: 600, targetId: 450),
                Moderation("id:601", FinanceModerationLink.Preferential, id: 601, at: At.AddDays(2), targetId: 460),
            ]);

        var input = Build(row, [Csv("id:600"), Csv("id:601")]);

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
        // Решение без операции: вставится комментарий без родителя, состояние ничьё не изменится.
        var row = Row(moderations: [Moderation("mod:a", link)]);

        var input = Build(row, [Csv("mod:a")]);

        var moderation = input.Moderations.ShouldHaveSingleItem();
        moderation.ParentOperationId.ShouldBeNull();
        moderation.ParentRestoredIndex.ShouldBeNull();
        input.ModerationKeys.ShouldBe(["mod:a"]);
        input.NotPassed.ShouldBeEmpty();
    }

    [Fact]
    public void ModerationOfNotRestoredOperationIsPostponed()
    {
        var row = Row(moderations: [Moderation("mod:a", FinanceModerationLink.NotRestoredOperation, targetKey: "id:9")]);

        var input = Build(row, [Csv("mod:a")]);

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["mod:a"].Text.ShouldStartWith("пропущено: операция id:9");
    }

    [Fact]
    public void NotRestoredRowsAreNotPassed()
    {
        var row = Row(
            [
                Operation("id:1", id: 1, status: FinanceRestoreStatus.AlreadyExists),
                Operation("id:2", id: 2, status: FinanceRestoreStatus.Conflict),
                Operation("op:3", money: null, status: FinanceRestoreStatus.InvalidMoney),
            ],
            [Moderation("id:4", FinanceModerationLink.ExistingOperation, id: 4, targetId: 1, status: FinanceRestoreStatus.AlreadyExists)]);

        var input = Build(row, [Csv("id:1", "7"), Csv("id:2", "7"), Csv("op:3", "7"), Csv("id:4")]);

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["id:1"].Text.ShouldBe("уже есть");
        input.NotPassed["id:2"].Text.ShouldBe("пропущено: id занят в базе другим");
        input.NotPassed["op:3"].Text.ShouldBe("пропущено: некорректная сумма");
        input.NotPassed["id:4"].Text.ShouldBe("уже есть");
    }

    [Fact]
    public void RowsMissingFromCsvAreSkipped()
    {
        var row = Row(
            [Operation("op:a")],
            [Moderation("id:5", FinanceModerationLink.ExistingOperation, id: 5, targetId: 1)]);

        var input = Build(row, []);

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["op:a"].Text.ShouldBe("пропущено: " + ApplyBuilder.NotInCsv);
        input.NotPassed["id:5"].Text.ShouldBe("пропущено: " + ApplyBuilder.NotInCsv);
    }

    [Fact]
    public void CashChoiceUsesCreatedType()
    {
        var row = Row([Operation("op:a", suggestion: CreateCash(Master))]);
        var created = new PaymentTypeIdentification(Project, 77);

        var input = Build(row, [Csv("op:a", "cash:10")], new Dictionary<UserIdentification, PaymentTypeIdentification> { [Master] = created });

        input.Operations.ShouldHaveSingleItem().PaymentType.ShouldBe(created);
    }

    [Fact]
    public void CashChoiceWithoutCreatedTypeIsError()
    {
        var row = Row([Operation("op:a", suggestion: CreateCash(Master))]);

        var input = Build(row, [Csv("op:a", "cash:10")]);

        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["op:a"].IsError.ShouldBeTrue();
    }

    [Fact]
    public void ProposedRefundIsSkippedAndCreatesNoCash()
    {
        var row = Row([Operation("op:a", byPlayer: true, type: FinanceOperationType.Refund, money: -100, suggestion: CreateCash(Master))]);
        var decisions = ApplyBuilder.Match(Plan(row), [Csv("op:a", "cash:10")]);

        // Отброшенная строка не должна создать тип «наличные».
        decisions.CashOwners.ShouldBeEmpty();
        var input = ApplyBuilder.BuildClaim(row, decisions, NoCash);
        input.IsEmpty.ShouldBeTrue();
        input.NotPassed["op:a"].Text.ShouldContain("возврат не подтверждён");
        input.NotPassed["op:a"].IsError.ShouldBeFalse();
    }

    [Fact]
    public void DuplicateKeyInPlanIsNotAppliedButOthersAre()
    {
        // Два события без id с одинаковым ключом: строку CSV не сопоставить ни с одним.
        var plan = Plan(Row(
            [Operation("op:dup", suggestion: CreateCash(Master)), Operation("op:dup", suggestion: CreateCash(Master)), Operation("op:ok", at: At.AddHours(1))],
            [Moderation("mod:x", FinanceModerationLink.RestoredOperation, targetKey: "op:dup")]));

        // Dry-run CSV с повтором ключа читается целиком.
        var csv = ManualFinanceCsv.Parse(Drp.Common.DrpCsv.Format(ReportWriter.Rows([plan], new ApplyResults())));
        csv.Count(r => r.Key == "op:dup").ShouldBe(2);
        var edited = csv.Select(r => r.Key == "op:dup" ? r with { PaymentType = PaymentTypeChoice.Cash(Master) } : r).ToList();

        var decisions = ApplyBuilder.Match(plan, edited);
        decisions.CashOwners.ShouldBeEmpty();
        var input = ApplyBuilder.BuildClaim(plan.Rows[0], decisions, NoCash);

        input.OperationKeys.ShouldBe(["op:ok"]);
        input.NotPassed["op:dup"].Text.ShouldBe("пропущено: " + ApplyBuilder.AmbiguousKey);
        input.NotPassed["mod:x"].Text.ShouldContain("вставится вместе с ней");
    }

    [Fact]
    public void DuplicateKeyInCsvIsNotApplied()
    {
        var plan = Plan(Row([Operation("op:a")]));

        var decisions = ApplyBuilder.Match(plan, [Csv("op:a", "7"), Csv("op:a", "7", line: 3)]);

        decisions.Choices.ShouldBeEmpty();
        decisions.NotApplicable["op:a"].Text.ShouldBe("пропущено: " + ApplyBuilder.AmbiguousKey);
    }

    [Fact]
    public void MatchValidatesPaymentTypeAgainstProject()
    {
        var plan = Plan(Row([Operation("op:a"), Operation("op:b", at: At.AddHours(1)), Operation("op:c", at: At.AddHours(2)), Operation("op:d", at: At.AddHours(3))]));

        var decisions = ApplyBuilder.Match(plan, [Csv("op:a", "7"), Csv("op:b", "999"), Csv("op:c", "8"), Csv("op:d", "cash:10"), Csv("op:zzz", "7", line: 9)]);

        decisions.Choices.Keys.ShouldBe(["op:a", "op:d"], ignoreOrder: true);
        decisions.NotApplicable["op:b"].Text.ShouldBe("ошибка: типа оплаты 999 нет в проекте");
        decisions.NotApplicable["op:c"].Text.ShouldContain("онлайновый");
        decisions.NotApplicable.Values.ShouldAllBe(e => e.IsError);
        decisions.CashOwners.ShouldBe([Master]);
        decisions.Warnings.ShouldHaveSingleItem().ShouldContain("Строка 9");
        decisions.KeysInCsv.ShouldNotContain("op:zzz");
        // Тип в строке, которой нет в плане, потерялся — это ошибка прогона.
        decisions.UnappliedChoices.ShouldBe([9]);
    }

    [Fact]
    public void MatchIgnoresRowsOfOtherProjects()
    {
        var plan = Plan(Row([Operation("op:a")]));

        var decisions = ApplyBuilder.Match(plan, [Csv("op:a", "7", project: new(1601))]);

        decisions.KeysInCsv.ShouldBeEmpty();
        decisions.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void OutcomesFollowDomainResultOrder()
    {
        var row = Row(
            [Operation("op:a"), Operation("id:9", id: 9, at: At.AddHours(1))],
            [Moderation("id:10", FinanceModerationLink.ExistingOperation, id: 10, targetId: 3)]);
        var input = Build(row, [Csv("op:a", "7"), Csv("id:9", "7"), Csv("id:10")]);

        var outcomes = ApplyBuilder.Outcomes(input, new RestoredFinanceResult(
            [new(RestoredFinanceEntryStatus.Inserted, 1234), new(RestoredFinanceEntryStatus.TakenElsewhere, 9)],
            [new(RestoredFinanceEntryStatus.AlreadyExisted, 10)]));

        outcomes["op:a"].Text.ShouldBe("вставлено (id 1234)");
        outcomes["id:9"].Text.ShouldBe("пропущено: id 9 занят другим");
        outcomes["id:10"].Text.ShouldBe("уже было (id 10)");
    }
}
