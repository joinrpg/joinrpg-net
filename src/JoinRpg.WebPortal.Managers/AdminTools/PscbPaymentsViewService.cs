using System.Globalization;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;
using PscbApi;
using PscbApi.Models;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Шлюз к API ПСКБ для админки сайта (ADR025) и сверка его списка с нашей базой (ADR027).
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
/// Список ещё и сверяется с базой: <c>orderId</c> это <c>CommentId</c> финансовой операции,
/// поэтому строка либо находит операцию — и получает ссылку на неё и на её заявку, — либо платёж
/// потерян. Исход сверки проставляется в каждой строке.
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
    IUriLocator<FinanceOperationIdentification> financeOperationUri,
    IUriLocator<ClaimIdentification> claimUri,
    ISearchService searchService,
    IUserRepository userRepository,
    IFinanceOperationsRepository financeOperationsRepository,
    IClaimsRepository claimsRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ILogger<PscbPaymentsViewService> logger) : IPscbPaymentsClient
{
    public async Task<PscbPaymentsListResult> GetPayments(PscbPaymentsQuery query)
        => (await GetPaymentsWithRestorePlan(query)).Result;

    /// <summary>
    /// Тот же список, что и <see cref="GetPayments"/>, плюс план восстановления каждой строки,
    /// которой вердикт разрешил автоматическое восстановление (ADR027).
    /// </summary>
    /// <remarks>
    /// Метод для сервиса восстановления из той же сборки: строка списка несёт только ссылки,
    /// а записи операции нужны номера заявки и типа оплаты, которые определяет вердикт.
    /// Скачивание и обогащение одни на оба потребления — список не ходит в банк дважды.
    /// </remarks>
    internal async Task<PscbPaymentsListWithRestorePlan> GetPaymentsWithRestorePlan(PscbPaymentsQuery query)
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
            var plans = result.IsSuccess
                ? await EnrichAsync(result)
                : NoPlans;
            return new PscbPaymentsListWithRestorePlan(result, plans);
        }
        catch (PscbApiExceptionBase ex)
        {
            logger.LogError(ex, "Request to the bank for the payments list has failed");
            return new PscbPaymentsListWithRestorePlan(
                PscbPaymentViewModelBuilder.ListFailure(Describe(ex), requestCount),
                NoPlans);
        }
    }

    private static readonly IReadOnlyDictionary<PscbPaymentListRow, RowRestorePlan> NoPlans
        = new Dictionary<PscbPaymentListRow, RowRestorePlan>();

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
    /// Добавляет к данным банка то, что знает JoinRpg: ссылки на пользователя и проект, списки
    /// тех, кого найти не удалось, возвраты, исход сверки каждой строки с нашей базой и вердикт
    /// восстановления. Возвращает планы восстановления строк, которым вердикт разрешил
    /// автоматическое восстановление.
    /// </summary>
    /// <remarks>
    /// Это дополнение к данным банка, а не их часть, поэтому его поломка не должна уносить уже
    /// полученный список: ошибка логируется, ссылки и списки остаются пустыми, а исход сверки —
    /// неизвестным (<c>MatchState</c> равен <c>null</c>, а не какому-нибудь из значений).
    /// Порядок значим: сверка идёт после возвратов, потому что «нет возврата» определяется по
    /// ссылке на операцию возврата, а вердикт восстановления — последним, потому что ему нужны
    /// и определённые плательщик с проектом, и исход сверки.
    /// </remarks>
    private async Task<IReadOnlyDictionary<PscbPaymentListRow, RowRestorePlan>> EnrichAsync(PscbPaymentsListResult result)
    {
        try
        {
            var resolution = new RowResolution();
            await FillUserLinksAsync(result, resolution);
            await FillProjectLinksAsync(result, resolution);
            await FillRefundsAsync(result.Payments);
            var operations = await FillReconciliationAsync(result.Payments);
            return await FillRestorePlanAsync(result.Payments, resolution, operations);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to enrich the payments list with the JoinRpg data");
            return NoPlans;
        }
    }

    /// <summary>
    /// Что удалось определить по строке списка: нужно и для ссылок, и для вердикта восстановления.
    /// </summary>
    /// <remarks>
    /// Ключ — сама строка. Строки не переопределяют равенство, поэтому сравнение идёт по ссылке:
    /// <c>orderId</c> уникальным ключом не является (банк переименовывает заброшенные попытки,
    /// и номер способен повториться), а плодить параллельные списки по индексу хрупко.
    /// </remarks>
    private sealed class RowResolution
    {
        public Dictionary<PscbPaymentListRow, UserIdentification> UserIds { get; } = [];

        public Dictionary<PscbPaymentListRow, ProjectIdentification> ProjectIds { get; } = [];
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
    private async Task FillUserLinksAsync(PscbPaymentsListResult result, RowResolution resolution)
    {
        var existingIds = await LoadExistingUserIdsAsync(result.Payments);
        var idsByEmail = await FindUsersByEmailAsync(result.Payments, existingIds);
        var unresolved = new Dictionary<string, PscbUnresolvedUser>(StringComparer.Ordinal);

        foreach (var row in result.Payments)
        {
            if (ResolveUserId(row, existingIds, idsByEmail) is { } userId)
            {
                row.UserUrl = uriService.Get(new UserLinkable(userId));
                resolution.UserIds[row] = new UserIdentification(userId);
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

    private async Task FillProjectLinksAsync(PscbPaymentsListResult result, RowResolution resolution)
    {
        // Поиск идёт по названию, а названий в выгрузке за месяц единицы при сотнях платежей,
        // поэтому запрос к базе один на distinct-название, а не один на платёж.
        var projects = new Dictionary<string, ResolvedProject?>(StringComparer.Ordinal);
        foreach (var name in DistinctProjectNames(result.Payments))
        {
            projects[name] = await FindProjectAsync(name);
        }

        var unresolved = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in result.Payments)
        {
            if (row.ProjectName is not { } name || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (projects.TryGetValue(name, out var project) && project is not null)
            {
                row.ProjectUrl = project.Url;
                resolution.ProjectIds[row] = new ProjectIdentification(project.ProjectId);
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
    /// Ссылка на операцию возврата в JoinRpg. О самом возврате сообщает банк — массивом
    /// <c>refunds[]</c> в элементе списка и статусом <c>ref</c>, — а наша операция возврата лежит
    /// в базе, и на неё полезно уметь перейти.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>orderId</c> это <c>CommentId</c> финансовой операции, дополненный нулями до десяти
    /// знаков, а операция возврата ссылается на возвращённую через <c>RefundedOperationId</c> —
    /// поэтому соединение идёт по числу и одним запросом на весь список.
    /// </para>
    /// <para>
    /// Значения вроде <c>__REPLACED_0000644272</c> числом не являются: так банк помечает
    /// заброшенную попытку, чей <c>orderId</c> магазин использовал повторно. Ссылки такая строка
    /// не получает — возврат относится к текущему платежу, а не к предыдущим попыткам.
    /// </para>
    /// </remarks>
    private async Task FillRefundsAsync(IReadOnlyCollection<PscbPaymentListRow> rows)
    {
        var operationIds = rows
            .Select(row => PscbPaymentViewModelBuilder.OperationIdFromOrderId(row.OrderId))
            .OfType<int>()
            .Distinct()
            .ToList();

        if (operationIds.Count == 0)
        {
            return;
        }

        var refunds = await financeOperationsRepository.GetApprovedRefunds(operationIds);
        if (refunds.Count == 0)
        {
            return;
        }

        // Возврат по операции один: повторный PaymentsService создать не даёт. Если их всё же
        // несколько, ссылка ведёт на последний — по дате операции.
        var byOperation = refunds
            .GroupBy(refund => refund.RefundedOperationId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(refund => refund.OperationDate).First());

        foreach (var row in rows)
        {
            if (PscbPaymentViewModelBuilder.OperationIdFromOrderId(row.OrderId) is { } operationId
                && byOperation.TryGetValue(operationId, out var refund))
            {
                row.RefundUrl = financeOperationUri.GetUri(refund.RefundId).AbsoluteUri;
            }
        }
    }

    /// <summary>
    /// Сверка каждой строки с нашей базой: операция по <c>orderId</c>, ссылки на неё и на её заявку
    /// и исход — проведён, потерян, не проведён (ADR027).
    /// </summary>
    /// <remarks>
    /// Найденная операция даёт и заявку, поэтому у строки с операцией ссылка на заявку есть сразу.
    /// Запрос к базе один на весь список и идёт по первичному ключу. Найденные операции
    /// возвращаются: вердикту восстановления нужен тип оплаты исходного платежа, чтобы вернуть
    /// деньги на тот же тип, а не на тот, что активен в проекте сегодня.
    /// </remarks>
    private async Task<IReadOnlyDictionary<int, MatchedOperation>> FillReconciliationAsync(IReadOnlyCollection<PscbPaymentListRow> rows)
    {
        var operationIds = rows
            .Select(row => PscbPaymentViewModelBuilder.OperationIdFromOrderId(row.OrderId))
            .OfType<int>()
            .Distinct()
            .ToList();

        Dictionary<int, MatchedOperation> found = [];
        if (operationIds.Count > 0)
        {
            foreach (var operation in await financeOperationsRepository.GetOperationsByCommentIds(operationIds))
            {
                found[operation.OperationId.FinanceOperationId] = operation;
            }
        }

        foreach (var row in rows)
        {
            var matched = PscbPaymentViewModelBuilder.OperationIdFromOrderId(row.OrderId) is { } operationId
                ? found.GetValueOrDefault(operationId)
                : null;

            var matchState = PscbPaymentViewModelBuilder.Classify(row, matched?.State, matched?.OperationType);
            row.MatchState = matchState;
            row.MatchStateText = PscbPaymentViewModelBuilder.MatchStateName(matchState);

            if (matched is null)
            {
                continue;
            }

            row.OperationUrl = financeOperationUri.GetUri(matched.OperationId).AbsoluteUri;
            row.ClaimUrl = claimUri.GetUri(matched.OperationId.ClaimId).AbsoluteUri;
        }

        return found;
    }

    /// <summary>
    /// Вердикт восстановления для строк, которые сверка сочла потерянными (ADR027): ссылка
    /// на заявку, куда платёж ляжет, причина, если восстановить автоматически нельзя, и план
    /// восстановления — заявка и тип оплаты — для строк без причины.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Правило заказчика: восстанавливать можно только то, что определилось однозначно. Платёж
    /// пишется на заявку с наименьшим номером среди всех заявок плательщика в проекте — закрытые
    /// и отклонённые подходят тоже, потому что потерянный платёж бывает и на них. Тип оплаты —
    /// единственный активный <c>Online</c> в проекте; если их ноль или несколько, восстанавливать
    /// некуда.
    /// </para>
    /// <para>
    /// Строка с пропущенным возвратом заявку и тип оплаты берёт у найденной операции платежа,
    /// поэтому плательщик и проект ей не нужны: деньги возвращаются туда, откуда пришли. Ей же
    /// план несёт дату денег исходного платежа — она идёт в текст комментария возврата.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyDictionary<PscbPaymentListRow, RowRestorePlan>> FillRestorePlanAsync(
        IReadOnlyCollection<PscbPaymentListRow> rows,
        RowResolution resolution,
        IReadOnlyDictionary<int, MatchedOperation> operations)
    {
        var candidates = rows
            .Where(row => row.MatchState is PscbPaymentMatchState.Lost or PscbPaymentMatchState.RefundMissing)
            .ToList();

        if (candidates.Count == 0)
        {
            return NoPlans;
        }

        var claims = await LoadFirstClaimIdsAsync(candidates, resolution);
        var projects = await LoadProjectInfosAsync(candidates, resolution, operations);

        Dictionary<PscbPaymentListRow, RowRestorePlan> plans = [];
        foreach (var row in candidates)
        {
            var matched = MatchedOperationOf(row, operations);
            row.RestoreBlockReason = BlockReason(row, matched, resolution, claims, projects);

            // Заявка строки с пропущенным возвратом уже проставлена сверкой — она у операции платежа.
            if (row.MatchState == PscbPaymentMatchState.Lost
                && resolution.ProjectIds.TryGetValue(row, out var projectId)
                && resolution.UserIds.TryGetValue(row, out var userId)
                && claims.TryGetValue((projectId, userId), out var lostClaimId))
            {
                row.ClaimUrl = claimUri.GetUri(lostClaimId).AbsoluteUri;

                // Причина отсутствует ровно тогда, когда тип оплаты выбран однозначно.
                if (row.RestoreBlockReason is null
                    && PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(OnlinePaymentTypesOf(projects.GetValueOrDefault(projectId))).PaymentTypeId is { } paymentTypeId)
                {
                    plans[row] = new RowRestorePlan(lostClaimId, paymentTypeId, RefundedOperationDate: null);
                }
            }
            else if (row.MatchState == PscbPaymentMatchState.RefundMissing
                && row.RestoreBlockReason is null
                && matched is { PaymentTypeId: { } refundedPaymentTypeId })
            {
                plans[row] = new RowRestorePlan(
                    matched.OperationId.ClaimId,
                    refundedPaymentTypeId,
                    matched.OperationDate);
            }
        }

        return plans;
    }

    /// <summary>
    /// По первой заявке на пару (проект, плательщик): заявок в любом статусе, выбирается наименьший
    /// номер. Один запрос на проект, а не на строку.
    /// </summary>
    private async Task<IReadOnlyDictionary<(ProjectIdentification ProjectId, UserIdentification UserId), ClaimIdentification>> LoadFirstClaimIdsAsync(
        IReadOnlyCollection<PscbPaymentListRow> candidates,
        RowResolution resolution)
    {
        var firstClaims = new Dictionary<(ProjectIdentification ProjectId, UserIdentification UserId), ClaimIdentification>();
        foreach (var group in ResolvedPairs(candidates, resolution).GroupBy(pair => pair.ProjectId))
        {
            var userIds = group.Select(pair => pair.UserId).Distinct().ToList();
            foreach (var claim in await claimsRepository.GetClaimIdsForPlayers(group.Key, userIds))
            {
                var key = (group.Key, claim.UserId);
                // Заявок у игрока в проекте бывает несколько; берём наименьший номер, как решил заказчик.
                if (!firstClaims.TryGetValue(key, out var existing) || claim.ClaimId.ClaimId < existing.ClaimId)
                {
                    firstClaims[key] = claim.ClaimId;
                }
            }
        }

        return firstClaims;
    }

    /// <summary>
    /// Метаданные проектов-кандидатов: блокировка (ADR023) и типы оплаты живут в
    /// <see cref="ProjectInfo"/>, поэтому готовность проекта принять платёж читается из них.
    /// Запрос — только когда есть что терять: полностью проведённому списку метаданные не нужны.
    /// </summary>
    private async Task<IReadOnlyDictionary<ProjectIdentification, ProjectInfo>> LoadProjectInfosAsync(
        IReadOnlyCollection<PscbPaymentListRow> candidates,
        RowResolution resolution,
        IReadOnlyDictionary<int, MatchedOperation> operations)
    {
        var projectIds = ResolvedPairs(candidates, resolution).Select(pair => pair.ProjectId).ToHashSet();
        foreach (var row in candidates)
        {
            // У строки с пропущенным возвратом проект определяется по операции платежа, а не по
            // названию из назначения: операция первоисточник.
            if (MatchedOperationOf(row, operations)?.OperationId.ProjectId is { } operationProjectId)
            {
                _ = projectIds.Add(operationProjectId);
            }
        }

        Dictionary<ProjectIdentification, ProjectInfo> projects = [];
        foreach (var projectId in projectIds)
        {
            projects[projectId] = await projectMetadataRepository.GetProjectMetadata(projectId);
        }

        return projects;
    }

    /// <summary>
    /// Онлайн-типы оплаты проекта из его метаданных. <c>null</c>, когда метаданных нет, —
    /// для вердикта это неотличимо от «типов нет», и причина отказа та же.
    /// </summary>
    private static IReadOnlyCollection<PaymentTypeInfo>? OnlinePaymentTypesOf(ProjectInfo? project)
        => project is null
            ? null
            : [.. project.ProjectFinanceSettings.PaymentTypes.Where(paymentType => paymentType.TypeKind == PaymentTypeKind.Online)];

    /// <summary>
    /// Причина, по которой строку нельзя восстановить автоматически, либо <c>null</c> — значит можно.
    /// Причины проверяются по порядку от самых общих: админу важнее понять, чего не хватает,
    /// чем увидеть все причины сразу.
    /// </summary>
    private static string? BlockReason(
        PscbPaymentListRow row,
        MatchedOperation? matched,
        RowResolution resolution,
        IReadOnlyDictionary<(ProjectIdentification ProjectId, UserIdentification UserId), ClaimIdentification> firstClaims,
        IReadOnlyDictionary<ProjectIdentification, ProjectInfo> projects)
    {
        if (PscbPaymentViewModelBuilder.RestoreBlockReasonFromBankData(row) is { } bankReason)
        {
            return bankReason;
        }

        // Возврат пишется туда же, откуда пришёл платёж: заявка и тип оплаты берутся у найденной
        // операции, и ни плательщик, ни название проекта для этого не нужны.
        if (row.MatchState == PscbPaymentMatchState.RefundMissing)
        {
            return matched switch
            {
                null => "операция платежа не найдена",
                { PaymentTypeId: null } => "у операции платежа нет типа оплаты",
                _ => IsBlocked(matched.OperationId.ProjectId, projects),
            };
        }

        if (!resolution.UserIds.TryGetValue(row, out var userId))
        {
            return "плательщик не определён";
        }

        if (!resolution.ProjectIds.TryGetValue(row, out var projectId))
        {
            return "проект не определён";
        }

        if (IsBlocked(projectId, projects) is { } blocked)
        {
            return blocked;
        }

        if (PscbPaymentViewModelBuilder.ChooseOnlinePaymentType(OnlinePaymentTypesOf(projects.GetValueOrDefault(projectId))).Reason is { } typeReason)
        {
            return typeReason;
        }

        return firstClaims.ContainsKey((projectId, userId)) ? null : "у плательщика нет заявки в проекте";
    }

    /// <remarks>
    /// Та же проверка, что в <see cref="ProjectInfo.EnsureNotBlocked"/>, но булевой формой:
    /// причина отказа — текст в строке списка, поэтому бросать исключение здесь нельзя.
    /// </remarks>
    private static string? IsBlocked(ProjectIdentification projectId, IReadOnlyDictionary<ProjectIdentification, ProjectInfo> projects)
        => projects.TryGetValue(projectId, out var project) && project.ProjectStatus == ProjectLifecycleStatus.Blocked
            ? "проект заблокирован, его восстанавливают инструментами DRP"
            : null;

    private static MatchedOperation? MatchedOperationOf(
        PscbPaymentListRow row,
        IReadOnlyDictionary<int, MatchedOperation> operations)
        => PscbPaymentViewModelBuilder.OperationIdFromOrderId(row.OrderId) is { } operationId
            ? operations.GetValueOrDefault(operationId)
            : null;

    /// <summary>
    /// Пары (проект, плательщик), которые удалось определить, — по ним идут запросы к базе.
    /// </summary>
    private static IEnumerable<(ProjectIdentification ProjectId, UserIdentification UserId)> ResolvedPairs(
        IReadOnlyCollection<PscbPaymentListRow> rows,
        RowResolution resolution)
        => rows
            .Where(row => resolution.ProjectIds.ContainsKey(row) && resolution.UserIds.ContainsKey(row))
            .Select(row => (resolution.ProjectIds[row], resolution.UserIds[row]))
            .Distinct();

    /// <summary>
    /// Проект с точно таким названием: и ссылка, и номер — номер нужен вердикту восстановления.
    /// </summary>
    /// <remarks>
    /// Поиск по проектам ищет вхождением подстроки и не фильтрует по правам, поэтому совпадение
    /// сверяется по полному имени. Проект определяется только при однозначном совпадении: при
    /// нескольких проектах с одним именем ссылка вела бы на случайный из них, а восстанавливать
    /// платёж в случайный проект нельзя. Названия короче трёх символов поиск не находит вовсе —
    /// это его собственное ограничение.
    /// </remarks>
    private async Task<ResolvedProject?> FindProjectAsync(string projectName)
    {
        var found = await searchService.SearchAsync(currentUserId: null, projectName, [LinkType.Project]);
        var matches = found
            .Where(result => result.ProjectId is not null && string.Equals(result.Name, projectName, StringComparison.Ordinal))
            .ToList();

        return matches.Count == 1 ? new ResolvedProject(matches[0].ProjectId!.Value, uriService.Get(matches[0])) : null;
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

    /// <summary>
    /// Проект, который удалось однозначно определить по названию из назначения платежа.
    /// </summary>
    private sealed record ResolvedProject(int ProjectId, string Url);
}
