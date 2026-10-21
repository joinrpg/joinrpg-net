using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PscbApi.Models;

namespace PscbApi.Test;

/// <summary>
/// Запрос списка платежей: адрес, подпись, нарезка на чанки, склейка и поведение на отказе банка.
/// </summary>
/// <remarks>
/// Тесты не ходят на сервер банка — ни на боевой, ни на тестовый (ADR025): транспорт подменён
/// самодельным <see cref="HttpMessageHandler"/>, приём тот же, что в <c>JoinRpg.BlobStorage.Test</c>.
/// </remarks>
public class BankApiGetPaymentsTests
{
    private const string MerchantId = "47607";
    private const string ApiKey = "secret-key";
    private const string DebugApiKey = "debug-secret-key";
    private const string ApiEndpoint = "https://bank.example.invalid";
    private const string DebugApiEndpoint = "https://demo.bank.example.invalid";
    private const string PaymentsUrl = $"{ApiEndpoint}/merchantApi/getPayments";

    private static readonly DateTimeOffset From = new(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(3));

    [Fact]
    public async Task RequestsThePaymentsListFromTheBankEndpoint()
    {
        var handler = new RecordingHandler(_ => Success("""[{"orderId":"0000000001","paymentId":"11","amount":100,"state":"end","type":"payment"}]"""));

        var result = await CreateApi(handler).GetPaymentsAsync(From, From.AddDays(5));

        handler.Requests.Count.ShouldBe(1);
        handler.Requests[0].Url.ShouldBe(PaymentsUrl);

        var body = ParseBody(handler.Requests[0].Body);
        body["marketPlace"]!.Value<string>().ShouldBe(MerchantId);
        body["dateFrom"]!.Value<string>().ShouldBe("2026-01-01T00:00:00+03:00");
        body["dateTo"]!.Value<string>().ShouldBe("2026-01-06T00:00:00+03:00");

        result.Status.ShouldBe(PaymentInfoQueryStatus.Success);
        var payment = result.Payments!.Single();
        payment.OrderId.ShouldBe("0000000001");
        payment.Id.ShouldBe("11");
        payment.Amount.ShouldBe(100m);
        payment.Status.ShouldBe(PaymentStatus.Paid);
        payment.Type.ShouldBe(PscbPaymentType.Payment);
    }

    [Fact]
    public async Task SignsTheRequestBodyWithTheApiKey()
    {
        var handler = new RecordingHandler(_ => Success("[]"));

        _ = await CreateApi(handler).GetPaymentsAsync(From, From.AddDays(5));

        var request = handler.Requests.Single();
        var signed = Encoding.UTF8.GetBytes(request.Body).Concat(Encoding.UTF8.GetBytes(ApiKey)).ToArray();
        request.Signature.ShouldBe(Convert.ToHexString(SHA256.HashData(signed)));
    }

    [Fact]
    public async Task UsesTheDebugEndpointAndDebugKeyInDebugMode()
    {
        var handler = new RecordingHandler(_ => Success("[]"));

        _ = await CreateApi(handler, debug: true).GetPaymentsAsync(From, From.AddDays(5));

        var request = handler.Requests.Single();
        request.Url.ShouldBe($"{DebugApiEndpoint}/merchantApi/getPayments");
        var signed = Encoding.UTF8.GetBytes(request.Body).Concat(Encoding.UTF8.GetBytes(DebugApiKey)).ToArray();
        request.Signature.ShouldBe(Convert.ToHexString(SHA256.HashData(signed)));
    }

    [Fact]
    public async Task OmitsOptionalParametersThatWereNotSet()
    {
        var handler = new RecordingHandler(_ => Success("[]"));

        _ = await CreateApi(handler).GetPaymentsAsync(From, From.AddDays(5));

        var body = ParseBody(handler.Requests.Single().Body);
        body["selectMode"].ShouldBeNull();
        body["paymentType"].ShouldBeNull();
        body["requestFiscalData"]!.Value<bool>().ShouldBeFalse();
        body["requestClearingData"]!.Value<bool>().ShouldBeFalse();
    }

    [Fact]
    public async Task SendsOptionalParametersWithTheBankIdentifiers()
    {
        var handler = new RecordingHandler(_ => Success("[]"));

        _ = await CreateApi(handler).GetPaymentsAsync(
            From,
            From.AddDays(5),
            PaymentsSelectMode.Created,
            PscbPaymentType.Invoice,
            requestFiscalData: true,
            requestClearingData: true);

        var body = ParseBody(handler.Requests.Single().Body);
        body["selectMode"]!.Value<string>().ShouldBe("created");
        body["paymentType"]!.Value<string>().ShouldBe("invoice");
        body["requestFiscalData"]!.Value<bool>().ShouldBeTrue();
        body["requestClearingData"]!.Value<bool>().ShouldBeTrue();
    }

    [Fact]
    public async Task RequestsLongPeriodInChunksAndMergesThem()
    {
        var handler = new RecordingHandler(call => Success(call == 0
            ? """[{"orderId":"0000000001","paymentId":"11"}]"""
            : """[{"orderId":"0000000002","paymentId":"12"}]"""));

        var result = await CreateApi(handler).GetPaymentsAsync(From, From.AddDays(40));

        handler.Requests.Count.ShouldBe(2);
        var first = ParseBody(handler.Requests[0].Body);
        var second = ParseBody(handler.Requests[1].Body);
        second["dateFrom"]!.Value<string>().ShouldBe(first["dateTo"]!.Value<string>());
        second["dateTo"]!.Value<string>().ShouldBe("2026-02-10T00:00:00+03:00");

        result.Payments!.Select(p => p.OrderId).ShouldBe(["0000000001", "0000000002"]);
    }

    /// <summary>
    /// Чанки не пересекаются, но статус платежа между двумя запросами может измениться, и один
    /// и тот же платёж способен попасть в оба ответа. В сверке дубль дороже защиты от него.
    /// </summary>
    [Fact]
    public async Task DropsDuplicatesAcrossChunks()
    {
        var handler = new RecordingHandler(_ => Success("""[{"orderId":"0000000001","paymentId":"11"}]"""));

        var result = await CreateApi(handler).GetPaymentsAsync(From, From.AddDays(40));

        handler.Requests.Count.ShouldBe(2);
        result.Payments!.Count.ShouldBe(1);
    }

    /// <summary>
    /// Частичный список в инструменте для сверки хуже ошибки: его примут за полный.
    /// </summary>
    [Fact]
    public async Task FailedChunkFailsTheWholeRequest()
    {
        var handler = new RecordingHandler(call => call == 0
            ? Success("""[{"orderId":"0000000001","paymentId":"11"}]""")
            : """{"status":"STATUS_FAILURE","errorCode":"SERVER_ERROR","errorDescription":"oops","requestId":"TEST-2"}""");

        var result = await CreateApi(handler).GetPaymentsAsync(From, From.AddDays(40));

        handler.Requests.Count.ShouldBe(2);
        result.Status.ShouldBe(PaymentInfoQueryStatus.Failure);
        result.ErrorCode.ShouldBe(ApiErrorCode.ServerError);
        result.ErrorDescription.ShouldBe("oops");
        result.RequestId.ShouldBe("TEST-2");
        result.Payments.ShouldBeNull();
    }

    [Fact]
    public async Task NonSuccessHttpStatusThrows()
    {
        var handler = new RecordingHandler(_ => null);

        await Should.ThrowAsync<PscbApiRequestException<PaymentsListQueryParams>>(
            () => CreateApi(handler).GetPaymentsAsync(From, From.AddDays(5)));
    }

    [Fact]
    public async Task RejectsAnEmptyPeriodBeforeCallingTheBank()
    {
        var handler = new RecordingHandler(_ => Success("[]"));

        await Should.ThrowAsync<ArgumentOutOfRangeException>(
            () => CreateApi(handler).GetPaymentsAsync(From, From));

        handler.Requests.ShouldBeEmpty();
    }

    /// <summary>
    /// Разбирает тело запроса, не превращая даты в <see cref="DateTime"/>: assertions видят ровно
    /// тот текст, который уходит банку, поэтому заодно фиксируют формат даты на проводе.
    /// </summary>
    private static JObject ParseBody(string json)
    {
        using var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None };
        return JObject.Load(reader);
    }

    private static string Success(string paymentsJson)
        => $$"""{"status":"STATUS_SUCCESS","requestId":"TEST-1","payments":{{paymentsJson}}}""";

    private static BankApi CreateApi(HttpMessageHandler handler, bool debug = false)
        => new(
            new StubHttpClientFactory(handler),
            new ApiConfiguration
            {
                Debug = debug,
                ApiEndpoint = ApiEndpoint,
                ApiDebugEndpoint = DebugApiEndpoint,
                MerchantId = MerchantId,
                ApiKey = ApiKey,
                ApiDebugKey = DebugApiKey,
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
            string? signature = request.Headers.TryGetValues("Signature", out var values) ? string.Join(',', values) : null;
            Requests.Add(new RecordedRequest(request.RequestUri!.ToString(), body, signature));

            var payload = respond(calls++);
            return payload is null
                ? new HttpResponseMessage(HttpStatusCode.InternalServerError)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(payload, Encoding.UTF8, "application/json") };
        }
    }

    private sealed record RecordedRequest(string Url, string Body, string? Signature);
}
