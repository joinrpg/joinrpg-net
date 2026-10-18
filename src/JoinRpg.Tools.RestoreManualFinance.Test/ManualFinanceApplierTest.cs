using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Services.Interfaces.Drp;
using static JoinRpg.Tools.RestoreManualFinance.Test.PlanFactory;

namespace JoinRpg.Tools.RestoreManualFinance.Test;

public class ManualFinanceApplierTest
{
    private sealed class FakeDomain : IManualFinanceDomain
    {
        public HashSet<ClaimIdentification> Failing { get; } = [];
        public List<(UserIdentification Master, ClaimIdentification ClaimId, int Operations, int Moderations)> Calls { get; } = [];
        public List<UserIdentification> CashCreated { get; } = [];

        public Task<PaymentTypeIdentification> CreateRestoredCashPaymentType(UserIdentification actingMaster, ProjectIdentification projectId, UserIdentification owner)
        {
            CashCreated.Add(owner);
            return Task.FromResult(new PaymentTypeIdentification(projectId, 77));
        }

        public Task<RestoredFinanceResult> RestoreFinanceOperations(
            UserIdentification actingMaster,
            ClaimIdentification claimId,
            IReadOnlyList<RestoredFinanceOperation> operations,
            IReadOnlyList<RestoredFinanceModeration> moderations)
        {
            Calls.Add((actingMaster, claimId, operations.Count, moderations.Count));
            if (Failing.Contains(claimId))
            {
                throw new InvalidOperationException("Сломалось");
            }
            return Task.FromResult(new RestoredFinanceResult(
                [.. operations.Select(o => new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, o.OperationId ?? 999))],
                [.. moderations.Select(m => new RestoredFinanceEntry(RestoredFinanceEntryStatus.Inserted, m.CommentId))]));
        }
    }

    private static async Task<(ApplyResults Results, FakeDomain Domain)> Apply(ProjectFinancePlan plan, IReadOnlyList<ManualFinanceCsvRow> csv, Action<FakeDomain>? setup = null)
    {
        var domain = new FakeDomain();
        setup?.Invoke(domain);
        var results = new ApplyResults();
        await new ManualFinanceApplier(domain, TextWriter.Null, TextWriter.Null).Apply(plan, csv, results, CancellationToken.None);
        return (results, domain);
    }

    [Fact]
    public async Task ErrorOnClaimDoesNotStopOthers()
    {
        var plan = Plan(
            Row([Operation("id:1", id: 1)]),
            Row([Operation("id:2", id: 2)], claim: OtherClaim));

        var (results, domain) = await Apply(plan, [Csv("id:1", "7"), Csv("id:2", "7")], d => d.Failing.Add(Claim));

        domain.Calls.Select(c => c.ClaimId).ShouldBe([Claim, OtherClaim]);
        domain.Calls.ShouldAllBe(c => c.Master == Master);
        results.Entries[(Project, "id:1")].Text.ShouldBe("ошибка: Сломалось");
        results.Claims[Claim].IsError.ShouldBeTrue();
        results.Entries[(Project, "id:2")].Text.ShouldBe("вставлено (id 2)");
        results.Claims[OtherClaim].Text.ShouldStartWith("применено");
        results.HasErrors.ShouldBeTrue();
    }

    [Fact]
    public async Task ConfirmedCashIsCreatedOnceBeforeClaims()
    {
        var plan = Plan(
            Row([Operation("op:a", suggestion: CreateCash(Master))]),
            Row([Operation("op:b", suggestion: CreateCash(Master))], claim: OtherClaim));

        var (results, domain) = await Apply(plan, [Csv("op:a", "cash:10"), Csv("op:b", "cash:10")]);

        domain.CashCreated.ShouldBe([Master]);
        domain.Calls.Count.ShouldBe(2);
        results.HasErrors.ShouldBeFalse();
    }

    [Fact]
    public async Task NothingToApplyMakesNoCalls()
    {
        var plan = Plan(
            Row([Operation("id:1", id: 1, status: FinanceRestoreStatus.AlreadyExists)]),
            Row([Operation("op:b", suggestion: CreateCash(Master))], claim: OtherClaim),
            Row(claim: new ClaimIdentification(Project, 102), exists: false));

        var (results, domain) = await Apply(plan, [Csv("id:1", "7"), Csv("op:b")]);

        domain.Calls.ShouldBeEmpty();
        domain.CashCreated.ShouldBeEmpty();
        results.Claims[Claim].Text.ShouldBe("нечего применять");
        results.Entries[(Project, "op:b")].Text.ShouldBe("пропущено: не задан тип оплаты");
        results.Claims[new ClaimIdentification(Project, 102)].Text.ShouldBe("пропущено: заявки нет в базе");
        results.HasErrors.ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectWithoutMoneyMasterIsErrorOnlyWhereSomethingToApply()
    {
        var plan = Plan(
            Row([Operation("op:a", suggestion: CreateCash(Master))]),
            Row([Operation("id:2", id: 2, status: FinanceRestoreStatus.AlreadyExists)], claim: OtherClaim));
        plan = plan with { ActingMaster = null };

        var (results, domain) = await Apply(plan, [Csv("op:a", "cash:10"), Csv("id:2")]);

        domain.Calls.ShouldBeEmpty();
        domain.CashCreated.ShouldBeEmpty();
        results.Claims[Claim].IsError.ShouldBeTrue();
        results.Entries[(Project, "op:a")].IsError.ShouldBeTrue();
        results.Claims[OtherClaim].Text.ShouldBe("нечего применять");
        results.Entries[(Project, "id:2")].Text.ShouldBe("уже есть");
    }

    [Fact]
    public async Task ChoiceForKeyMissingFromPlanIsError()
    {
        var plan = Plan(Row([Operation("id:1", id: 1, status: FinanceRestoreStatus.AlreadyExists)]));

        var (results, _) = await Apply(plan, [Csv("id:1"), Csv("id:404", "7", line: 7), Csv("id:405", line: 8)]);

        results.UnappliedCsvLines.ShouldBe([7]);
        results.HasErrors.ShouldBeTrue();
    }

    [Fact]
    public void ActingMasterIsOwnerElseSmallestId()
    {
        FinanceSourceBuilder.ChooseActingMaster([new(new(30), false), new(new(20), false)]).ShouldBe(new UserIdentification(20));
        FinanceSourceBuilder.ChooseActingMaster([new(new(20), false), new(new(30), IsOwner: true)]).ShouldBe(new UserIdentification(30));
        FinanceSourceBuilder.ChooseActingMaster([]).ShouldBeNull();
    }

    [Fact]
    public void SourceIsBuiltFromLookup()
    {
        var events = new ClaimNotificationEvent[]
        {
            new(Claim, new ClaimCommentIdentification(Claim, 500), At, Master, ClaimNotificationKind.ClaimChange, null, null,
                DomainTypes.Characters.Claims.CommentExtraAction.PaidFee, ClaimEventAuthor.Master, null, 1500, "Принято", []),
            new(Claim, Claim, At, Master, ClaimNotificationKind.ClaimChange, null, null, null, null, null, null, "", []),
            // Нефинансовый комментарий и финансовое событие задолго до бэкапа — о них базу не спрашиваем.
            new(Claim, new ClaimCommentIdentification(Claim, 501), At, Master, ClaimNotificationKind.ClaimChange, null, null,
                null, ClaimEventAuthor.Master, null, null, "Комментарий", []),
            new(Claim, new ClaimCommentIdentification(Claim, 502), At.AddDays(-10), Master, ClaimNotificationKind.ClaimChange, null, null,
                DomainTypes.Characters.Claims.CommentExtraAction.ApproveFinance, ClaimEventAuthor.Master, null, null, "", []),
        };
        FinanceRestorePlanner.NotifiedCommentIds(events, At.AddDays(-1)).ShouldBe([500]);

        var lookup = new ClaimFinanceRestoreLookup(
            [Claim], [], [], [], [CashType()], [new MoneyMasterForRestore(Master, IsOwner: true)]);
        var plan = FinanceSourceBuilder.BuildPlan(Project, events, lookup, At.AddDays(-1));

        plan.ActingMaster.ShouldBe(Master);
        var operation = plan.Rows.ShouldHaveSingleItem().Operations.ShouldHaveSingleItem();
        operation.Key.ShouldBe("id:500");
        operation.PaymentType.PaymentTypeId.ShouldBe(7);
    }
}
