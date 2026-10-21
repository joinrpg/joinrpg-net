using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Шлюз к API ПСБ для админки сайта (ADR025).
/// </summary>
/// <remarks>
/// <para>
/// Отдаёт то, что сказал банк, и ничего не примешивает из данных JoinRpg. Права проверяет
/// контроллер: шлюз показывает платежи магазина целиком, а не по проектам, поэтому доступ
/// к конкретному проекту здесь не при чём.
/// </para>
/// <para>
/// <c>BankApi</c> не зарегистрирован в DI и создаётся на вызов — так же, как в
/// <c>PaymentsService</c>. Адреса возврата из <c>ApiConfiguration</c> не заполняются: они нужны
/// только для создания платежа, а шлюз платежи не создаёт.
/// </para>
/// </remarks>
internal class PscbPaymentsViewService(
    IBankSecretsProvider bankSecrets,
    IHttpClientFactory httpClientFactory,
    ILogger<PscbPaymentsViewService> logger) : IPscbPaymentsClient
{
    public async Task<PscbPaymentsListResult> GetPayments(PscbPaymentsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Период проверяется до первого обращения к банку: иначе админ увидел бы ошибку банка
        // про неверные аргументы вместо понятной.
        var requestCount = CountRequests(query);

        try
        {
            var info = await Api.GetPaymentsAsync(
                query.DateFrom,
                query.DateTo,
                ToSelectMode(query.SelectMode),
                ToPaymentType(query.PaymentType),
                query.RequestFiscalData);

            return PscbPaymentViewModelBuilder.BuildList(info, requestCount);
        }
        catch (PscbApiExceptionBase ex)
        {
            logger.LogError(ex, "Request to the bank for the payments list has failed");
            return PscbPaymentViewModelBuilder.ListFailure(Describe(ex), requestCount);
        }
    }

    public async Task<PscbPaymentDetailsResult> GetPaymentInfo(PscbPaymentDetailsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query.OrderId))
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Не указан orderId платежа");
        }

        var orderId = query.OrderId.Trim();
        try
        {
            var info = await Api.GetPaymentInfoAsync(
                orderId,
                query.RequestCardData,
                query.RequestFiscalData,
                query.RequestClearingData);

            return PscbPaymentViewModelBuilder.BuildDetails(info);
        }
        catch (PscbApiExceptionBase ex)
        {
            logger.LogError(ex, "Request to the bank for the payment {orderId} has failed", orderId);
            return PscbPaymentViewModelBuilder.DetailsFailure(Describe(ex));
        }
    }

    /// <summary>
    /// Сколько обращений к банку займёт период: банк принимает не больше месяца за запрос,
    /// поэтому период режется на чанки.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Период пустой или длиннее допустимого.</exception>
    private static int CountRequests(PscbPaymentsQuery query)
    {
        try
        {
            return PaymentsPeriodSplitter.Split(query.DateFrom, query.DateTo).Count;
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new ArgumentOutOfRangeException(nameof(query),
                $"Период должен быть непустым и не длиннее {(int)PaymentsPeriodSplitter.MaxPeriodLength.TotalDays} дней");
        }
    }

    private BankApi Api => new(
        httpClientFactory,
        new ApiConfiguration
        {
            Debug = bankSecrets.Debug,
            DebugOutput = bankSecrets.DebugOutput,
            ApiEndpoint = bankSecrets.ApiEndpoint,
            ApiDebugEndpoint = bankSecrets.ApiDebugEndpoint,
            MerchantId = bankSecrets.MerchantId,
            ApiKey = bankSecrets.ApiKey,
            ApiDebugKey = bankSecrets.ApiDebugKey,
        },
        logger);

    private static string Describe(PscbApiExceptionBase exception)
        => $"Запрос к банку не прошёл: {exception.Message}";

    private static PaymentsSelectMode? ToSelectMode(PscbPaymentsSelectMode? selectMode) => selectMode switch
    {
        PscbPaymentsSelectMode.Paid => PaymentsSelectMode.Paid,
        PscbPaymentsSelectMode.Created => PaymentsSelectMode.Created,
        null => null,
        _ => throw new ArgumentOutOfRangeException(nameof(selectMode), selectMode, null),
    };

    private static PscbPaymentType? ToPaymentType(PscbPaymentKind? paymentType) => paymentType switch
    {
        PscbPaymentKind.Payment => PscbPaymentType.Payment,
        PscbPaymentKind.Invoice => PscbPaymentType.Invoice,
        null => null,
        _ => throw new ArgumentOutOfRangeException(nameof(paymentType), paymentType, null),
    };
}
