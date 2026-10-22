using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using PscbApi.Models;

namespace PscbApi.Test;

/// <summary>
/// Запрос параметров платежа: адрес, флаги и разбор полей, добавленных для шлюза (ADR025).
/// </summary>
/// <remarks>
/// Тесты не ходят на сервер банка — ни на боевой, ни на тестовый: транспорт подменён
/// самодельным <see cref="HttpMessageHandler"/>.
/// </remarks>
public class BankApiGetPaymentInfoTests
{
    private const string MerchantId = "47607";
    private const string ApiEndpoint = "https://bank.example.invalid";
    private const string CheckPaymentUrl = $"{ApiEndpoint}/merchantApi/checkPayment";

    [Fact]
    public async Task RequestsPaymentParametersFromTheBankEndpoint()
    {
        var handler = new RecordingHandler(_ => """{"status":"STATUS_SUCCESS","payment":{"orderId":"0000000001"}}""");

        var result = await CreateApi(handler).GetPaymentInfoAsync("0000000001");

        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Url.ShouldBe(CheckPaymentUrl);

        var body = ParseBody(handler.Requests[0].Body);
        body["orderId"]!.Value<string>().ShouldBe("0000000001");
        body["marketPlace"]!.Value<string>().ShouldBe(MerchantId);
        body["requestCardData"]!.Value<bool>().ShouldBeFalse();
        body["requestFiscalData"]!.Value<bool>().ShouldBeFalse();
        body["requestClearingData"]!.Value<bool>().ShouldBeFalse();

        result.Status.ShouldBe(PaymentInfoQueryStatus.Success);
        result.Payment!.OrderId.ShouldBe("0000000001");
    }

    [Fact]
    public async Task SendsTheRequestedDataFlags()
    {
        var handler = new RecordingHandler(_ => """{"status":"STATUS_SUCCESS","payment":{"orderId":"0000000001"}}""");

        _ = await CreateApi(handler).GetPaymentInfoAsync("0000000001", getCardData: true, getFiscalData: true, getClearingData: true);

        var body = ParseBody(handler.Requests.Single().Body);
        body["requestCardData"]!.Value<bool>().ShouldBeTrue();
        body["requestFiscalData"]!.Value<bool>().ShouldBeTrue();
        body["requestClearingData"]!.Value<bool>().ShouldBeTrue();
    }

    /// <summary>
    /// Поля, которых не хватало в модели: без них шлюз не смог бы отдать «все детали» платежа,
    /// которые знает банк.
    /// </summary>
    [Fact]
    public async Task ParsesTheFieldsAddedForTheGateway()
    {
        var handler = new RecordingHandler(_ =>
            """
            {
              "status": "STATUS_SUCCESS",
              "requestId": "TEST-1",
              "description": "Платеж завершен",
              "payment": {
                "orderId": "0000000001",
                "paymentId": "212157227",
                "transactionId": "TX-1",
                "amount": 1250.00,
                "fee": 12.50,
                "state": "end",
                "subState": "hold_confirmed",
                "type": "payment",
                "marketPlace": "47607",
                "paymentMethod": "ac",
                "stateDate": "2026-01-05T10:00:00+03:00",
                "merchantData": { "claimId": 42 },
                "registryId": "R-77",
                "registryDate": "2026-01-06T00:00:00+03:00",
                "paymentParams": {
                  "card": "427655++++++1084",
                  "card_type": "VISA",
                  "rrn": "7432467657657768",
                  "cardholderEmail": "payer@example.com"
                }
              }
            }
            """);

        var result = await CreateApi(handler).GetPaymentInfoAsync("0000000001");
        var payment = result.Payment!;

        result.Description.ShouldBe("Платеж завершен");
        payment.TransactionId.ShouldBe("TX-1");
        payment.Fee.ShouldBe(12.50m);
        payment.Type.ShouldBe(PscbPaymentType.Payment);
        payment.MarketPlace.ShouldBe(MerchantId);
        payment.SubStatus.ShouldBe(PaymentSubStatus.HoldConfirmed);
        payment.PaymentMethod.ShouldBe(PscbPaymentMethod.BankCards);
        payment.RegistryId.ShouldBe("R-77");
        payment.RegistryDate!.Value.UtcDateTime.ShouldBe(new DateTime(2026, 1, 5, 21, 0, 0, DateTimeKind.Utc));
        payment.MerchantData!["claimId"]!.Value<int>().ShouldBe(42);
        payment.Card!.MaskedName.ShouldBe("427655++++++1084");
        payment.Card.ReferenceRetrievalNumber.ShouldBe("7432467657657768");
        payment.Card.CardholderEmail.ShouldBe("payer@example.com");
    }

    [Fact]
    public async Task ParsesTheErrorReportedByTheBank()
    {
        var handler = new RecordingHandler(_ =>
            """
            {
              "status": "STATUS_FAILURE",
              "requestId": "TEST-2",
              "errorCode": "UNKNOWN_PAYMENT",
              "errorDescription": "Payment not found"
            }
            """);

        var result = await CreateApi(handler).GetPaymentInfoAsync("9999999999");

        // Банк отвечает кодом HTTP 200 и на отказ тоже, поэтому статус читается из тела.
        result.Status.ShouldBe(PaymentInfoQueryStatus.Failure);
        result.ErrorCode.ShouldBe(ApiErrorCode.UnknownPayment);
        result.ErrorDescription.ShouldBe("Payment not found");
        result.Payment.ShouldBeNull();
    }

    /// <summary>
    /// В <c>checkPayment</c> банк кладёт <c>lastError</c> массивом в конверт, а не внутрь платежа,
    /// как в списке. Объект при этом тот же, что и в <c>getPayments</c>, поэтому текст <c>null</c>
    /// вместо кода процессинга ронял и боевой путь обновления статуса платежа — дефект был латентным
    /// до появления шлюза (ADR025).
    /// </summary>
    [Fact]
    public async Task ToleratesTheLiteralTextNullInTheEnvelopeErrors()
    {
        var handler = new RecordingHandler(_ =>
            """
            {
              "status": "STATUS_SUCCESS",
              "requestId": "TEST-3",
              "description": "Платеж завершен",
              "payment": { "orderId": "0000000001", "state": "end" },
              "lastError": [
                { "description": "Платеж завершен", "code": "0", "subCode": "null" }
              ]
            }
            """);

        var result = await CreateApi(handler).GetPaymentInfoAsync("0000000001");

        result.Status.ShouldBe(PaymentInfoQueryStatus.Success);
        var error = result.Errors.ShouldHaveSingleItem();
        error.Description.ShouldBe("Платеж завершен");
        error.PaymentCode.ShouldBe(PaymentsSystemResponseCode.Success);
        error.ProcessingCode.ShouldBeNull();
    }

    /// <summary>
    /// Разбирает тело запроса, не превращая даты в <see cref="DateTime"/>.
    /// </summary>
    private static JObject ParseBody(string json)
    {
        using var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(json)) { DateParseHandling = Newtonsoft.Json.DateParseHandling.None };
        return JObject.Load(reader);
    }

    private static BankApi CreateApi(HttpMessageHandler handler)
        => new(
            new StubHttpClientFactory(handler),
            new ApiConfiguration
            {
                ApiEndpoint = ApiEndpoint,
                ApiDebugEndpoint = "https://demo.bank.example.invalid",
                MerchantId = MerchantId,
                ApiKey = "secret-key",
                ApiDebugKey = "debug-secret-key",
            },
            NullLogger<BankApi>.Instance);

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler);
    }

    private sealed class RecordingHandler(Func<int, string?> respond) : HttpMessageHandler
    {
        private int calls;

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(request.RequestUri!.ToString(), body));

            var payload = respond(calls++);
            return payload is null
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        }
    }

    private sealed record RecordedRequest(string Url, string Body);
}
