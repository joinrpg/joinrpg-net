using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ClaimNotifications;

namespace JoinRpg.Services.Drp.ManualFinance;

/// <summary>
/// Строит план восстановления ручных финансовых операций по уведомлениям (DRP, #5561): каких оплат,
/// возвратов и модераций после бэкапа нет в базе, в каком состоянии их вставить и на какой тип оплаты.
/// Онлайн-оплаты, переводы и запросы льготы не восстанавливает — только считает.
/// </summary>
public static class FinanceRestorePlanner
{
    /// <summary>Насколько может разойтись время в базе и в уведомлении, чтобы считать операцию той же.</summary>
    public static readonly TimeSpan SameTimeTolerance = TimeSpan.FromMinutes(1);

    /// <returns>Строки по заявкам, у которых после бэкапа есть ручные финансовые события, в порядке id заявки.</returns>
    public static IReadOnlyList<FinanceRestorePlanRow> Plan(FinanceRestoreSource source)
    {
        var context = new PlanContext(source);
        // События чуть раньше бэкапа не восстанавливаются, но «объясняют» строки базы рядом с бэкапом:
        // без них такую строку могло бы забрать событие после бэкапа.
        return [.. source.Events
            .Where(e => e.CreatedAt >= source.BackupTakenAt - SameTimeTolerance)
            .GroupBy(e => e.ClaimId)
            .Select(g => PlanClaim(context, g.Key, Deduplicate(g)))
            .OfType<FinanceRestorePlanRow>()
            .OrderBy(r => r.ClaimId.ClaimId)];
    }

    /// <summary>
    /// Id из уведомлений, о которых спрашивать базу (<see cref="FinanceRestoreSource.TakenCommentIds"/>): только
    /// у финансовых событий, которые рассматривает <see cref="Plan"/> (не раньше бэкапа за вычетом
    /// <see cref="SameTimeTolerance"/>).
    /// </summary>
    public static IReadOnlyCollection<int> NotifiedCommentIds(IEnumerable<ClaimNotificationEvent> events, DateTimeOffset backupTakenAt)
        => [.. events
            .Where(e => e.Action is CommentExtraAction.PaidFee or CommentExtraAction.RefundFee
                or CommentExtraAction.ApproveFinance or CommentExtraAction.RejectFinance)
            .Where(e => e.CreatedAt >= backupTakenAt - SameTimeTolerance)
            .Select(e => e.CommentId)
            .OfType<int>()
            .Distinct()];

    private sealed class PlanContext(FinanceRestoreSource source)
    {
        public DateTimeOffset BackupTakenAt { get; } = source.BackupTakenAt;
        public HashSet<ClaimIdentification> Claims { get; } = [.. source.Claims];
        public ILookup<ClaimIdentification, ExistingFinanceOperationInfo> OperationsByClaim { get; } = source.ExistingOperations.ToLookup(o => o.ClaimId);
        public ILookup<ClaimIdentification, ExistingFinanceCommentInfo> CommentsByClaim { get; } = source.ExistingComments.ToLookup(c => c.ClaimId);

        /// <summary>Id комментария (операции) в базе → заявка, которой он принадлежит.</summary>
        public Dictionary<int, ClaimIdentification> ExistingIds { get; } = source.ExistingComments.Select(c => (Id: c.CommentId, c.ClaimId))
            .Concat(source.ExistingOperations.Select(o => (Id: o.OperationId, o.ClaimId)))
            .DistinctBy(x => x.Id)
            .ToDictionary(x => x.Id, x => x.ClaimId);

        public HashSet<int> TakenIds { get; } = [.. source.TakenCommentIds];

        /// <summary>Неонлайн-типы оплаты проекта: только на них могут быть ручные операции.</summary>
        public IReadOnlyList<ProjectPaymentTypeInfo> ManualPaymentTypes { get; } =
            [.. source.PaymentTypes.Where(p => p.Kind is PaymentTypeKind.Cash or PaymentTypeKind.Custom)];

        /// <summary>Есть ли id в этой заявке (null — нет нигде), иначе конфликт.</summary>
        public FinanceRestoreStatus? StatusById(int id, ClaimIdentification claimId)
        {
            if (ExistingIds.TryGetValue(id, out var owner))
            {
                return owner == claimId ? FinanceRestoreStatus.AlreadyExists : FinanceRestoreStatus.Conflict;
            }
            return TakenIds.Contains(id) ? FinanceRestoreStatus.Conflict : null;
        }
    }

    /// <summary>Операция из уведомления и id строки базы, с которой она совпала (если <see cref="FinanceRestoreStatus.AlreadyExists"/>).</summary>
    private sealed record PlannedOperation(FinanceOperationToRestore Operation, int? ExistingOperationId);

    private static FinanceRestorePlanRow? PlanClaim(PlanContext context, ClaimIdentification claimId, IReadOnlyList<ClaimNotificationEvent> allEvents)
    {
        var window = allEvents.Where(e => e.CreatedAt < context.BackupTakenAt && e.Kind == ClaimNotificationKind.ClaimChange).ToList();
        var events = allEvents.Where(e => e.CreatedAt >= context.BackupTakenAt).ToList();
        var changes = events.Where(e => e.Kind == ClaimNotificationKind.ClaimChange).OrderBy(e => e.CreatedAt).ToList();
        var operationEvents = changes.Where(IsOperation).ToList();
        var moderationEvents = changes.Where(IsModeration).ToList();
        var preferentialEvents = changes.Where(e => e.Action == CommentExtraAction.RequestPreferential).ToList();
        var transfers = changes.Count(e => e.Action is CommentExtraAction.TransferTo or CommentExtraAction.TransferFrom);
        var onlinePayments = events.Count(e => e.Kind == ClaimNotificationKind.OnlinePayment);
        var unparsed = events.Count(e => e.Kind == ClaimNotificationKind.Other);

        if (operationEvents.Count + moderationEvents.Count + preferentialEvents.Count + transfers == 0)
        {
            return null;
        }

        var warnings = new List<FinanceRestoreWarning>();
        if (!context.Claims.Contains(claimId))
        {
            warnings.Add(FinanceRestoreWarning.ClaimNotFound);
            AddCountWarnings(warnings, transfers, unparsed);
            return new FinanceRestorePlanRow(claimId, ClaimExists: false, [], [],
                SkippedForMissingClaim: operationEvents.Count + moderationEvents.Count,
                transfers, preferentialEvents.Count, onlinePayments, unparsed, warnings);
        }

        var existingOperations = context.OperationsByClaim[claimId].ToList();
        var existingComments = context.CommentsByClaim[claimId].ToList();

        var planned = PlanOperations(context, claimId, [.. window.Where(IsOperation)], operationEvents, existingOperations);
        var moderations = PlanModerations(context, claimId, [.. window.Where(IsModeration)], moderationEvents, existingComments);
        var operations = LinkModerations(planned, moderations, existingOperations, preferentialEvents);

        var restoredOperations = operations.Where(o => o.Status == FinanceRestoreStatus.Restore).ToList();
        var restoredModerations = moderations.Where(m => m.Status == FinanceRestoreStatus.Restore).ToList();
        if (operations.Any(o => o.Status == FinanceRestoreStatus.Conflict) || moderations.Any(m => m.Status == FinanceRestoreStatus.Conflict))
        {
            warnings.Add(FinanceRestoreWarning.IdConflict);
        }
        if (operations.Any(o => o.Status == FinanceRestoreStatus.InvalidMoney))
        {
            warnings.Add(FinanceRestoreWarning.InvalidMoney);
        }
        if (restoredOperations.Any(o => o.PaymentType.How is PaymentTypeGuess.Ambiguous or PaymentTypeGuess.NotFound))
        {
            warnings.Add(FinanceRestoreWarning.PaymentTypeNotDetermined);
        }
        if (restoredModerations.Any(m => m.Link == FinanceModerationLink.NotFound))
        {
            warnings.Add(FinanceRestoreWarning.ModerationTargetNotFound);
        }
        if (restoredModerations.Any(m => m.Link == FinanceModerationLink.Ambiguous))
        {
            warnings.Add(FinanceRestoreWarning.ModerationAmbiguous);
        }
        if (restoredModerations.Any(m => m.Link == FinanceModerationLink.NotRestoredOperation))
        {
            warnings.Add(FinanceRestoreWarning.ModerationOfNotRestoredOperation);
        }
        if (restoredModerations.Any(m => m.Link == FinanceModerationLink.Preferential))
        {
            warnings.Add(FinanceRestoreWarning.PreferentialModeration);
        }
        AddCountWarnings(warnings, transfers, unparsed);

        return new FinanceRestorePlanRow(claimId, ClaimExists: true, operations, moderations, SkippedForMissingClaim: 0,
            transfers, preferentialEvents.Count, onlinePayments, unparsed, warnings);
    }

    private static bool IsOperation(ClaimNotificationEvent e) => e.Action is CommentExtraAction.PaidFee or CommentExtraAction.RefundFee;

    private static bool IsModeration(ClaimNotificationEvent e) => e.Action is CommentExtraAction.ApproveFinance or CommentExtraAction.RejectFinance;

    private static void AddCountWarnings(List<FinanceRestoreWarning> warnings, int transfers, int unparsed)
    {
        if (transfers > 0)
        {
            warnings.Add(FinanceRestoreWarning.TransfersNotRestored);
        }
        if (unparsed > 0)
        {
            warnings.Add(FinanceRestoreWarning.UnparsedEvents);
        }
    }

    private static FinanceOperationType OperationType(ClaimNotificationEvent e)
        => e.Action == CommentExtraAction.PaidFee ? FinanceOperationType.Submit : FinanceOperationType.Refund;

    /// <summary>Строка базы без id, совпадающая с операцией из уведомления; каждая строка — не больше чем с одним событием.</summary>
    private static ExistingFinanceOperationInfo? MatchOperation(
        IReadOnlyList<ExistingFinanceOperationInfo> existingOperations,
        HashSet<int> matched,
        ClaimNotificationEvent e)
    {
        var type = OperationType(e);
        var existing = existingOperations.FirstOrDefault(o => !matched.Contains(o.OperationId)
            && o.Type == type
            && o.Money == e.Money
            && IsSameTime(o.CreatedAt, e.CreatedAt));
        if (existing is not null)
        {
            _ = matched.Add(existing.OperationId);
        }
        return existing;
    }

    private static List<PlannedOperation> PlanOperations(
        PlanContext context,
        ClaimIdentification claimId,
        IReadOnlyList<ClaimNotificationEvent> windowEvents,
        IReadOnlyList<ClaimNotificationEvent> events,
        IReadOnlyList<ExistingFinanceOperationInfo> existingOperations)
    {
        var matched = new HashSet<int>();
        foreach (var e in windowEvents)
        {
            if (e.CommentId is int id)
            {
                _ = matched.Add(id);
            }
            else
            {
                _ = MatchOperation(existingOperations, matched, e);
            }
        }

        var result = new List<PlannedOperation>();
        foreach (var e in events)
        {
            var type = OperationType(e);
            FinanceRestoreStatus status;
            int? existingId = null;
            if (e.CommentId is int id)
            {
                status = context.StatusById(id, claimId) ?? FinanceRestoreStatus.Restore;
                if (status == FinanceRestoreStatus.AlreadyExists)
                {
                    existingId = id;
                    _ = matched.Add(id);
                }
            }
            else if (MatchOperation(existingOperations, matched, e) is { } existing)
            {
                status = FinanceRestoreStatus.AlreadyExists;
                existingId = existing.OperationId;
            }
            else
            {
                status = FinanceRestoreStatus.Restore;
            }

            if (status == FinanceRestoreStatus.Restore && !IsValidMoney(type, e.Money))
            {
                status = FinanceRestoreStatus.InvalidMoney;
            }

            var byPlayer = e.Author == ClaimEventAuthor.Player;
            var operation = new FinanceOperationToRestore(
                Key: MakeKey(e.CommentId, "op", claimId, e),
                e.CommentId,
                e.Initiator,
                byPlayer,
                type,
                e.Money,
                e.CreatedAt,
                e.Text,
                e.Action!.Value,
                State: byPlayer ? FinanceOperationState.Proposed : FinanceOperationState.Approved,
                ModeratedAt: null,
                SuggestPaymentType(context.ManualPaymentTypes, e, byPlayer),
                status);
            result.Add(new PlannedOperation(operation, existingId));
        }
        return result;
    }

    /// <summary>Знак суммы — по типу операции, как в <c>AcceptFee</c>: оплата положительная, возврат отрицательный.</summary>
    private static bool IsValidMoney(FinanceOperationType type, int? money)
        => type == FinanceOperationType.Submit ? money > 0 : money < 0;

    /// <summary>Комментарий базы без id, совпадающий с модерацией из уведомления; каждый — не больше чем с одним событием.</summary>
    private static bool MatchModeration(IReadOnlyList<ExistingFinanceCommentInfo> existingComments, HashSet<int> matched, ClaimNotificationEvent e)
    {
        var existing = existingComments.FirstOrDefault(c => !matched.Contains(c.CommentId)
            && c.Author == e.Initiator
            && c.ExtraAction == e.Action
            && IsSameTime(c.CreatedAt, e.CreatedAt));
        if (existing is null)
        {
            return false;
        }
        _ = matched.Add(existing.CommentId);
        return true;
    }

    private static List<FinanceModerationToRestore> PlanModerations(
        PlanContext context,
        ClaimIdentification claimId,
        IReadOnlyList<ClaimNotificationEvent> windowEvents,
        IReadOnlyList<ClaimNotificationEvent> events,
        IReadOnlyList<ExistingFinanceCommentInfo> existingComments)
    {
        var matched = new HashSet<int>();
        foreach (var e in windowEvents)
        {
            if (e.CommentId is int id)
            {
                _ = matched.Add(id);
            }
            else
            {
                _ = MatchModeration(existingComments, matched, e);
            }
        }

        var result = new List<FinanceModerationToRestore>();
        foreach (var e in events)
        {
            FinanceRestoreStatus status;
            if (e.CommentId is int id)
            {
                status = context.StatusById(id, claimId) ?? FinanceRestoreStatus.Restore;
                _ = matched.Add(id);
            }
            else
            {
                status = MatchModeration(existingComments, matched, e) ? FinanceRestoreStatus.AlreadyExists : FinanceRestoreStatus.Restore;
            }

            result.Add(new FinanceModerationToRestore(
                Key: MakeKey(e.CommentId, "mod", claimId, e),
                e.CommentId,
                e.Initiator,
                Approved: e.Action == CommentExtraAction.ApproveFinance,
                e.CreatedAt,
                e.Text,
                FinanceModerationLink.NotFound,
                TargetOperationKey: null,
                TargetOperationId: null,
                status));
        }
        return result;
    }

    /// <summary>Заявленная операция, которую могла модерировать модерация.</summary>
    private sealed class ModerationTarget
    {
        public required DateTimeOffset CreatedAt { get; init; }
        public required FinanceModerationLink Link { get; init; }

        /// <summary>Индекс операции из уведомлений, если цель — она.</summary>
        public int? OperationIndex { get; init; }

        public int? ExistingOperationId { get; init; }
        public bool IsLinked { get; set; }
    }

    /// <summary>
    /// Связывает модерации с операциями по заявке, в порядке времени: модерация достаётся единственной
    /// ещё не связанной заявленной операции, созданной раньше неё. Уже имеющаяся в базе модерация тоже
    /// связывается — чтобы не увести операцию, которую на самом деле модерировала она.
    /// </summary>
    /// <returns>Операции с учётом модерации.</returns>
    private static List<FinanceOperationToRestore> LinkModerations(
        IReadOnlyList<PlannedOperation> planned,
        List<FinanceModerationToRestore> moderations,
        IReadOnlyList<ExistingFinanceOperationInfo> existingOperations,
        IReadOnlyList<ClaimNotificationEvent> preferentialEvents)
    {
        var operations = planned.Select(p => p.Operation).ToList();
        // Строки базы, совпавшие с операциями из уведомлений, — цель в виде этих операций, а не отдельно.
        var explained = planned.Select(p => p.ExistingOperationId).OfType<int>().ToHashSet();
        var existingPreferential = existingOperations
            .Where(o => o.Type == FinanceOperationType.PreferentialFeeRequest && o.State == FinanceOperationState.Proposed)
            .ToList();

        List<ModerationTarget> targets =
        [
            // Операция игрока из уведомлений — цель в любом состоянии: уже применённая модерация могла сделать её Approved.
            .. planned
                .Select((p, index) => (p, index))
                .Where(x => x.p.Operation.IsByPlayer)
                .Select(x => new ModerationTarget
                {
                    CreatedAt = x.p.Operation.CreatedAt,
                    Link = x.p.Operation.Status switch
                    {
                        FinanceRestoreStatus.Restore => FinanceModerationLink.RestoredOperation,
                        FinanceRestoreStatus.AlreadyExists => FinanceModerationLink.ExistingOperation,
                        _ => FinanceModerationLink.NotRestoredOperation,
                    },
                    OperationIndex = x.index,
                    ExistingOperationId = x.p.ExistingOperationId,
                }),
            .. existingOperations
                .Where(o => o.Type == FinanceOperationType.Submit && o.State == FinanceOperationState.Proposed && !explained.Contains(o.OperationId))
                .Select(o => new ModerationTarget { CreatedAt = o.CreatedAt, Link = FinanceModerationLink.ExistingOperation, ExistingOperationId = o.OperationId }),
            .. existingPreferential
                .Select(o => new ModerationTarget { CreatedAt = o.CreatedAt, Link = FinanceModerationLink.Preferential, ExistingOperationId = o.OperationId }),
            .. preferentialEvents
                .Where(e => !existingPreferential.Any(o => IsSameTime(o.CreatedAt, e.CreatedAt)))
                .Select(e => new ModerationTarget { CreatedAt = e.CreatedAt, Link = FinanceModerationLink.Preferential }),
        ];

        for (var i = 0; i < moderations.Count; i++)
        {
            var moderation = moderations[i];
            var candidates = targets.Where(t => !t.IsLinked && t.CreatedAt < moderation.CreatedAt).ToList();
            if (candidates.Count != 1)
            {
                moderations[i] = moderation with { Link = candidates.Count == 0 ? FinanceModerationLink.NotFound : FinanceModerationLink.Ambiguous };
                continue;
            }

            var target = candidates[0];
            target.IsLinked = true;
            string? targetKey = null;
            if (target.OperationIndex is int index)
            {
                var operation = operations[index];
                targetKey = operation.Key;
                // Невставляемая модерация и невставляемая операция ничьё состояние не меняют.
                if (moderation.Status != FinanceRestoreStatus.Conflict && target.Link != FinanceModerationLink.NotRestoredOperation)
                {
                    operations[index] = operation with
                    {
                        State = moderation.Approved ? FinanceOperationState.Approved : FinanceOperationState.Declined,
                        ModeratedAt = moderation.CreatedAt,
                    };
                }
            }
            moderations[i] = moderation with { Link = target.Link, TargetOperationKey = targetKey, TargetOperationId = target.ExistingOperationId };
        }
        return operations;
    }

    /// <summary>
    /// Предлагает тип оплаты по шагам: владелец — инициатор, владелец среди получателей, единственный
    /// неонлайн-тип проекта. Неоднозначный шаг дальше не идёт: его кандидатов выбирает человек.
    /// Заявленная игроком операция бывает только на чужой тип (<c>RequireFeeAccess</c>): типы игрока не предлагаются.
    /// </summary>
    private static PaymentTypeSuggestion SuggestPaymentType(IReadOnlyList<ProjectPaymentTypeInfo> paymentTypes, ClaimNotificationEvent e, bool byPlayer)
    {
        var recipients = e.Recipients.Select(r => r.UserId).ToHashSet();
        IReadOnlyList<ProjectPaymentTypeInfo> allowed = byPlayer ? [.. paymentTypes.Where(p => p.Owner != e.Initiator)] : paymentTypes;
        (PaymentTypeGuess How, IEnumerable<ProjectPaymentTypeInfo> Candidates)[] steps =
        [
            (PaymentTypeGuess.Initiator, byPlayer ? [] : allowed.Where(p => p.Owner == e.Initiator)),
            (PaymentTypeGuess.Recipient, allowed.Where(p => recipients.Contains(p.Owner))),
            (PaymentTypeGuess.SingleInProject, allowed),
        ];
        foreach (var (how, candidates) in steps)
        {
            if (Choose([.. candidates], how) is { } suggestion)
            {
                return suggestion;
            }
        }

        return e.Author == ClaimEventAuthor.Master
            ? new PaymentTypeSuggestion(PaymentTypeId: null, CreateCashFor: e.Initiator, PaymentTypeGuess.CreateCash, Candidates: [])
            : new PaymentTypeSuggestion(PaymentTypeId: null, CreateCashFor: null, PaymentTypeGuess.NotFound, Candidates: []);
    }

    /// <summary>
    /// Один кандидат — он; несколько — единственный активный, иначе единственный «наличные» среди них,
    /// иначе неоднозначно. Нет кандидатов — null, следующий шаг.
    /// </summary>
    private static PaymentTypeSuggestion? Choose(IReadOnlyList<ProjectPaymentTypeInfo> candidates, PaymentTypeGuess how)
    {
        if (candidates.Count == 0)
        {
            return null;
        }
        if (candidates.Count == 1)
        {
            return Chosen(candidates[0], how);
        }

        var active = candidates.Where(p => p.IsActive).ToList();
        if (active.Count == 1)
        {
            return Chosen(active[0], how);
        }

        var pool = active.Count > 1 ? active : candidates;
        var cash = pool.Where(p => p.Kind == PaymentTypeKind.Cash).ToList();
        if (cash.Count == 1)
        {
            return Chosen(cash[0], how);
        }

        return new PaymentTypeSuggestion(PaymentTypeId: null, CreateCashFor: null, PaymentTypeGuess.Ambiguous,
            [.. pool.Select(p => p.PaymentTypeId).Order()]);
    }

    private static PaymentTypeSuggestion Chosen(ProjectPaymentTypeInfo paymentType, PaymentTypeGuess how)
        => new(paymentType.PaymentTypeId, CreateCashFor: null, how, Candidates: []);

    /// <summary>
    /// <c>id:{id}</c>, если id известен; иначе по заявке, времени (тики UTC) и инициатору — так ключ
    /// не меняется при повторном построении плана по тем же уведомлениям.
    /// </summary>
    private static string MakeKey(int? id, string prefix, ClaimIdentification claimId, ClaimNotificationEvent e)
        => id is int value
        ? $"id:{value}"
        : $"{prefix}:{claimId.ClaimId}:{e.CreatedAt.UtcTicks}:{e.Initiator.Value}";

    private static bool IsSameTime(DateTimeOffset a, DateTimeOffset b) => (a - b).Duration() <= SameTimeTolerance;

    /// <summary>
    /// Одно и то же событие не должно дать две строки: с id — по id, без id — по автору, времени,
    /// действию, сумме и тексту. Получатели копий объединяются — для подбора типа оплаты нужны все.
    /// </summary>
    private static List<ClaimNotificationEvent> Deduplicate(IEnumerable<ClaimNotificationEvent> events)
        => [.. events
            .GroupBy(e => e.CommentId is int id
                ? (object)(e.Kind, id)
                : (e.Kind, e.Initiator, e.CreatedAt, e.Action, e.Money, Text: e.Text.Replace("\r\n", "\n").Trim()))
            .Select(g => g.First() with { Recipients = [.. g.SelectMany(e => e.Recipients).Distinct()] })];
}
