using JoinRpg.Services.Interfaces;
using JoinRpg.WebPortal.Managers.AdminTools;
using JoinRpg.XGameApi.Contract.Pscb;
using Newtonsoft.Json.Linq;
using PscbApi.Models;

namespace JoinRpg.WebPortal.Managers.Test.AdminTools;

/// <summary>
/// Преобразование ответа банка в контракт шлюза и набор колонок CSV (ADR025).
/// </summary>
public class PscbPaymentViewModelBuilderTests
{
    private static readonly DateTimeOffset StatusDate = new(2026, 1, 5, 10, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public void MapsPaymentFieldsAndUsesBankIdentifiersForCodedValues()
    {
        var dto = PscbPaymentViewModelBuilder.Build(new PaymentData
        {
            Id = "212157227",
            TransactionId = "TX-1",
            OrderId = "0000000001",
            OrderIdDisplayValue = "1",
            MarketPlace = "47607",
            Amount = 1250.00m,
            Fee = 12.50m,
            Status = PaymentStatus.Paid,
            SubStatus = PaymentSubStatus.HoldConfirmed,
            Type = PscbPaymentType.Payment,
            PaymentMethod = PscbPaymentMethod.BankCards,
            CurrentStatusDate = StatusDate,
            CustomerAccount = "42",
            CustomerComment = "за Васю",
            CustomerEmail = "payer@example.com",
            CustomerPhone = "79000000000",
            Details = "За участие в игре",
            MerchantData = JObject.Parse("""{"claimId":42}"""),
            RecurrentPaymentToken = "TOKEN",
            RegistryId = "R-77",
            RegistryDate = StatusDate,
        });

        dto.PaymentId.ShouldBe("212157227");
        dto.TransactionId.ShouldBe("TX-1");
        dto.OrderId.ShouldBe("0000000001");
        dto.DisplayOrderId.ShouldBe("1");
        dto.MarketPlace.ShouldBe("47607");
        dto.Amount.ShouldBe(1250.00m);
        dto.Fee.ShouldBe(12.50m);
        dto.Status.ShouldBe("end");
        dto.SubStatus.ShouldBe("hold_confirmed");
        dto.Kind.ShouldBe("payment");
        dto.PaymentMethod.ShouldBe("ac");
        dto.StatusDate.ShouldBe(StatusDate);
        dto.CustomerAccount.ShouldBe("42");
        dto.CustomerComment.ShouldBe("за Васю");
        dto.CustomerEmail.ShouldBe("payer@example.com");
        dto.CustomerPhone.ShouldBe("79000000000");
        dto.Details.ShouldBe("За участие в игре");
        dto.MerchantData.ShouldBe("""{"claimId":42}""");
        dto.RecurrentPaymentToken.ShouldBe("TOKEN");
        dto.RegistryId.ShouldBe("R-77");
        dto.RegistryDate.ShouldBe(StatusDate);
    }

    [Fact]
    public void MapsCardRefundsAndReceipts()
    {
        var dto = PscbPaymentViewModelBuilder.Build(new PaymentData
        {
            Id = "11",
            OrderId = "0000000001",
            Card = new BankCardData
            {
                MaskedName = "427655++++++1084",
                Type = "VISA",
                Bank = "SBERBANK",
                Country = "Россия",
                CountryCode = "RUS",
                ExpirationDate = "2204",
                ReferenceRetrievalNumber = "7432467657657768",
                ProcessingStatus = "approved",
                CardholderEmail = "payer@example.com",
            },
            Refunds = [new RefundData { Id = "RF-1", Status = RefundStatus.Completed, Amount = 100m }],
            Receipts = [new ReceiptData { Id = "RC-1", Type = ReceiptType.Income, Amount = 1250m, DocumentNumber = "12" }],
        });

        dto.Card!.MaskedNumber.ShouldBe("427655++++++1084");
        dto.Card.CardType.ShouldBe("VISA");
        dto.Card.ReferenceRetrievalNumber.ShouldBe("7432467657657768");
        dto.Refunds.ShouldHaveSingleItem().RefundId.ShouldBe("RF-1");
        dto.Refunds.Single().Status.ShouldBe("END");
        dto.Receipts.ShouldHaveSingleItem().Type.ShouldBe("income");
    }

    /// <summary>
    /// В списке банк кладёт lastError внутрь каждого элемента, и для завершённого платежа это
    /// не ошибка, а описание состояния — «Платеж завершен».
    /// </summary>
    [Fact]
    public void MapsTheBankLastActionAndShowsItInCsv()
    {
        var dto = PscbPaymentViewModelBuilder.Build(new PaymentData
        {
            Id = "1128001119",
            OrderId = "0000643617",
            Status = PaymentStatus.Paid,
            LastError = new BankResponseInfo
            {
                Description = "Платеж завершен",
                PaymentCode = PaymentsSystemResponseCode.Success,
                ProcessingCode = ProcessingCenterResponseCode.Approved,
            },
        });

        dto.LastAction.ShouldNotBeNull();
        dto.LastAction.Description.ShouldBe("Платеж завершен");
        dto.LastAction.PaymentsSystemCode.ShouldBe(0);
        dto.LastAction.ProcessingCenterCode.ShouldBe(0);

        var columns = new PscbPaymentsCsvExporter().ParseColumns().ToList();
        ByName(columns, "Описание состояния").ExtractValue(dto).ShouldBe("Платеж завершен");
    }

    /// <summary>
    /// Банк возвращает разный набор полей в зависимости от статуса платежа и флагов запроса,
    /// поэтому пустая модель обязана преобразовываться, а не падать.
    /// </summary>
    [Fact]
    public void ToleratesABarePayment()
    {
        var dto = PscbPaymentViewModelBuilder.Build(new PaymentData { Id = "11", OrderId = "0000000001" });

        dto.Card.ShouldBeNull();
        dto.LastAction.ShouldBeNull();
        dto.MerchantData.ShouldBeNull();
        dto.SubStatus.ShouldBeNull();
        dto.Kind.ShouldBeNull();
        dto.RegistryDate.ShouldBeNull();
        dto.Refunds.ShouldBeEmpty();
        dto.Receipts.ShouldBeEmpty();
    }

    [Fact]
    public void BuildsSuccessfulList()
    {
        var result = PscbPaymentViewModelBuilder.BuildList(
            new PaymentsListInfo
            {
                Status = PaymentInfoQueryStatus.Success,
                RequestId = "TEST-1",
                Payments = [new PaymentData { Id = "11", OrderId = "0000000001" }],
            },
            requestCount: 2);

        result.IsSuccess.ShouldBeTrue();
        result.RequestId.ShouldBe("TEST-1");
        result.RequestCount.ShouldBe(2);
        result.Payments.ShouldHaveSingleItem().OrderId.ShouldBe("0000000001");
    }

    [Fact]
    public void BuildsFailedListWithoutPayments()
    {
        var result = PscbPaymentViewModelBuilder.BuildList(
            new PaymentsListInfo
            {
                Status = PaymentInfoQueryStatus.Failure,
                ErrorCode = ApiErrorCode.ServerError,
                ErrorDescription = "oops",
            },
            requestCount: 1);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorCode.ShouldBe("SERVER_ERROR");
        result.ErrorDescription.ShouldBe("oops");
        result.Payments.ShouldBeEmpty();
    }

    [Fact]
    public void BuildsDetailsWithTheBankErrors()
    {
        var result = PscbPaymentViewModelBuilder.BuildDetails(new PaymentInfo
        {
            Status = PaymentInfoQueryStatus.Success,
            Description = "Платеж завершен",
            Payment = new PaymentData { Id = "11", OrderId = "0000000001" },
            Errors = [new BankResponseInfo
            {
                Description = "Declined",
                PaymentCode = PaymentsSystemResponseCode.DeclinedByProcessing,
                ProcessingCode = ProcessingCenterResponseCode.NotSufficientFunds,
            }],
        });

        result.IsSuccess.ShouldBeTrue();
        result.Description.ShouldBe("Платеж завершен");
        result.Payment!.OrderId.ShouldBe("0000000001");
        var error = result.LastErrors.ShouldHaveSingleItem();
        error.Description.ShouldBe("Declined");
        error.PaymentsSystemCode.ShouldBe((int)PaymentsSystemResponseCode.DeclinedByProcessing);
        error.ProcessingCenterCode.ShouldBe((int)ProcessingCenterResponseCode.NotSufficientFunds);
    }

    [Fact]
    public void CsvColumnsHaveRussianHeadersAndAreUnique()
    {
        var columns = new PscbPaymentsCsvExporter().ParseColumns().ToList();

        columns.ShouldNotBeEmpty();
        columns.Select(c => c.Name).ShouldAllBe(name => !string.IsNullOrWhiteSpace(name));
        columns.Select(c => c.Name).Distinct().Count().ShouldBe(columns.Count);
    }

    /// <summary>
    /// Чеки и возвраты приходят не всегда, поэтому ни одна колонка не имеет права падать на пустом
    /// платеже: выгрузка периода без флагов — обычный случай.
    /// </summary>
    [Fact]
    public void CsvColumnsTolerateAnEmptyPayment()
    {
        var columns = new PscbPaymentsCsvExporter().ParseColumns().ToList();
        var payment = new PscbPaymentInfo { OrderId = "0000000001", Amount = 100m };

        foreach (var column in columns)
        {
            Should.NotThrow(() => column.ExtractValue(payment));
        }

        ByName(columns, "Возвратов").ExtractValue(payment).ShouldBe(0);
        ByName(columns, "Чеков").ExtractValue(payment).ShouldBe(0);
        ByName(columns, "Номер карты").ExtractValue(payment).ShouldBeNull();
        ByName(columns, "Описание состояния").ExtractValue(payment).ShouldBeNull();
    }

    [Fact]
    public void CsvColumnsRenderNestedCollectionsAsCountAndIds()
    {
        var columns = new PscbPaymentsCsvExporter().ParseColumns().ToList();
        var payment = new PscbPaymentInfo
        {
            OrderId = "0000000001",
            Amount = 1250.50m,
            StatusDate = StatusDate,
            Refunds =
            [
                new PscbRefundInfo { RefundId = "RF-1", Amount = 100m },
                new PscbRefundInfo { RefundId = "RF-2", Amount = 50m },
            ],
            Receipts = [new PscbReceiptInfo { ReceiptId = "RC-1" }],
        };

        ByName(columns, "Сумма").ExtractValue(payment).ShouldBe("1250.5");
        ByName(columns, "Возвратов").ExtractValue(payment).ShouldBe(2);
        ByName(columns, "Возвраты").ExtractValue(payment).ShouldBe("RF-1 | RF-2");
        ByName(columns, "Чеков").ExtractValue(payment).ShouldBe(1);
        ByName(columns, "Чеки").ExtractValue(payment).ShouldBe("RC-1");
        ByName(columns, "Дата статуса").ExtractValue(payment).ShouldBe("2026-01-05 10:00:00 +03:00");
    }

    private static ITableColumn ByName(IReadOnlyCollection<ITableColumn> columns, string name)
        => columns.Single(c => c.Name == name);
}
