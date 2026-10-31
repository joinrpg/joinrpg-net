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
/// Оркестрация восстановления потерянных платежей ПСКБ (ADR028): один запрос — одна строка
/// заново скачанного списка, найденная по <c>orderId</c>. Отказ всего прогона (банк не вернул
/// список, сверка с базой не удалась) приходит исключением, исход строки — результатом.
/// Продолжение прогона после сбоя строки живёт в острове (<c>PscbPaymentsPanelTest</c>).
/// Банк подменён, доменный сервис — рукописный фейк.
/// </summary>
public class PscbPaymentsRestoreViewServiceTests
{
    private const string LostPaymentOrderId = "0000063617";

    private const string StatusDateIso = "2026-05-10T15:00:00+03:00";

    private static readonly DateTime StatusDateUtc = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Потерянный платёж: проект 7 «Проект», плательщик 42, его заявка 55, онлайн-тип 3.</summary>
    [Fact]
    public async Task RestoresTheLostPayment()
    {
        var (service, domain) = CreateService(LostPaymentScenario());

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        var input = domain.Payments.ShouldHaveSingleItem();
        input.OperationId.ShouldBe(63617);
        input.ClaimId.ShouldBe(new ClaimIdentification(7, 55));
        input.PaymentTypeId.ShouldBe(new PaymentTypeIdentification(7, 3));
        input.MoneyAmount.ShouldBe(1500);
        input.OperationDate.ShouldBe(StatusDateUtc);
        input.BankPaymentId.ShouldBe("11");
        input.Refund.ShouldBeNull();

        result.OrderId.ShouldBe(LostPaymentOrderId);
        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Restored);
        result.Message.ShouldBe("платёж восстановлен");
        result.RestoredPaymentsCount.ShouldBe(1);
        result.RestoredRefundsCount.ShouldBe(0);
    }

    /// <summary>
    /// Банк зафиксировал возврат потерянного платежа (state=ref): возврат восстанавливается
    /// тем же запросом, вторым записыванием — строка отчитывается обеими записями.
    /// </summary>
    /// <remarks>
    /// Ограничение источника данных (ADR028): у строки state=ref список банка даёт лишь один
    /// момент — <c>stateDate</c> возврата, настоящей даты платежа в списке нет. Платёж и возврат
    /// намеренно восстанавливаются этой одной датой, поэтому <c>OperationDate</c> платежа равна
    /// дате возврата — проектное решение, а не пропуск: оно зафиксировано утверждением ниже.
    /// </remarks>
    [Fact]
    public async Task RestoresTheLostPaymentTogetherWithItsRefund()
    {
        var scenario = LostPaymentScenario();
        scenario.Payments = [LostPaymentJson(refunds: """[{"refundId":"1115846166","state":"END","amount":1500.0}]""", state: "ref")];
        var (service, domain) = CreateService(scenario);

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        var payment = domain.Payments.ShouldHaveSingleItem();
        // Дата денег платежа — stateDate возврата: для пары «потерянный платёж + возврат» это
        // единственная дата, которую знает список банка (ADR028).
        payment.OperationDate.ShouldBe(StatusDateUtc);

        var refund = payment.Refund.ShouldNotBeNull();
        refund.RefundedOperationId.ShouldBe(63617);
        refund.MoneyAmount.ShouldBe(1500);
        refund.BankRefundId.ShouldBe("1115846166");
        refund.RefundedOperationDate.ShouldBe(StatusDateUtc);

        result.RestoredPaymentsCount.ShouldBe(1);
        result.RestoredRefundsCount.ShouldBe(1);
        result.Message.ShouldBe("платёж восстановлен; возврат восстановлен");
    }

    /// <summary>
    /// Сбой вставки возврата не проваливает строку: исход диктует платёж, он восстановлен,
    /// а сообщение объясняет, что возврат не записан и почему — возврат доделает повторный прогон.
    /// </summary>
    [Fact]
    public async Task ReportsTheFailedRefundWithoutFailingTheRestoredPayment()
    {
        var scenario = LostPaymentScenario();
        scenario.Payments = [LostPaymentJson(refunds: """[{"refundId":"1115846166","state":"END","amount":1500.0}]""", state: "ref")];
        var (service, domain) = CreateService(scenario);
        domain.RefundStatus = LostOperationRestoreStatus.Failed;
        domain.RefundFailureMessage = "Source online payment operation of the claim is not found";

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        result.RestoredPaymentsCount.ShouldBe(1);
        result.RestoredRefundsCount.ShouldBe(0);
        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Restored);
        result.Message.ShouldBe("платёж восстановлен; возврат не записан — восстановите вручную: Source online payment operation of the claim is not found");
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

        var result = await service.RestoreRow(Query(), "0000637705");

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
        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Restored);
    }

    /// <summary>
    /// Строка, которой вердикт отказал, доменным сервисом не трогается: запрос возвращает отказ
    /// с причиной вердикта — той же, что видна в таблице списка.
    /// </summary>
    [Fact]
    public async Task ReportsTheBlockReasonInsteadOfRestoring()
    {
        var scenario = LostPaymentScenario();
        scenario.Claims = new PscbFakeClaimsRepository(); // заявок нет — вердикт отказывает
        var (service, domain) = CreateService(scenario);

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        domain.Payments.ShouldBeEmpty();
        domain.Refunds.ShouldBeEmpty();
        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Failed);
        result.Message.ShouldBe("у плательщика нет заявки в проекте");
        result.RestoredPaymentsCount.ShouldBe(0);
        result.RestoredRefundsCount.ShouldBe(0);
    }

    /// <summary>
    /// Проведённая строка не пишется повторно: запрос возвращает «уже в базе» — идемпотентность,
    /// на которой держится повторный прогон.
    /// </summary>
    [Fact]
    public async Task ReportsTheSettledRowAsAlreadySettled()
    {
        var scenario = LostPaymentScenario();
        scenario.Finances.AddOperation(63617);
        var (service, domain) = CreateService(scenario);

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        domain.Payments.ShouldBeEmpty();
        result.Outcome.ShouldBe(PscbRestoreRowOutcome.AlreadySettled);
        result.Message.ShouldBe("операция уже в базе — повторный прогон ничего не записал");
        result.RestoredPaymentsCount.ShouldBe(0);
        result.RestoredRefundsCount.ShouldBe(0);
    }

    /// <summary>
    /// Доменный сервис упал на строке — запрос не бросает: исход строки становится отказом
    /// с текстом сбоя. Продолжение прогона после этого — забота острова.
    /// </summary>
    [Fact]
    public async Task ConvertsARowFailureIntoAFailedOutcome()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.Throw = new InvalidOperationException("База недоступна");

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Failed);
        result.Message.ShouldBe("База недоступна");
        result.RestoredPaymentsCount.ShouldBe(0);
        result.RestoredRefundsCount.ShouldBe(0);
    }

    /// <summary>
    /// Нет админских прав — отказ всего прогона, а не строки: домен бросает
    /// <c>MustBeAdminException</c>, и оркестратор его не проглатывает.
    /// </summary>
    [Fact]
    public async Task PropagatesTheAdminCheckFailure()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.Throw = new MustBeAdminException();

        await Should.ThrowAsync<MustBeAdminException>(() => service.RestoreRow(Query(), LostPaymentOrderId));
    }

    /// <summary>Повторный запрос идемпотентен: операция уже в базе — ничего не записано.</summary>
    [Fact]
    public async Task ReportsTheAlreadyRestoredRowWithoutCountingIt()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.PaymentStatus = LostOperationRestoreStatus.AlreadyRestored;

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        result.RestoredPaymentsCount.ShouldBe(0);
        result.RestoredRefundsCount.ShouldBe(0);
        result.Outcome.ShouldBe(PscbRestoreRowOutcome.AlreadySettled);
        result.Message.ShouldBe("операция уже в базе — повторный прогон ничего не записал");
    }

    /// <summary>
    /// Операция дописана к комментарию заявки, возвращённому DRP без операции: запись в базе
    /// есть, поэтому строка считается восстановленной, а сообщение объясняет, куда лёг платёж.
    /// </summary>
    [Fact]
    public async Task CountsTheAttachedPaymentAsRestored()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.PaymentStatus = LostOperationRestoreStatus.Attached;

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        result.RestoredPaymentsCount.ShouldBe(1);
        result.RestoredRefundsCount.ShouldBe(0);
        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Restored);
        result.Message.ShouldBe("платёж записан в уже существующий комментарий заявки");
    }

    /// <summary>Номер занят чужим комментарием: строка уходит человеку, а не пишется с новым номером.</summary>
    [Fact]
    public async Task ReportsTheTakenNumberAsAFailure()
    {
        var (service, domain) = CreateService(LostPaymentScenario());
        domain.PaymentStatus = LostOperationRestoreStatus.NumberTaken;

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        result.RestoredPaymentsCount.ShouldBe(0);
        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Failed);
        result.Message.ShouldContain("занят");
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

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        result.RestoredPaymentsCount.ShouldBe(1);
        result.Message.ShouldContain("взнос заявки не пересчитан");
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

        var ex = await Should.ThrowAsync<JoinValidationException>(() => service.RestoreRow(Query(), LostPaymentOrderId));

        ex.Message.ShouldStartWith("Банк не вернул список платежей");
        domain.Payments.ShouldBeEmpty();
    }

    /// <summary>
    /// Сверка с базой не удалась — прогон отказывается целиком: список без исходов сверки
    /// молча прочитался бы как «нечего восстанавливать».
    /// </summary>
    [Fact]
    public async Task RefusesTheRunWhenTheReconciliationFailed()
    {
        var scenario = LostPaymentScenario();
        scenario.Finances.Throw = new InvalidOperationException("База недоступна");
        var (service, domain) = CreateService(scenario);

        var ex = await Should.ThrowAsync<JoinValidationException>(() => service.RestoreRow(Query(), LostPaymentOrderId));

        ex.Message.ShouldContain("Сверка списка с базой не удалась");
        domain.Payments.ShouldBeEmpty();
    }

    /// <summary>
    /// Строки нет в свежем списке банка (период изменился, заказ переименован) — отказ строки
    /// с предложением обновить список, а не тихий ноль.
    /// </summary>
    [Fact]
    public async Task ReportsTheRowMissingFromTheBankList()
    {
        var (service, domain) = CreateService(LostPaymentScenario());

        var result = await service.RestoreRow(Query(), "0000999999");

        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Failed);
        result.Message.ShouldContain("не найдена");
        domain.Payments.ShouldBeEmpty();
        domain.Refunds.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefusesAPartialRefundEvenWhenThePaymentStatusIsPaid()
    {
        var scenario = LostPaymentScenario();
        scenario.Payments = [LostPaymentJson(refunds: """[{"refundId":"1","state":"END","amount":500}]""")];
        var (service, domain) = CreateService(scenario);

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Failed);
        result.Message.ShouldBe("частичный возврат");
        domain.Payments.ShouldBeEmpty();
        domain.Refunds.ShouldBeEmpty();
    }

    [Fact]
    public async Task RefusesAnAmbiguousOrderIdWithoutWritingMoney()
    {
        var scenario = LostPaymentScenario();
        scenario.Payments = [LostPaymentJson(), LostPaymentJson().Replace("\"paymentId\":\"11\"", "\"paymentId\":\"12\"")];
        var (service, domain) = CreateService(scenario);

        var result = await service.RestoreRow(Query(), LostPaymentOrderId);

        result.Outcome.ShouldBe(PscbRestoreRowOutcome.Failed);
        result.Message.ShouldContain("несколько раз");
        domain.Payments.ShouldBeEmpty();
        domain.Refunds.ShouldBeEmpty();
    }

    /// <summary>Пустой orderId отклоняется до первого обращения к банку.</summary>
    [Fact]
    public async Task RefusesABlankOrderId()
    {
        var (service, domain) = CreateService(LostPaymentScenario());

        _ = await Should.ThrowAsync<ArgumentOutOfRangeException>(() => service.RestoreRow(Query(), " "));

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

        /// <summary>Текст сбоя возврата — попадает в <c>FailureMessage</c> при исходе <c>Failed</c>.</summary>
        public string? RefundFailureMessage { get; set; }

        public bool FeeRecalculated { get; set; } = true;

        /// <summary>Исключение на каждый вызов — например, отказ доступа.</summary>
        public Exception? Throw { get; set; }

        public Task<LostPaymentRestoreResult> RestorePaymentAsync(LostPaymentToRestore payment)
        {
            Payments.Add(payment);
            if (Throw is not null)
            {
                throw Throw;
            }

            return Task.FromResult(new LostPaymentRestoreResult(
                PaymentStatus,
                payment.OperationId,
                PaymentStatus == LostOperationRestoreStatus.Restored && payment.Refund is not null
                    ? RefundStatus == LostOperationRestoreStatus.Failed
                        // Сбой возврата: записи нет — ни номера, ни пересчёта по возврату, только текст сбоя.
                        ? new LostRefundRestoreResult(LostOperationRestoreStatus.Failed, OperationId: null, FeeRecalculated, RefundFailureMessage)
                        : new LostRefundRestoreResult(RefundStatus, payment.OperationId + 1000, FeeRecalculated)
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
