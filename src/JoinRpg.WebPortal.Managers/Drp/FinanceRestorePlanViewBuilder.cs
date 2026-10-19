using System.Globalization;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Перекладка плана восстановления ручных оплат (#5631) в модель страницы и итог применения в тексты для мастера
/// (<see cref="FinanceRestoreViewService"/>). Без обращений к базе — чтобы проверять тестами.
/// </summary>
internal static class FinanceRestorePlanViewBuilder
{
    /// <summary>Пользователи, имена которых нужны для показа плана: авторы операций и решений.</summary>
    public static IReadOnlyCollection<UserIdentification> UsersToShow(IEnumerable<FinanceRestorePlanRow> plan)
        => [.. plan.SelectMany(r => r.Operations.Select(o => o.Initiator).Concat(r.Moderations.Select(m => m.Initiator))).Distinct()];

    /// <summary>Заявки плана, которые есть в базе, — для имён персонажа и игрока.</summary>
    public static IReadOnlyCollection<ClaimIdentification> ExistingClaims(IEnumerable<FinanceRestorePlanRow> plan)
        => [.. plan.Where(r => r.ClaimExists).Select(r => r.ClaimId)];

    /// <param name="currentUser">Мастер, который смотрит план: ему предлагается его тип «наличные».</param>
    public static FinanceRestorePlanViewModel Build(
        ProjectInfo projectInfo,
        UserIdentification currentUser,
        IReadOnlyList<FinanceRestorePlanRow> plan,
        IReadOnlyCollection<ClaimWithPlayer> claims,
        IReadOnlyCollection<UserInfoHeader> users)
    {
        var claimsById = claims.ToDictionary(c => c.ClaimId);
        var names = users.ToDictionary(u => u.UserId, u => u.DisplayName.DisplayName);
        string NameOf(UserIdentification id) => names.TryGetValue(id, out var name) ? name : id.Value.ToString(CultureInfo.InvariantCulture);

        // Свой тип «наличные» — в любом состоянии: CreateRestoredCashPaymentType вернёт и выключенный.
        var ownCash = FindOwnCash(projectInfo, currentUser);
        var rows = plan.Select(row =>
        {
            var claim = claimsById.GetValueOrDefault(row.ClaimId);
            var ambiguous = AmbiguousKeys(row);
            return new FinanceRestoreRowViewModel(
                row.ClaimId,
                row.ClaimExists,
                claim?.CharacterName,
                claim?.Player.DisplayName.DisplayName,
                [.. row.Operations.Select(o => ToOperationView(o, row.ClaimExists, ambiguous, NameOf(o.Initiator), currentUser, ownCash))],
                [.. row.Moderations.Select(m => ToModerationView(m, row, ambiguous, NameOf(m.Initiator), projectInfo.TimeZone))],
                row.AlreadyExisting,
                row.Conflicts.Count,
                row.Transfers,
                row.OnlinePayments,
                row.Unparsed,
                [.. row.Warnings.Select(ToWarningView)]);
        }).ToList();

        // Выключенный тип показывается, только если его предложили, — иначе предложение выпало бы из списка.
        // Свой «наличные» — всегда: создать его панель уже не предложит, и без него мастеру нечего выбрать.
        var offered = rows
            .SelectMany(r => r.Operations)
            .SelectMany(o => o.SuggestedPaymentTypeId is { } id ? o.CandidatePaymentTypeIds.Append(id) : o.CandidatePaymentTypeIds)
            .ToHashSet();
        if (ownCash is not null)
        {
            _ = offered.Add(ownCash.PaymentTypeId.PaymentTypeId);
        }
        var paymentTypes = projectInfo.ProjectFinanceSettings.PaymentTypes
            .Where(p => IsManual(p.TypeKind) && (p.Enabled || offered.Contains(p.PaymentTypeId.PaymentTypeId)))
            .Select(p => new FinanceRestorePaymentTypeView(p.PaymentTypeId.PaymentTypeId, p.Name, p.User.DisplayName.DisplayName, p.Enabled))
            .ToList();

        return new FinanceRestorePlanViewModel(projectInfo.ProjectId, UnavailableReason: null, paymentTypes, rows, HasOwnCash: ownCash is not null);
    }

    /// <summary>
    /// Наличный тип мастера, в том числе выключенный. Не <c>GetCashPaymentType</c>: тот падает, если таких типов
    /// несколько, а в старых данных это бывает; тогда предпочитается включённый — как в
    /// <c>ProjectFinanceSettingsService.FindCashPaymentType</c>.
    /// </summary>
    private static PaymentTypeInfo? FindOwnCash(ProjectInfo projectInfo, UserIdentification currentUser)
        => projectInfo.ProjectFinanceSettings.PaymentTypes
            .Where(p => p.User.UserId == currentUser && p.TypeKind == PaymentTypeKind.Cash)
            .OrderByDescending(p => p.Enabled)
            .FirstOrDefault();

    public static FinanceRestorePlanViewModel Unavailable(ProjectInfo projectInfo, ClaimRestoreUnavailableReason reason)
        => new(projectInfo.ProjectId, reason, [], [], HasOwnCash: false);

    /// <summary>Ручные операции бывают только на неонлайн-типы.</summary>
    public static bool IsManual(PaymentTypeKind kind) => kind is PaymentTypeKind.Cash or PaymentTypeKind.Custom;

    /// <summary>Неоднозначный ключ — разобрать вручную: выбор типа в панели хранится по ключу.</summary>
    internal const string AmbiguousKeyText = "Неоднозначный ключ — разберите вручную";

    /// <summary>
    /// Ключи, которые повторяются в строке плана. Такие строки не передаются в доменный сервис
    /// (<see cref="FinanceRestoreInputBuilder.BuildClaim"/>) и не применяются в панели.
    /// </summary>
    private static HashSet<string> AmbiguousKeys(FinanceRestorePlanRow row)
        => [.. row.Operations.Select(o => o.Key).Concat(row.Moderations.Select(m => m.Key))
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)];

    private static FinanceRestoreOperationView ToOperationView(
        FinanceOperationToRestore operation,
        bool claimExists,
        HashSet<string> ambiguousKeys,
        string authorName,
        UserIdentification currentUser,
        PaymentTypeInfo? ownCash)
    {
        var reason = operation.NotApplicableReason();
        var notApplicableText = ambiguousKeys.Contains(operation.Key)
            ? AmbiguousKeyText
            : reason is { } r ? NotApplicableText(r) : null;
        var suggestion = operation.PaymentType;
        // «Создать наличные» на текущего мастера: если они уже есть — это они и есть.
        var suggested = suggestion.PaymentTypeId
            ?? (suggestion.CreateCashFor == currentUser ? ownCash?.PaymentTypeId.PaymentTypeId : null);
        return new FinanceRestoreOperationView(
            operation.Key,
            operation.OperationId,
            IsRefund: operation.Type == FinanceOperationType.Refund,
            Money: Math.Abs(operation.Money ?? 0),
            operation.CreatedAt,
            authorName,
            operation.IsByPlayer,
            ToStateView(operation.State),
            suggested,
            [.. suggestion.Candidates],
            CanApply: claimExists && notApplicableText is null,
            NotApplicableReason: notApplicableText);
    }

    private static FinanceRestoreModerationView ToModerationView(
        FinanceModerationToRestore moderation,
        FinanceRestorePlanRow row,
        HashSet<string> ambiguousKeys,
        string authorName,
        TimeZoneInfo timeZone)
    {
        var target = Target(row, moderation);
        var reason = moderation.NotApplicableReason()
            ?? (ParentWillNotBeInserted(moderation, target, ambiguousKeys) ? FinanceRestoreSkipReason.ParentNotInserted : null);
        var notApplicableText = ambiguousKeys.Contains(moderation.Key)
            ? AmbiguousKeyText
            : reason is { } r ? NotApplicableText(r) : null;
        return new FinanceRestoreModerationView(
            moderation.Key,
            moderation.CommentId,
            moderation.Approved,
            moderation.CreatedAt,
            authorName,
            CanApply: row.ClaimExists && notApplicableText is null,
            notApplicableText ?? LinkNote(moderation, target, timeZone));
    }

    /// <summary>Операция из уведомлений, с которой связалось решение.</summary>
    private static FinanceOperationToRestore? Target(FinanceRestorePlanRow row, FinanceModerationToRestore moderation)
        => moderation.TargetOperationKey is { } key ? row.Operations.FirstOrDefault(o => o.Key == key) : null;

    /// <summary>
    /// Решение по операции, которая не вставится ни в каком прогоне: из-за конфликта или суммы
    /// (<see cref="FinanceModerationLink.NotRestoredOperation"/>), неприменимой (неподтверждённый возврат) или
    /// с неоднозначным ключом.
    /// </summary>
    private static bool ParentWillNotBeInserted(FinanceModerationToRestore moderation, FinanceOperationToRestore? target, HashSet<string> ambiguousKeys)
        => moderation.Link == FinanceModerationLink.NotRestoredOperation
            || (moderation.Link == FinanceModerationLink.RestoredOperation
                && (target is null || target.NotApplicableReason() is not null || ambiguousKeys.Contains(target.Key)));

    private static string? LinkNote(FinanceModerationToRestore moderation, FinanceOperationToRestore? target, TimeZoneInfo timeZone)
        => moderation.Link switch
        {
            FinanceModerationLink.RestoredOperation => target is null
                ? null
                : $"К операции «{Describe(target, timeZone)}» — восстановится вместе с ней",
            FinanceModerationLink.ExistingOperation => moderation.TargetOperationId is { } id
                ? $"К операции № {id}, которая уже есть в базе"
                : "К операции, которая уже есть в базе",
            FinanceModerationLink.Preferential => "К запросу льготного взноса — восстановится комментарием, льготу проверьте вручную",
            FinanceModerationLink.NotFound => "Операция не найдена — восстановится комментарием без привязки к операции",
            FinanceModerationLink.Ambiguous => "Непонятно, к какой операции, — восстановится комментарием без привязки, операции останутся ждать подтверждения",
            FinanceModerationLink.NotRestoredOperation => null,
            _ => throw new ArgumentOutOfRangeException(nameof(moderation), moderation.Link, null),
        };

    /// <summary>
    /// Что из того, что мастер мог восстановить, в этот раз не передано и почему. «Уже есть», конфликты и прочее
    /// неприменимое здесь не повторяется: это видно в плане.
    /// </summary>
    public static IReadOnlyList<string> Skipped(FinanceRestorePlanRow row, ClaimFinanceRestoreInput input, TimeZoneInfo timeZone)
    {
        var result = new List<string>();
        var ambiguous = AmbiguousKeys(row);
        foreach (var operation in row.Operations.Where(o => !ambiguous.Contains(o.Key)))
        {
            if (input.NotPassed.TryGetValue(operation.Key, out var reason) && IsWorthReporting(reason))
            {
                result.Add($"{Describe(operation, timeZone)}: {SkippedText(reason)}");
            }
        }
        // Решения, которые в плане и так показаны неприменимыми, не повторяем.
        foreach (var moderation in row.Moderations.Where(m => !ambiguous.Contains(m.Key)
            && !ParentWillNotBeInserted(m, Target(row, m), ambiguous)))
        {
            if (input.NotPassed.TryGetValue(moderation.Key, out var reason) && IsWorthReporting(reason))
            {
                result.Add($"{Describe(moderation, timeZone)}: {SkippedText(reason)}");
            }
        }
        return result;
    }

    private static bool IsWorthReporting(FinanceRestoreSkipReason reason)
        => reason is FinanceRestoreSkipReason.NoPaymentType
            or FinanceRestoreSkipReason.ParentNotRestoredInThisRun
            or FinanceRestoreSkipReason.ParentUnknown;

    /// <summary>«оплата 500 от 01.07.2026 13:00» — время в часовом поясе проекта.</summary>
    public static string Describe(FinanceOperationToRestore operation, TimeZoneInfo timeZone)
        => $"{(operation.Type == FinanceOperationType.Refund ? "возврат" : "оплата")} {Math.Abs(operation.Money ?? 0)} от {Format(operation.CreatedAt, timeZone)}";

    private static string Describe(FinanceModerationToRestore moderation, TimeZoneInfo timeZone)
        => $"{(moderation.Approved ? "подтверждение" : "отклонение")} от {Format(moderation.CreatedAt, timeZone)}";

    private static string Format(DateTimeOffset value, TimeZoneInfo timeZone)
        => TimeZoneInfo.ConvertTime(value, timeZone).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Почему строка плана не восстановится — пояснение в плане.</summary>
    internal static string NotApplicableText(FinanceRestoreSkipReason reason) => reason switch
    {
        FinanceRestoreSkipReason.AlreadyExists => "Уже есть в базе",
        FinanceRestoreSkipReason.Conflict => "Номер в базе занят другим — не восстановится",
        FinanceRestoreSkipReason.InvalidMoney => "Сумма не разобралась — внесите вручную",
        FinanceRestoreSkipReason.RefundNotApproved => "Возврат не подтверждён мастером — внесите вручную",
        FinanceRestoreSkipReason.ParentNotInserted => "Относится к операции, которая не восстановится, — проверьте вручную",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };

    /// <summary>Почему применимая строка не передана в этот раз — продолжение «оплата 500 от …: ».</summary>
    internal static string SkippedText(FinanceRestoreSkipReason reason) => reason switch
    {
        FinanceRestoreSkipReason.NoPaymentType => "не выбран тип оплаты",
        FinanceRestoreSkipReason.ParentNotRestoredInThisRun => "восстановится вместе со своей операцией, когда для неё выберут тип оплаты",
        FinanceRestoreSkipReason.ParentUnknown => "не удалось определить операцию — проверьте вручную",
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };

    internal static FinanceRestoreOperationStateView ToStateView(FinanceOperationState state) => state switch
    {
        FinanceOperationState.Approved => FinanceRestoreOperationStateView.Approved,
        FinanceOperationState.Proposed => FinanceRestoreOperationStateView.Proposed,
        FinanceOperationState.Declined => FinanceRestoreOperationStateView.Declined,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };

    internal static FinanceRestoreWarningView ToWarningView(FinanceRestoreWarning warning) => warning switch
    {
        FinanceRestoreWarning.ClaimNotFound => FinanceRestoreWarningView.ClaimNotFound,
        FinanceRestoreWarning.IdConflict => FinanceRestoreWarningView.IdConflict,
        FinanceRestoreWarning.InvalidMoney => FinanceRestoreWarningView.InvalidMoney,
        FinanceRestoreWarning.PaymentTypeNotDetermined => FinanceRestoreWarningView.PaymentTypeNotDetermined,
        FinanceRestoreWarning.ModerationTargetNotFound => FinanceRestoreWarningView.ModerationTargetNotFound,
        FinanceRestoreWarning.ModerationAmbiguous => FinanceRestoreWarningView.ModerationAmbiguous,
        FinanceRestoreWarning.ModerationOfNotRestoredOperation => FinanceRestoreWarningView.ModerationOfNotRestoredOperation,
        FinanceRestoreWarning.PreferentialModeration => FinanceRestoreWarningView.PreferentialModeration,
        FinanceRestoreWarning.TransfersNotRestored => FinanceRestoreWarningView.TransfersNotRestored,
        FinanceRestoreWarning.UnparsedEvents => FinanceRestoreWarningView.UnparsedEvents,
        _ => throw new ArgumentOutOfRangeException(nameof(warning), warning, null),
    };
}
