using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.Web.AdminTools.PscbPayments;

/// <summary>
/// Шлюз к API ПСБ: список платежей магазина и параметры одного платежа (ADR025).
/// </summary>
/// <remarks>
/// Шлюз отдаёт то, что сказал банк, и ничего не примешивает из данных JoinRpg.
/// Контракт один на две поверхности: остров админки и внешний <c>x-api</c> отдают одни и те же типы.
/// </remarks>
public interface IPscbPaymentsClient
{
    /// <summary>
    /// Список платежей за период. Банк принимает не больше месяца за запрос, поэтому период
    /// режется на чанки на сервере.
    /// </summary>
    Task<PscbPaymentsListResult> GetPayments(PscbPaymentsQuery query);

    /// <summary>
    /// Все известные банку параметры одного платежа.
    /// </summary>
    Task<PscbPaymentDetailsResult> GetPaymentInfo(PscbPaymentDetailsQuery query);
}

/// <summary>
/// Запрос списка платежей.
/// </summary>
/// <param name="DateFrom">Нижняя граница периода, включительно.</param>
/// <param name="DateTo">Верхняя граница периода, исключительно.</param>
/// <param name="SelectMode">Отбор по статусу. Если не задан, банк берёт только завершённые платежи.</param>
/// <param name="PaymentType">Отбор по виду записи. Если не задан, банк возвращает платежи и счета вместе.</param>
/// <param name="RequestFiscalData">Запрашивать ли фискальные чеки.</param>
public record PscbPaymentsQuery(
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    PscbPaymentsSelectMode? SelectMode,
    PscbPaymentKind? PaymentType,
    bool RequestFiscalData);

/// <summary>
/// Запрос параметров платежа.
/// </summary>
/// <param name="OrderId">Идентификатор платежа на нашей стороне.</param>
/// <param name="RequestCardData">Запрашивать ли расширенные данные банковской карты.</param>
/// <param name="RequestFiscalData">Запрашивать ли фискальные чеки.</param>
/// <param name="RequestClearingData">Запрашивать ли данные для сверки — номер реестра и его дату.</param>
public record PscbPaymentDetailsQuery(
    string OrderId,
    bool RequestCardData,
    bool RequestFiscalData,
    bool RequestClearingData);
