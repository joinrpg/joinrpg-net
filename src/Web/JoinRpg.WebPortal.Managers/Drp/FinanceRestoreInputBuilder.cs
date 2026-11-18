using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Services.Interfaces.Drp;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Вход <see cref="IClaimRestoreService.RestoreFinanceOperations"/> по одной заявке и строки плана, которые в него
/// не попали.
/// </summary>
/// <param name="OperationKeys">Ключи операций — в порядке <paramref name="Operations"/>.</param>
/// <param name="ModerationKeys">Ключи модераций — в порядке <paramref name="Moderations"/>.</param>
/// <param name="NotPassed">Строки плана, которые не передаются, — с причиной.</param>
public record ClaimFinanceRestoreInput(
    ClaimIdentification ClaimId,
    IReadOnlyList<RestoredFinanceOperation> Operations,
    IReadOnlyList<string> OperationKeys,
    IReadOnlyList<RestoredFinanceModeration> Moderations,
    IReadOnlyList<string> ModerationKeys,
    IReadOnlyDictionary<string, FinanceRestoreSkipReason> NotPassed)
{
    public bool IsEmpty => Operations.Count == 0 && Moderations.Count == 0;
}

/// <summary>
/// Перекладка между базой, планировщиком восстановления ручных финансовых операций (<see cref="FinanceRestorePlanner"/>)
/// и доменным сервисом (<see cref="IClaimRestoreService.RestoreFinanceOperations"/>) — общая для консольного
/// инструмента и страницы. Без обращений к базе.
/// </summary>
public static class FinanceRestoreInputBuilder
{
    /// <summary>
    /// Вход планировщика по уведомлениям и выборке из базы
    /// (<see cref="IProjectRestoreLookupRepository.GetClaimFinanceForRestore"/> по
    /// <see cref="FinanceRestorePlanner.NotifiedCommentIds"/>).
    /// </summary>
    public static FinanceRestoreSource BuildSource(
        ClaimFinanceRestoreLookup lookup,
        IReadOnlyCollection<ClaimNotificationEvent> events,
        DateTimeOffset backupTakenAt)
        => new(
            events,
            lookup.Claims,
            [.. lookup.Operations.Select(o => new ExistingFinanceOperationInfo(o.OperationId, o.ClaimId, o.Type, o.State, o.Money, o.CreatedAt))],
            [.. lookup.Comments.Select(c => new ExistingFinanceCommentInfo(c.CommentId, c.ClaimId, c.Author, c.CreatedAt, c.ExtraAction))],
            lookup.TakenCommentIds,
            [.. lookup.PaymentTypes.Select(p => new ProjectPaymentTypeInfo(p.PaymentTypeId.PaymentTypeId, p.Kind, p.Owner, p.IsActive))],
            backupTakenAt);

    /// <summary>
    /// Вход доменного сервиса по заявке. Передаются применимые операции
    /// (<see cref="FinanceRestoreApplicability.NotApplicableReason(FinanceOperationToRestore)"/>) с выбранным типом
    /// и применимые модерации, кроме решений по операциям, которые не вставляются или не передаются в этом прогоне.
    /// Строки с повторяющимся в заявке ключом не передаются (<see cref="FinanceRestoreSkipReason.AmbiguousKey"/>):
    /// по ключу связываются решения с операциями и ответ сервиса со строками.
    /// </summary>
    /// <param name="paymentType">Тип оплаты операции; null — не выбран, операция не передаётся (<see cref="FinanceRestoreSkipReason.NoPaymentType"/>).</param>
    public static ClaimFinanceRestoreInput BuildClaim(
        FinanceRestorePlanRow row,
        Func<FinanceOperationToRestore, PaymentTypeIdentification?> paymentType)
    {
        var notPassed = new Dictionary<string, FinanceRestoreSkipReason>();
        var operations = new List<RestoredFinanceOperation>();
        var operationKeys = new List<string>();
        var passedIndex = new Dictionary<string, int>();
        var duplicateKeys = row.Operations.Select(o => o.Key)
            .Concat(row.Moderations.Select(m => m.Key))
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        foreach (var operation in row.Operations)
        {
            if (duplicateKeys.Contains(operation.Key))
            {
                notPassed[operation.Key] = FinanceRestoreSkipReason.AmbiguousKey;
                continue;
            }
            if (operation.NotApplicableReason() is { } reason)
            {
                notPassed[operation.Key] = reason;
                continue;
            }
            if (paymentType(operation) is not { } type)
            {
                notPassed[operation.Key] = FinanceRestoreSkipReason.NoPaymentType;
                continue;
            }

            passedIndex[operation.Key] = operations.Count;
            operationKeys.Add(operation.Key);
            operations.Add(new RestoredFinanceOperation(
                operation.OperationId,
                operation.Initiator,
                IsCommentByPlayer: operation.IsByPlayer,
                operation.Type,
                operation.Money!.Value,
                type,
                operation.State,
                operation.CreatedAt,
                ChangedAt: operation.ModeratedAt ?? operation.CreatedAt,
                operation.Text));
        }

        var moderations = new List<RestoredFinanceModeration>();
        var moderationKeys = new List<string>();
        foreach (var moderation in row.Moderations)
        {
            if (duplicateKeys.Contains(moderation.Key))
            {
                notPassed[moderation.Key] = FinanceRestoreSkipReason.AmbiguousKey;
                continue;
            }
            if (moderation.NotApplicableReason() is { } reason)
            {
                notPassed[moderation.Key] = reason;
                continue;
            }

            var (parentId, parentIndex, skip) = Parent(moderation, operations, passedIndex);
            if (skip is { } skipReason)
            {
                notPassed[moderation.Key] = skipReason;
                continue;
            }

            moderationKeys.Add(moderation.Key);
            moderations.Add(new RestoredFinanceModeration(
                moderation.CommentId,
                moderation.Initiator,
                moderation.Approved,
                moderation.CreatedAt,
                moderation.Text,
                ParentOperationId: parentId,
                ParentRestoredIndex: parentIndex));
        }

        return new ClaimFinanceRestoreInput(row.ClaimId, operations, operationKeys, moderations, moderationKeys, notPassed);
    }

    /// <summary>
    /// Родитель модерации — не больше чем одним способом (как требует доменный сервис), либо причина пропуска.
    /// Решение, которое не связалось с операцией (не найдена, неоднозначно, запрос льготы только из
    /// уведомления), передаётся без родителя: вставится комментарий, состояние ничьё не изменится.
    /// Решение по невставляемой или непереданной в этом прогоне операции откладывается.
    /// </summary>
    private static (int? ParentId, int? ParentIndex, FinanceRestoreSkipReason? Skip) Parent(
        FinanceModerationToRestore moderation,
        List<RestoredFinanceOperation> operations,
        Dictionary<string, int> passedIndex)
    {
        switch (moderation.Link)
        {
            case FinanceModerationLink.RestoredOperation:
                if (moderation.TargetOperationKey is { } key && passedIndex.TryGetValue(key, out var index))
                {
                    return operations[index].OperationId is int id ? (id, null, null) : (null, index, null);
                }
                return (null, null, FinanceRestoreSkipReason.ParentNotRestoredInThisRun);
            case FinanceModerationLink.ExistingOperation or FinanceModerationLink.Preferential when moderation.TargetOperationId is int existingId:
                return (existingId, null, null);
            case FinanceModerationLink.Preferential or FinanceModerationLink.NotFound or FinanceModerationLink.Ambiguous:
                return (null, null, null);
            case FinanceModerationLink.NotRestoredOperation:
                return (null, null, FinanceRestoreSkipReason.ParentNotInserted);
            default:
                return (null, null, FinanceRestoreSkipReason.ParentUnknown);
        }
    }

    /// <summary>Ответ доменного сервиса по каждой переданной строке — по ключу.</summary>
    public static IReadOnlyDictionary<string, RestoredFinanceEntry> Outcomes(ClaimFinanceRestoreInput input, RestoredFinanceResult result)
    {
        var outcomes = new Dictionary<string, RestoredFinanceEntry>();
        for (var i = 0; i < input.OperationKeys.Count; i++)
        {
            outcomes[input.OperationKeys[i]] = result.Operations[i];
        }
        for (var i = 0; i < input.ModerationKeys.Count; i++)
        {
            outcomes[input.ModerationKeys[i]] = result.Moderations[i];
        }
        return outcomes;
    }
}
