using System.Net;
using System.Text;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Interfaces;
using JoinRpg.WebPortal.Managers.AdminTools;
using JoinRpg.WebPortal.Managers.Test.AdminTools.Pscb;
using JoinRpg.XGameApi.Contract.Pscb;
using Microsoft.Extensions.Logging.Abstractions;
using static JoinRpg.WebPortal.Managers.Test.AdminTools.Pscb.PscbPaymentsTestData;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools;

/// <summary>
/// Оркестрация восстановления потерянных платежей ПСКБ (ADR027): заново скачанный список,
/// кандидаты по вердикту сверки, одна строка — одна запись доменного сервиса, продолжение после
/// ошибки и отчёт по каждой строке. Банк подменён, доменный сервис — рукописный фейк.
/// </summary>
public class PscbPaymentsRestoreViewServiceTests
{
    private const string StatusDateIso = "2026-05-10T15:00:00+03:00";

    private static readonly DateTime StatusDateUtc = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Потерянный платёж: проект 7 «Проект», плательщик 42, его заявка 55, онлайн-тип 3.</summary>
    [Fact]
    public async Task RestoresTheLostPayment()
    {
        var (service, domain) = CreateService(LostPaymentScenario());

        var result = await service.Restore(Query());

        var input = domain.Payments.ShouldHaveSingleItem();
        input.OperationId.ShouldBe(63617);
        input.ClaimId.ShouldBe(new ClaimIdentification(7, 55));
        input.PaymentTypeId.ShouldBe(new PaymentTypeIdentification(7, 3));
        input.MoneyAmount.ShouldBe(1500);
        input.OperationDate.ShouldBe(StatusDateUtc);
        input.BankPaymentId.ShouldBe("11");
        input.Refund.ShouldBeNull();

        result.RestoredPaymentsCount.ShouldBe(1);
        result.RestoredRefundsCount.ShouldBe(0);
        var row = result.Rows.ShouldHaveSingleItem();
        row.OrderId.ShouldBe("0000063617");
        row.Outcome.ShouldBe(PscbRestoreRowOutcome.Restored);
        row.Message.ShouldBe("платёж восстановлен");
    }

    /// <summary>
    /// Банк зафиксировал возврат потерянного платежа (state=ref): возврат восстанавливается
    /// тем же прогоном, вторым записыванием.
    /// </summary>
    [Fact]
    public async Task RestoresTheLostPaymentTogetherWithItsRefund()
    {
        var scenario = LostPaymentScenario();
        scenario.Payments = [LostPaymentJson(refunds: """[{"refundId":"1115846166","state":"END","amount":1500.0}]""", state: "ref")];
        var (service, domain) = CreateService(scenario);

        var result = await service.Restore(Query());

        var refund = domain.Payments.ShouldHaveSingleItem().Refund.ShouldNotBeNull();
        refund.RefundedOperationId.ShouldBe(63617);
        refund.MoneyAmount.ShouldBe(1500);
        refund.BankRefundId.ShouldBe("1115846166");
        refund.RefundedOperationDate.ShouldBe(StatusDateUtc);

        result.RestoredPaymentsCount.ShouldBe(1);
        result.RestoredRefundsCount.ShouldBe(1);
        result.Rows.ShouldHaveSingleItem().Message.ShouldBe("платёж восстановлен; возврат восстановлен");
    }

    /// <summary>
    /// Пропущенный возврат: платёж проведён, а возврата у нас нет. Заявка, тип оплаты и дата
    /// исходного платежа берутся у найденной операции — деньги возвращаются туда, откуда пришли.
    /// </summary>
    [Fact]
    public async Task RestoresTheMissingRefundFromTheFoundOperation()
    {
        var scenario = new Scenario();
        scenario.Finances.AddOperation(637705, claimId: 9, operationDate: new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc));
        scenario.Projects.AddReadyProject(42);
        scenario.Payments =
        [
            Payment(
                "0000637705",
                account: null,
                details: null,
                refunds: """[{"refundId":"1115846166","state":"END","amount":100.0}]""",
                state: "ref",
                statusDate: StatusDateIso),
        ];
        var (service, domain) = CreateService(scenario);

        var result = await service.Restore(Query());

        domain.Payments.ShouldBeEmpty();
        var refund = domain.Refunds.ShouldHaveSingleItem();
        refund.RefundedOperationId.ShouldBe(637705);
        refund.ClaimId.ShouldBe(new ClaimIdentification(42, 9));
        refund.PaymentTypeId.ShouldBe(new PaymentTypeIdentification(42, 3));
        refund.MoneyAmount.ShouldBe(100);
        refund.OperationDate.ShouldBe(StatusDateUtc);
        refund.RefundedOperationDate.ShouldBe(new DateTime(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc));

        result.RestoredPaymentsCount.ShouldBe(0);
        result.RestoredRefundsCount.ShouldBe(1);
        result.Rows.ShouldHaveSingleItem().Outcome.ShouldBe(PscbRestoreRowOutcome.Restored);
    }

    /// <summary>
    /// Строка, которой вердикт отказал, в прогон не попадает вовсе: доменный сервис о ней
    /// не спрашивают, а причина остаётся в таблице списка.
    /// </summary>
    [Fact]
    public async Task SkipsTheRowsBlockedByTheVerdict()
    {
        var scenario = LostPaymentScenario();
        scenario.Claims = new PscbFakeClaimsRepository(); // заявок нет — вердикт отказывает
        var (service, domain) = CreateService(scenario);

        var result = await service.Restore(Query());

        domain.Payments.ShouldBeEmpty();
        domain.Refunds.ShouldBeEmpty();
        result.Rows.ShouldBeEmpty();
        result.RestoredPaymentsCount.ShouldBe(0);
    }

    /// <summary>Проведённые строки прогон не трогает: восстанавливать нечего — пустой успех.</summary>
    [Fact]
    public async Task RestoresNothingWhenNoRowIsLost()
    {
        var scenario = LostPaymentScenario();
        scenario.Finances.AddOperation(63617);
        var (service, domain) = CreateService(scenario);

        var result = await service.Restore(Query());

        domain.Payments.ShouldBeEmpty();
        result.Rows.ShouldBeEmpty();
        result.RestoredPaymentsCount.ShouldBe(0);
    }

    /// <summary>
    /// Обработка по одной строке: падение одной не уносит остальные — как в FinanceRestorePanel.
    /// </summary>
    [Fact]
    public async Task ContinuesWithTheNextRowAfterAFailure()
    {
        var scenario = LostPaymentScenario();
        scenario.Payments =
        [
            LostPaymentJson(),
            Payment("0000063618", account: "42", details: TicketPurpose("Проект"), statusDate: StatusDateIso),
        ];
        var (service, domain) = CreateService(scenario);
        domain.ThrowByOperationId[63617] = new InvalidOperationException("База недоступна");

        var result = await service.Restore(Query());

        domain.Payments.Count.ShouldBe(2);
        result.Rows.Count.ShouldBe(2);
        result.Rows.First().Outcome.ShouldBe(PscbRestoreRowOutcome.Failed);
        result.Rows.First().Message.ShouldBe("База недоступна");
        result.Rows.Last().Outcome.ShouldBe(PscbRestoreRowOutcome.Restored);
        result.RestoredPaymentsCount.ShouldBe(1);
    }

    /// <summary>
    /// Нет админских прав — отказ всего прогона, а не по строке: домен бросает
    /// <c>MustBeAdminException</c>, и оркестратор его не проглатывает.
    /// </summary>
    [Fact]
    public async Task PropagatesTheAdminCheckFailure()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.Throw = new MustBeAdminException();

        await Should.ThrowAsync<MustBeAdminException>(() => service.Restore(Query()));
    }

    /// <summary>Повторный прогон идемпотентен: операция уже в базе — ничего не записано.</summary>
    [Fact]
    public async Task ReportsTheAlreadyRestoredRowWithoutCountingIt()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.PaymentStatus = LostOperationRestoreStatus.AlreadyRestored;

        var result = await service.Restore(Query());

        result.RestoredPaymentsCount.ShouldBe(0);
        var row = result.Rows.ShouldHaveSingleItem();
        row.Outcome.ShouldBe(PscbRestoreRowOutcome.AlreadySettled);
        row.Message.ShouldBe("операция уже в базе — повторный прогон ничего не записал");
    }

    /// <summary>Номер занят чужим комментарием: строка уходит человеку, а не пишется с новым номером.</summary>
    [Fact]
    public async Task ReportsTheTakenNumberAsAFailure()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.PaymentStatus = LostOperationRestoreStatus.NumberTaken;

        var result = await service.Restore(Query());

        result.RestoredPaymentsCount.ShouldBe(0);
        var row = result.Rows.ShouldHaveSingleItem();
        row.Outcome.ShouldBe(PscbRestoreRowOutcome.Failed);
        row.Message.ShouldContain("занят");
    }

    /// <summary>
    /// Операция записана, а пересчёт взноса не сохранился: промах не молчит — предупреждение
    /// в отчёте строки.
    /// </summary>
    [Fact]
    public async Task WarnsWhenTheFeeWasNotRecalculated()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.FeeRecalculated = false;

        var result = await service.Restore(Query());

        result.RestoredPaymentsCount.ShouldBe(1);
        result.Rows.ShouldHaveSingleItem().Message.ShouldContain("взнос заявки не пересчитан");
    }

    /// <summary>
    /// Ограничение на прогон: больше ста записей — отказ целиком, до первой записи,
    /// с предложением сузить диапазон.
    /// </summary>
    [Fact]
    public async Task RefusesTheWholeRunWhenThereAreTooManyRecords()
    {
        var scenario = LostPaymentScenario();
        scenario.Payments = [.. Enumerable.Range(63617, 101).Select(id => Payment(id.ToString("D10"), account: "42", details: TicketPurpose("Проект")))];
        var (service, domain) = CreateService(scenario);

        var ex = await Should.ThrowAsync<JoinValidationException>(() => service.Restore(Query()));

        ex.Message.ShouldContain("Сузьте диапазон");
        domain.Payments.ShouldBeEmpty();
    }

    /// <summary>Банк не вернул список — прогон отказывается целиком: восстанавливать нечего сверять.</summary>
    [Fact]
    public async Task RefusesTheRunWhenTheBankDidNotReturnTheList()
    {
        var view = new PscbPaymentsViewService(
            new FakeBankSecrets(),
            new RawHttpClientFactory("""{"status":"STATUS_FAIL","errorCode":"NOT_AUTHORIZED","errorDescription":"Доступ запрещён"}"""),
            new FakeUriService(),
            new FakeFinanceOperationUri(),
            new FakeClaimUri(),
            new FakeSearchService(),
            Users(42),
            new FakeFinanceOperationsRepository(),
            new PscbFakeClaimsRepository(),
            new PscbFakeProjectMetadataRepository(),
            NullLogger<PscbPaymentsViewService>.Instance);
        var domain = new FakeLostPaymentsRestoreService();
        var service = CreateRestoreService(view, domain);

        var ex = await Should.ThrowAsync<JoinValidationException>(() => service.Restore(Query()));

        ex.Message.ShouldStartWith("Банк не вернул список платежей");
        domain.Payments.ShouldBeEmpty();
    }

    /// <summary>Проект 7 «Проект» найден, плательщик 42 существует, его заявка 55, онлайн-тип один.</summary>
    private static Scenario LostPaymentScenario()
    {
        var scenario = new Scenario();
        scenario.Search.AddProject(7, "Проект");
        scenario.Users.AddExisting(42);
        scenario.Projects.AddReadyProject(7);
        scenario.Claims.AddClaim(7, 42, claimId: 55);
        scenario.Payments = [LostPaymentJson()];
        return scenario;
    }

    private static string LostPaymentJson(string? refunds = null, string state = "end")
        => Payment("0000063617", account: "42", details: TicketPurpose("Проект"), refunds: refunds, state: state, amount: "1500.0", statusDate: StatusDateIso);

    private static (PscbPaymentsRestoreViewService Service, FakeLostPaymentsRestoreService Domain) CreateService(Scenario scenario)
    {
        var view = new PscbPaymentsViewService(
            new FakeBankSecrets(),
            new StubHttpClientFactory(scenario.Payments),
            new FakeUriService(),
            new FakeFinanceOperationUri(),
            new FakeClaimUri(),
            scenario.Search,
            scenario.Users,
            scenario.Finances,
            scenario.Claims,
            scenario.Projects,
            NullLogger<PscbPaymentsViewService>.Instance);
        var domain = new FakeLostPaymentsRestoreService();
        return (CreateRestoreService(view, domain), domain);
    }

    private static PscbPaymentsRestoreViewService CreateRestoreService(
        PscbPaymentsViewService view,
        FakeLostPaymentsRestoreService domain)
        => new(
            view,
            domain,
            new FakeCurrentUserAccessor(new UserIdentification(1), isAdmin: true),
            NullLogger<PscbPaymentsRestoreViewService>.Instance);

    /// <summary>Подставной мир одного теста: банк, база и поиск.</summary>
    private sealed class Scenario
    {
        public FakeSearchService Search { get; } = new();

        public PscbFakeUserRepository Users { get; } = new();

        public FakeFinanceOperationsRepository Finances { get; } = new();

        public PscbFakeClaimsRepository Claims { get; set; } = new();

        public PscbFakeProjectMetadataRepository Projects { get; } = new();

        public string[] Payments { get; set; } = [];
    }

    /// <summary>Доменный сервис восстановления: записывает входы, исходы настраиваются тестом.</summary>
    private sealed class FakeLostPaymentsRestoreService : ILostPaymentsRestoreService
    {
        public List<LostPaymentToRestore> Payments { get; } = [];

        public List<LostRefundToRestore> Refunds { get; } = [];

        public LostOperationRestoreStatus PaymentStatus { get; set; } = LostOperationRestoreStatus.Restored;

        public LostOperationRestoreStatus RefundStatus { get; set; } = LostOperationRestoreStatus.Restored;

        public bool FeeRecalculated { get; set; } = true;

        /// <summary>Исключение на каждый вызов — например, отказ доступа.</summary>
        public Exception? Throw { get; set; }

        /// <summary>Исключение на платёж с конкретным номером — для проверки продолжения прогона.</summary>
        public Dictionary<int, Exception> ThrowByOperationId { get; } = [];

        public Task<LostPaymentRestoreResult> RestorePaymentAsync(LostPaymentToRestore payment)
        {
            Payments.Add(payment);
            if (Throw is not null)
            {
                throw Throw;
            }

            if (ThrowByOperationId.TryGetValue(payment.OperationId, out var exception))
            {
                throw exception;
            }

            return Task.FromResult(new LostPaymentRestoreResult(
                PaymentStatus,
                payment.OperationId,
                PaymentStatus == LostOperationRestoreStatus.Restored && payment.Refund is not null
                    ? new LostRefundRestoreResult(RefundStatus, payment.OperationId + 1000, FeeRecalculated)
                    : null,
                FeeRecalculated));
        }

        public Task<LostRefundRestoreResult> RestoreRefundAsync(LostRefundToRestore refund)
        {
            Refunds.Add(refund);
            if (Throw is not null)
            {
                throw Throw;
            }

            return Task.FromResult(new LostRefundRestoreResult(RefundStatus, refund.RefundedOperationId + 1000, FeeRecalculated));
        }
    }

    /// <summary>Банк, отвечающий одним заранее заданным телом — для отказа списка.</summary>
    private sealed class RawHttpClientFactory(string body) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new RawResponseHandler(body));

        private sealed class RawResponseHandler(string body) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                });
        }
    }
}
