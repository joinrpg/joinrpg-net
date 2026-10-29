using JoinRpg.DomainTypes;
using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Portal.Controllers.XGameApi;
using JoinRpg.Portal.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.Controllers;

/// <summary>
/// Отказ банка в шлюзе к API ПСКБ (ADR025).
/// </summary>
public class PscbPaymentsControllerTest
{
    private const string RefusalText = "NOT_AUTHORIZED; Неверный ключ; requestId: TEST-1";

    private static readonly DateTimeOffset From = new(2026, 10, 1, 0, 0, 0, TimeSpan.FromHours(3));
    private static readonly DateTimeOffset To = new(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(3));

    /// <summary>
    /// Иначе админ скачал бы файл с одним заголовком и решил, что платежей за период нет.
    /// </summary>
    [Fact]
    public async Task CsvIsReplacedWithTheBankRefusal()
    {
        var controller = new PscbPaymentsController(new FakeClient(Refused()), new FakeExportDataService(), new FakeRestoreClient());

        var result = await controller.GetPayments(From, To, null, null, requestFiscalData: false, format: "csv");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Title.ShouldBe(PscbBankFailureHelper.PaymentsListTitle);
        details.Detail.ShouldBe(RefusalText);
    }

    /// <summary>
    /// Внешняя поверхность отвечает так же, как админская: реализация у них одна, но продублирована
    /// в двух контроллерах.
    /// </summary>
    [Fact]
    public async Task XApiCsvIsReplacedWithTheBankRefusal()
    {
        var controller = new PscbPaymentsApiController(new FakeClient(Refused()), new FakeExportDataService());

        var result = await controller.GetPayments(From, To, null, null, requestFiscalData: false, format: "csv");

        var problem = result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status502BadGateway);
        problem.Value.ShouldBeOfType<ProblemDetails>().Detail.ShouldBe(RefusalText);
    }

    [Fact]
    public async Task CsvIsReturnedWhenTheBankAnswered()
    {
        var controller = new PscbPaymentsController(new FakeClient(Succeeded()), new FakeExportDataService(), new FakeRestoreClient());

        var result = await controller.GetPayments(From, To, null, null, requestFiscalData: false, format: "csv");

        result.ShouldBeOfType<FileContentResult>().FileContents.ShouldBe(FakeGenerator.Content);
    }

    /// <summary>
    /// В JSON отказ банка остаётся в теле со статусом 200: остров показывает его как ошибку и не
    /// предлагает ссылку на CSV.
    /// </summary>
    [Fact]
    public async Task JsonKeepsTheRefusalInTheBody()
    {
        var controller = new PscbPaymentsController(new FakeClient(Refused()), new FakeExportDataService(), new FakeRestoreClient());

        var result = await controller.GetPayments(From, To, null, null, requestFiscalData: false, format: null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<PscbPaymentsListResult>().IsSuccess.ShouldBeFalse();
    }

    /// <summary>
    /// Восстановление строки отдаёт итог сервиса как есть и отправляет сервису тот же запрос,
    /// что прислал остров (ADR027).
    /// </summary>
    [Fact]
    public async Task RestoreRowReturnsTheServiceResult()
    {
        var restore = new FakeRestoreClient();
        var controller = new PscbPaymentsController(new FakeClient(Succeeded()), new FakeExportDataService(), restore);
        var request = new PscbRestoreRowRequest(new PscbPaymentsQuery(From, To, null, null, RequestFiscalData: false), "0000063617");

        var result = await controller.RestoreRow(request);

        result.Result.ShouldBeOfType<Ok<PscbRestoreRowResult>>().Value.ShouldBe(restore.Result);
        restore.Requests.ShouldBe([request]);
    }

    /// <summary>
    /// Отказ сервиса становится текстом в 400 — остров показывает его админу и обрывает прогон.
    /// </summary>
    [Fact]
    public async Task RestoreRowServiceRefusalBecomesBadRequest()
    {
        var restore = new FakeRestoreClient { Error = new JoinValidationException("Сверка списка с базой не удалась") };
        var controller = new PscbPaymentsController(new FakeClient(Succeeded()), new FakeExportDataService(), restore);

        var result = await controller.RestoreRow(new PscbRestoreRowRequest(new PscbPaymentsQuery(From, To, null, null, RequestFiscalData: false), "0000063617"));

        result.Result.ShouldBeOfType<BadRequest<string>>().Value.ShouldBe("Сверка списка с базой не удалась");
    }

    /// <summary>
    /// Нарушение ограничений периода — тоже отказ текстом: сервис кидает <see cref="ArgumentException"/>.
    /// </summary>
    [Fact]
    public async Task RestoreRowArgumentFailureBecomesBadRequest()
    {
        var restore = new FakeRestoreClient { Error = new ArgumentException("Период длиннее 730 дней") };
        var controller = new PscbPaymentsController(new FakeClient(Succeeded()), new FakeExportDataService(), restore);

        var result = await controller.RestoreRow(new PscbRestoreRowRequest(new PscbPaymentsQuery(From, To, null, null, RequestFiscalData: false), "0000063617"));

        result.Result.ShouldBeOfType<BadRequest<string>>().Value.ShouldBe("Период длиннее 730 дней");
    }

    private static PscbPaymentsListResult Refused() => new()
    {
        IsSuccess = false,
        ErrorCode = "NOT_AUTHORIZED",
        ErrorDescription = "Неверный ключ",
        RequestId = "TEST-1",
        RequestCount = 2,
    };

    private static PscbPaymentsListResult Succeeded() => new()
    {
        IsSuccess = true,
        RequestCount = 2,
    };

    private sealed class FakeClient(PscbPaymentsListResult result) : IPscbPaymentsClient
    {
        public Task<PscbPaymentsListResult> GetPayments(PscbPaymentsQuery query) => Task.FromResult(result);

        public Task<PscbPaymentDetailsResult> GetPaymentInfo(PscbPaymentDetailsQuery query)
            => throw new NotSupportedException();
    }

    private sealed class FakeRestoreClient : IPscbPaymentsRestoreClient
    {
        public PscbRestoreRowResult Result { get; } = new()
        {
            OrderId = "0000063617",
            Outcome = PscbRestoreRowOutcome.Restored,
            Message = "платёж восстановлен; возврат восстановлен",
            RestoredPaymentsCount = 1,
            RestoredRefundsCount = 1,
        };

        public Exception? Error { get; set; }

        public List<PscbRestoreRowRequest> Requests { get; } = [];

        public Task<PscbRestoreRowResult> RestoreRow(PscbRestoreRowRequest request)
        {
            Requests.Add(request);
            return Error is { } error ? throw error : Task.FromResult(Result);
        }
    }

    private sealed class FakeExportDataService : IExportDataService
    {
        public IExportGenerator GetGenerator<T>(ExportType type, IEnumerable<T> data, IGeneratorFrontend<T> frontend) where T : class
            => new FakeGenerator();
    }

    private sealed class FakeGenerator : IExportGenerator
    {
        public static readonly byte[] Content = [1, 2, 3];

        public byte[] Generate() => Content;

        public string ContentType => "text/csv";

        public string FileExtension => "csv";
    }
}
