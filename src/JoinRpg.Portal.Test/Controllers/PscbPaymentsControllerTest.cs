using JoinRpg.Portal.Controllers.WebApi;
using JoinRpg.Portal.Controllers.XGameApi;
using JoinRpg.Portal.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;
using Microsoft.AspNetCore.Http;
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
        var controller = new PscbPaymentsController(new FakeClient(Refused()), new FakeExportDataService());

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
        var controller = new PscbPaymentsController(new FakeClient(Succeeded()), new FakeExportDataService());

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
        var controller = new PscbPaymentsController(new FakeClient(Refused()), new FakeExportDataService());

        var result = await controller.GetPayments(From, To, null, null, requestFiscalData: false, format: null);

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<PscbPaymentsListResult>().IsSuccess.ShouldBeFalse();
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
