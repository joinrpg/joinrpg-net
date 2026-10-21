using System.Globalization;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Шлюз к API ПСКБ для админки сайта (ADR025).
/// </summary>
/// <remarks>
/// <para>
/// Данные платежа берутся у банка как есть, а ссылки — наши: <c>account</c> это id пользователя
/// в нашей базе, а название проекта извлекается из назначения платежа, которое JoinRpg формирует
/// сам. Ссылки строятся без проверки, что пользователь или проект существуют.
/// </para>
/// <para>
/// Права проверяет контроллер: шлюз показывает платежи магазина целиком, а не по проектам,
/// поэтому доступ к конкретному проекту здесь не при чём.
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
    IUriService uriService,
    ISearchService searchService,
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

            var result = PscbPaymentViewModelBuilder.BuildList(info, requestCount);
            if (result.IsSuccess)
            {
                await FillLinksAsync(result.Payments);
            }
            return result;
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
    /// Проставляет ссылки на пользователя и проект.
    /// </summary>
    /// <remarks>
    /// Ссылки — дополнение к данным банка, а не их часть, поэтому их поломка не должна уносить
    /// уже полученный список: ошибка логируется, ссылки остаются пустыми.
    /// </remarks>
    private async Task FillLinksAsync(IReadOnlyCollection<PscbPaymentListRow> rows)
    {
        try
        {
            FillUserLinks(rows);
            await FillProjectLinksAsync(rows);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to build user or project links for the payments list");
        }
    }

    private void FillUserLinks(IEnumerable<PscbPaymentListRow> rows)
    {
        foreach (var row in rows)
        {
            if (int.TryParse(row.UserId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
            {
                row.UserUrl = uriService.Get(new UserLinkable(userId));
            }
        }
    }

    private async Task FillProjectLinksAsync(IReadOnlyCollection<PscbPaymentListRow> rows)
    {
        // Поиск идёт по названию, а названий в выгрузке за месяц единицы при сотнях платежей,
        // поэтому запрос к базе один на distinct-название, а не один на платёж.
        var urls = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var name in rows
                     .Select(row => row.ProjectName)
                     .OfType<string>()
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.Ordinal))
        {
            urls[name] = await FindProjectUrlAsync(name);
        }

        foreach (var row in rows)
        {
            if (row.ProjectName is { } name && urls.TryGetValue(name, out var url))
            {
                row.ProjectUrl = url;
            }
        }
    }

    /// <summary>
    /// Ссылка на проект с точно таким названием.
    /// </summary>
    /// <remarks>
    /// Поиск по проектам ищет вхождением подстроки и не фильтрует по правам, поэтому совпадение
    /// сверяется по полному имени. Ссылка ставится только при однозначном совпадении: при
    /// нескольких проектах с одним именем она вела бы на случайный из них. Названия короче трёх
    /// символов поиск не находит вовсе — это его собственное ограничение.
    /// </remarks>
    private async Task<string?> FindProjectUrlAsync(string projectName)
    {
        var found = await searchService.SearchAsync(currentUserId: null, projectName, [LinkType.Project]);
        var matches = found
            .Where(result => result.ProjectId is not null && string.Equals(result.Name, projectName, StringComparison.Ordinal))
            .ToList();

        return matches.Count == 1 ? uriService.Get(matches[0]) : null;
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

    /// <summary>
    /// Ссылка на профиль пользователя для <see cref="IUriService"/>.
    /// </summary>
    private sealed record UserLinkable(int UserId) : ILinkable
    {
        public LinkType LinkType => LinkType.ResultUser;

        public string? Identification => UserId.ToString(CultureInfo.InvariantCulture);

        public int? ProjectId => null;
    }
}
