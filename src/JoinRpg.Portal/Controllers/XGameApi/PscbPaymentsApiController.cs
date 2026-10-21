using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.XApi;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.WebPortal.Managers.AdminTools;
using JoinRpg.XGameApi.Contract.Pscb;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

/// <summary>
/// Шлюз к API ПСКБ для внешних потребителей (ADR025). Только для админа сайта.
/// </summary>
/// <remarks>
/// Отдаёт то, что сказал банк, своими типами и ничего не примешивает из данных JoinRpg.
/// Поля, которых нет в контракте, в ответ не попадают: состав ответа объявляет банк, а не мы,
/// и добавление поля — это правка контракта. Та же логика доступна админке по куке на
/// <c>/webapi/PscbPayments</c>.
/// </remarks>
[Route("x-api/pscb/payments")]
[XAdminAuthorize]
public class PscbPaymentsApiController(IPscbPaymentsClient client, IExportDataService exportDataService) : XGameApiController
{
    /// <summary>
    /// Список платежей магазина за период. Банк принимает не больше месяца за запрос, поэтому
    /// период запрашивается частями и склеивается.
    /// </summary>
    /// <param name="dateFrom">Нижняя граница периода, включительно.</param>
    /// <param name="dateTo">Верхняя граница периода, исключительно.</param>
    /// <param name="selectMode">Отбор по статусу. Если не задан, банк берёт только завершённые платежи.</param>
    /// <param name="paymentType">Отбор по виду записи. Если не задан, банк возвращает платежи и счета вместе.</param>
    /// <param name="requestFiscalData">Запрашивать ли фискальные чеки.</param>
    /// <param name="format">Формат ответа: <c>csv</c> отдаёт файл, всё остальное — JSON.</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<IActionResult> GetPayments(
        DateTimeOffset dateFrom,
        DateTimeOffset dateTo,
        PscbPaymentsSelectMode? selectMode,
        PscbPaymentKind? paymentType,
        bool requestFiscalData,
        string? format)
    {
        PscbPaymentsListResult result;
        try
        {
            result = await client.GetPayments(new PscbPaymentsQuery(dateFrom, dateTo, selectMode, paymentType, requestFiscalData));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }

        if (string.Equals(format, PscbPaymentsRoutes.CsvFormat, StringComparison.OrdinalIgnoreCase))
        {
            return GeneratorResultHelper.Result(
                $"Платежи ПСКБ {dateFrom:yyyy-MM-dd}-{dateTo:yyyy-MM-dd}",
                PscbPaymentsCsvExporter.CreateGenerator(exportDataService, result.Payments));
        }

        return Ok(result);
    }

    /// <summary>
    /// Все известные банку параметры одного платежа.
    /// </summary>
    /// <param name="orderId">Идентификатор платежа на нашей стороне.</param>
    /// <param name="requestCardData">Запрашивать ли расширенные данные банковской карты.</param>
    /// <param name="requestFiscalData">Запрашивать ли фискальные чеки.</param>
    /// <param name="requestClearingData">Запрашивать ли данные для сверки — номер реестра и его дату.</param>
    [HttpGet("{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<PscbPaymentDetailsResult>> GetPaymentInfo(
        string orderId,
        bool requestCardData,
        bool requestFiscalData,
        bool requestClearingData)
    {
        try
        {
            return await client.GetPaymentInfo(new PscbPaymentDetailsQuery(orderId, requestCardData, requestFiscalData, requestClearingData));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Problem(title: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
