using System.Globalization;

namespace JoinRpg.Web.AdminTools.PscbPayments;

/// <summary>
/// Адреса ручек шлюза к API ПСКБ и строка запроса к ним (ADR025).
/// </summary>
/// <remarks>
/// Адрес нужен двум потребителям: WASM-клиенту, который ходит за данными, и острову, который
/// строит ссылку на выгрузку CSV. <c>*UriLocator</c> здесь не нужен — он применяется, когда один
/// адрес формируют и серверная разметка, и браузер, а здесь серверная разметка его не строит.
/// Пути от корня, потому что в браузере относительный адрес разрешился бы от <c>/admin/pscb-payments</c>.
/// </remarks>
public static class PscbPaymentsRoutes
{
    /// <summary>Список платежей. <c>format=csv</c> отдаёт файл вместо JSON.</summary>
    public const string GetPayments = "/webapi/PscbPayments/GetPayments";

    /// <summary>Параметры одного платежа.</summary>
    public const string GetPaymentInfo = "/webapi/PscbPayments/GetPaymentInfo";

    /// <summary>Восстановление потерянных платежей (ADR027). POST, тело — параметры периода.</summary>
    public const string Restore = "/webapi/PscbPayments/Restore";

    /// <summary>Значение <c>format</c> для выгрузки списка платежей файлом.</summary>
    public const string CsvFormat = "csv";

    /// <summary>
    /// Адрес запроса списка платежей вместе со строкой запроса.
    /// </summary>
    /// <param name="query">Параметры выборки.</param>
    /// <param name="format">Формат ответа; если не задан, ручка отвечает JSON.</param>
    public static string GetPaymentsUri(PscbPaymentsQuery query, string? format = null)
    {
        ArgumentNullException.ThrowIfNull(query);

        var parameters = new List<string>
        {
            $"dateFrom={Date(query.DateFrom)}",
            $"dateTo={Date(query.DateTo)}",
        };
        if (query.SelectMode is { } selectMode)
        {
            parameters.Add($"selectMode={selectMode}");
        }
        if (query.PaymentType is { } paymentType)
        {
            parameters.Add($"paymentType={paymentType}");
        }
        parameters.Add($"requestFiscalData={Flag(query.RequestFiscalData)}");
        if (format is not null)
        {
            parameters.Add($"format={Uri.EscapeDataString(format)}");
        }
        return GetPayments + "?" + string.Join('&', parameters);
    }

    /// <summary>
    /// Адрес запроса параметров платежа вместе со строкой запроса.
    /// </summary>
    public static string GetPaymentInfoUri(PscbPaymentDetailsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        return GetPaymentInfo
            + $"?orderId={Uri.EscapeDataString(query.OrderId)}"
            + $"&requestCardData={Flag(query.RequestCardData)}"
            + $"&requestFiscalData={Flag(query.RequestFiscalData)}"
            + $"&requestClearingData={Flag(query.RequestClearingData)}";
    }

    private static string Date(DateTimeOffset value)
        => Uri.EscapeDataString(value.ToString("O", CultureInfo.InvariantCulture));

    private static string Flag(bool value) => value ? "true" : "false";
}
