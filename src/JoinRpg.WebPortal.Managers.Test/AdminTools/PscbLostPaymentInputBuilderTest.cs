using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.WebPortal.Managers.AdminTools;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools;

/// <summary>
/// Преобразование обогащённой строки списка ПСКБ и её плана во вход доменного сервиса
/// восстановления (ADR027). Чистая функция: ни банка, ни репозиториев.
/// </summary>
public class PscbLostPaymentInputBuilderTest
{
    /// <summary>Момент, когда платёж вошёл в текущий статус, в часовом поясе банка.</summary>
    private static readonly DateTimeOffset StatusDate = new(2026, 5, 10, 15, 0, 0, TimeSpan.FromHours(3));

    /// <summary>Те же деньги в UTC — доменный вход хранит дату денег в UTC.</summary>
    private static readonly DateTime StatusDateUtc = new(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime OriginalPaymentDate = new(2026, 4, 1, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MapsTheLostPaymentIntoTheDomainInput()
    {
        var input = PscbLostPaymentInputBuilder.BuildPayment(LostRow(), Plan());

        input.ClaimId.ShouldBe(new ClaimIdentification(7, 55));
        input.OperationId.ShouldBe(63617);
        input.PaymentTypeId.ShouldBe(new PaymentTypeIdentification(7, 3));
        input.MoneyAmount.ShouldBe(1500);
        input.OperationDate.ShouldBe(StatusDateUtc);
        input.BankPaymentId.ShouldBe("1128001119");
        input.PayerComment.ShouldBe("За Василия");
        input.Refund.ShouldBeNull();
    }

    /// <summary>
    /// Банк зафиксировал возврат потерянного платежа (state=ref): возврат восстанавливается
    /// вторым записыванием, со ссылкой на сам платёж и его же датой денег — другой даты у строки
    /// потерянного платежа нет.
    /// </summary>
    /// <remarks>
    /// Ограничение источника данных (ADR027): у строки state=ref список банка даёт лишь один
    /// момент — <c>stateDate</c> возврата, настоящей даты платежа в списке нет. Обе записи
    /// намеренно восстанавливаются этой датой, поэтому <c>OperationDate</c> платёжа равна дате
    /// возврата — проектное решение, а не пропуск: поведение зафиксировано утверждением ниже.
    /// </remarks>
    [Fact]
    public void AddsTheRefundOfTheLostPaymentWithTheRefundMomentAsThePaymentDate()
    {
        var input = PscbLostPaymentInputBuilder.BuildPayment(LostRow(refunded: true), Plan());

        // Дата денег платежа — stateDate возврата: для пары «потерянный платёж + возврат» это
        // единственная дата, которую знает список банка (ADR027).
        input.OperationDate.ShouldBe(StatusDateUtc);

        var refund = input.Refund.ShouldNotBeNull();
        refund.ClaimId.ShouldBe(new ClaimIdentification(7, 55));
        refund.RefundedOperationId.ShouldBe(63617);
        refund.PaymentTypeId.ShouldBe(new PaymentTypeIdentification(7, 3));
        refund.MoneyAmount.ShouldBe(1500);
        refund.OperationDate.ShouldBe(StatusDateUtc);
        refund.RefundedOperationDate.ShouldBe(StatusDateUtc);
        refund.BankPaymentId.ShouldBe("1128001119");
        refund.BankRefundId.ShouldBe("1115846166");
    }

    /// <summary>
    /// Пропущенный возврат: заявка, тип оплаты и дата исходного платежа берутся из плана —
    /// их определила сверка по найденной операции.
    /// </summary>
    [Fact]
    public void MapsTheMissingRefundFromThePlan()
    {
        var refund = PscbLostPaymentInputBuilder.BuildRefund(
            LostRow(refunded: true),
            Plan(refundedOperationDate: OriginalPaymentDate));

        refund.ClaimId.ShouldBe(new ClaimIdentification(7, 55));
        refund.RefundedOperationId.ShouldBe(63617);
        refund.PaymentTypeId.ShouldBe(new PaymentTypeIdentification(7, 3));
        refund.MoneyAmount.ShouldBe(1500);
        refund.OperationDate.ShouldBe(StatusDateUtc);
        refund.RefundedOperationDate.ShouldBe(OriginalPaymentDate);
        refund.BankRefundId.ShouldBe("1115846166");
    }

    /// <summary>
    /// Дата исходного платежа — часть плана пропущенного возврата: без неё текст комментария
    /// «возврат платежа от ...» не построить.
    /// </summary>
    [Fact]
    public void ThrowsWhenTheRefundPlanCarriesNoOriginalOperationDate()
    {
        var ex = Should.Throw<ArgumentException>(
            () => PscbLostPaymentInputBuilder.BuildRefund(LostRow(refunded: true), Plan()));

        ex.Message.ShouldContain("даты исходной операции");
    }

    [Fact]
    public void ThrowsOnAnAmountWithKopecks()
    {
        var row = LostRow();
        row.Amount = 1500.50m;

        Should.Throw<ArgumentException>(() => PscbLostPaymentInputBuilder.BuildPayment(row, Plan()));
    }

    /// <summary>
    /// Значение вроде <c>__REPLACED_0000644272</c> номером операции не является. Вердикт такие
    /// строки отсеивает, поэтому здесь это уже рассогласование входа.
    /// </summary>
    [Fact]
    public void ThrowsWhenTheOrderIdIsNotANumber()
    {
        var row = LostRow();
        row.OrderId = "__REPLACED_000063617";

        Should.Throw<ArgumentException>(() => PscbLostPaymentInputBuilder.BuildPayment(row, Plan()));
    }

    [Fact]
    public void ThrowsWhenTheBankPaymentIdIsMissing()
    {
        var row = LostRow();
        row.BankPaymentId = null;

        Should.Throw<ArgumentException>(() => PscbLostPaymentInputBuilder.BuildPayment(row, Plan()));
    }

    /// <summary>
    /// Несколько возвратов у одного платежа вердикт отсеивает: модель возврата у нас одна
    /// на платёж, и выбирать из двух нечего.
    /// </summary>
    [Fact]
    public void ThrowsWhenTheRowCarriesSeveralRefunds()
    {
        var row = LostRow(refunded: true);
        row.Refunds = [FullRefund(), FullRefund()];

        Should.Throw<ArgumentException>(() => PscbLostPaymentInputBuilder.BuildPayment(row, Plan()));
    }

    [Fact]
    public void ThrowsWhenTheRefundIdIsMissing()
    {
        var row = LostRow(refunded: true);
        row.Refunds = [new PscbRefundInfo { RefundId = null, Status = "END", Amount = 1500m }];

        Should.Throw<ArgumentException>(() => PscbLostPaymentInputBuilder.BuildPayment(row, Plan()));
    }

    private static RowRestorePlan Plan(DateTime? refundedOperationDate = null) => new(
        new ClaimIdentification(7, 55),
        new PaymentTypeIdentification(7, 3),
        refundedOperationDate);

    private static PscbPaymentListRow LostRow(bool refunded = false) => new()
    {
        OrderId = "0000063617",
        BankPaymentId = "1128001119",
        Amount = 1500m,
        Status = refunded ? "ref" : "end",
        RefundedByBank = refunded,
        Refunds = refunded ? [FullRefund()] : [],
        StatusDate = StatusDate,
        PayerComment = "За Василия",
        MatchState = PscbPaymentMatchState.Lost,
    };

    private static PscbRefundInfo FullRefund() => new() { RefundId = "1115846166", Status = "END", Amount = 1500m };
}
