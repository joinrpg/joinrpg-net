using System.Globalization;
using JoinRpg.Data.Interfaces;
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
/// сам. И пользователь, и проект ищутся в базе: строка, для которой ничего не нашлось, остаётся
/// без ссылки, а то, что не нашлось, отдаётся отдельными списками — по ним админ видит, какие
/// платежи не удалось атрибутировать.
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
    IUserRepository userRepository,
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
                await FillLinksAsync(result);
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
    /// Проставляет ссылки на пользователя и проект и собирает то, что не нашлось.
    /// </summary>
    /// <remarks>
    /// Ссылки — дополнение к данным банка, а не их часть, поэтому их поломка не должна уносить
    /// уже полученный список: ошибка логируется, ссылки и списки остаются пустыми.
    /// </remarks>
    private async Task FillLinksAsync(PscbPaymentsListResult result)
    {
        try
        {
            await FillUserLinksAsync(result);
            await FillProjectLinksAsync(result);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to build user or project links for the payments list");
        }
    }

    /// <summary>
    /// Ссылка на плательщика: пользователь ищется в базе по id из платежа, а если такого id нет —
    /// по email из того же платежа.
    /// </summary>
    /// <remarks>
    /// Банк возвращает то, что ему передал магазин, поэтому id может вести в никуда: пользователь
    /// удалён или объединён, а платёж создан не через JoinRpg. Найденный по email пользователь
    /// может иметь другой id, и ссылка ведёт на него — база первоисточник, а не значение из платежа.
    /// </remarks>
    private async Task FillUserLinksAsync(PscbPaymentsListResult result)
    {
        var existingIds = await LoadExistingUserIdsAsync(result.Payments);
        var idsByEmail = await FindUsersByEmailAsync(result.Payments, existingIds);
        var unresolved = new Dictionary<string, PscbUnresolvedUser>(StringComparer.Ordinal);

        foreach (var row in result.Payments)
        {
            if (ResolveUserId(row, existingIds, idsByEmail) is { } userId)
            {
                row.UserUrl = uriService.Get(new UserLinkable(userId));
                continue;
            }

            // Нечего ни искать, ни показывать: строка не называет плательщика никак.
            if (string.IsNullOrWhiteSpace(row.UserId) && string.IsNullOrWhiteSpace(row.UserEmail))
            {
                continue;
            }

            var key = $"{row.UserId}|{row.UserEmail}";
            if (unresolved.TryGetValue(key, out var entry))
            {
                entry.PaymentCount++;
            }
            else
            {
                unresolved[key] = new PscbUnresolvedUser { UserId = row.UserId, Email = row.UserEmail, PaymentCount = 1 };
            }
        }

        // id плательщика — число, поэтому и порядок числовой: как текст «1181» встал бы раньше «126».
        // Значения, которые числом не являются, идут в конце, а email разбирает совпадения.
        result.UnresolvedUsers = [.. unresolved.Values
            .OrderBy(user => (NotANumber: ParseUserId(user.UserId) is null, Id: ParseUserId(user.UserId) ?? 0))
            .ThenBy(user => user.UserId, StringComparer.Ordinal)
            .ThenBy(user => user.Email, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Какие из id плательщиков есть в базе — одним запросом на весь список.
    /// </summary>
    private async Task<IReadOnlySet<int>> LoadExistingUserIdsAsync(IEnumerable<PscbPaymentListRow> rows)
    {
        var ids = rows
            .Select(row => ParseUserId(row.UserId))
            .OfType<int>()
            .Distinct()
            .Select(id => new UserIdentification(id))
            .ToList();

        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        HashSet<int> existing = [.. (await userRepository.GetUserInfoHeaders(ids)).Select(header => header.UserId.Value)];
        return existing;
    }

    /// <summary>
    /// Поиск по email — только для строк, где id не нашёлся, и по одному запросу на distinct-email.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, int>> FindUsersByEmailAsync(
        IEnumerable<PscbPaymentListRow> rows,
        IReadOnlySet<int> existingIds)
    {
        var found = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var email in rows
                     .Where(row => ParseUserId(row.UserId) is not { } id || !existingIds.Contains(id))
                     .Select(row => row.UserEmail)
                     .OfType<string>()
                     .Where(email => !string.IsNullOrWhiteSpace(email))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (await userRepository.FindByEmail(email) is { } userId)
            {
                found[email] = userId.Value;
            }
        }

        return found;
    }

    private static int? ResolveUserId(
        PscbPaymentListRow row,
        IReadOnlySet<int> existingIds,
        IReadOnlyDictionary<string, int> idsByEmail)
    {
        if (ParseUserId(row.UserId) is { } id && existingIds.Contains(id))
        {
            return id;
        }

        return row.UserEmail is { } email && idsByEmail.TryGetValue(email, out var found) ? found : null;
    }

    private static int? ParseUserId(string? account)
        => int.TryParse(account, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId) ? userId : null;

    private async Task FillProjectLinksAsync(PscbPaymentsListResult result)
    {
        // Поиск идёт по названию, а названий в выгрузке за месяц единицы при сотнях платежей,
        // поэтому запрос к базе один на distinct-название, а не один на платёж.
        var urls = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var name in DistinctProjectNames(result.Payments))
        {
            urls[name] = await FindProjectUrlAsync(name);
        }

        var unresolved = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in result.Payments)
        {
            if (row.ProjectName is not { } name || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (urls.TryGetValue(name, out var url) && url is not null)
            {
                row.ProjectUrl = url;
                continue;
            }

            unresolved[name] = unresolved.GetValueOrDefault(name) + 1;
        }

        result.UnresolvedProjects = [.. unresolved
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => new PscbUnresolvedProject { ProjectName = pair.Key, PaymentCount = pair.Value })];
    }

    private static IEnumerable<string> DistinctProjectNames(IEnumerable<PscbPaymentListRow> rows)
        => rows
            .Select(row => row.ProjectName)
            .OfType<string>()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal);

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
