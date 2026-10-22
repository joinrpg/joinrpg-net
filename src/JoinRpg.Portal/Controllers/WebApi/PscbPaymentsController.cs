using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.WebPortal.Managers.AdminTools;
using JoinRpg.XGameApi.Contract.Pscb;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Шлюз к API ПСКБ для админки сайта (ADR025). Отдаёт то, что сказал банк, без данных JoinRpg.
/// </summary>
/// <remarks>
/// Та же логика доступна внешним потребителям по JWT на <c>x-api/pscb/payments</c>
/// (<see cref="XGameApi.PscbPaymentsApiController"/>): остров Blazor ходит с кукой, а
/// <c>[XAdminAuthorize]</c> жёстко задаёт схему JWT, поэтому одной ручкой обойтись нельзя.
/// </remarks>
[ApiController]
[Route("/webapi/PscbPayments/[action]")]
[AdminAuthorize]
public class PscbPaymentsController(IPscbPaymentsClient client, IExportDataService exportDataService) : ControllerBase
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
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
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
            if (!result.IsSuccess)
            {
                // В CSV отказ банка положить некуда, а файл с одним заголовком админ прочитал бы
                // как «платежей за период нет».
                return Problem(
                    title: PscbBankFailureHelper.PaymentsListTitle,
                    detail: PscbBankFailureHelper.Describe(result),
                    statusCode: StatusCodes.Status502BadGateway);
            }

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
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
