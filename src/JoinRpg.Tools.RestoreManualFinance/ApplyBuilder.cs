using System.Globalization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Services.Interfaces.Drp;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>
/// Что взято из CSV для одного проекта: тип оплаты по ключу операции, какие ключи в CSV вообще есть,
/// строки плана, которые применять нельзя, и строки CSV, которых нет в плане.
/// </summary>
/// <param name="Choices">Проверенный тип оплаты по ключу восстанавливаемой операции, которая пойдёт в домен.</param>
/// <param name="KeysInCsv">Ключи строк CSV этого проекта, которые есть в плане.</param>
/// <param name="NotApplicable">
/// Строки плана, которые не передаются в домен, с причиной: ошибка типа в CSV, неоднозначный ключ,
/// неподтверждённый возврат.
/// </param>
/// <param name="Warnings">Строки CSV, которых нет в плане.</param>
/// <param name="UnappliedChoices">Номера строк CSV с непустым «Тип оплаты», ключа которых нет в плане.</param>
internal record ProjectCsvDecisions(
    IReadOnlyDictionary<string, PaymentTypeChoice> Choices,
    IReadOnlySet<string> KeysInCsv,
    IReadOnlyDictionary<string, ApplyOutcome> NotApplicable,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<int> UnappliedChoices)
{
    /// <summary>
    /// На кого создать тип «наличные» перед применением: только для операций, которые действительно пойдут
    /// в домен (<see cref="NotApplicable"/> сюда не попадают).
    /// </summary>
    public IReadOnlyCollection<UserIdentification> CashOwners
        => [.. Choices.Values.Select(c => c.CashFor).OfType<UserIdentification>().Distinct().OrderBy(u => u.Value)];
}

/// <summary>
/// Вход <see cref="IClaimRestoreService.RestoreFinanceOperations"/> по одной заявке и судьба каждой строки
/// плана, которая в него не попала.
/// </summary>
/// <param name="OperationKeys">Ключи операций — в порядке <paramref name="Operations"/>.</param>
/// <param name="ModerationKeys">Ключи модераций — в порядке <paramref name="Moderations"/>.</param>
/// <param name="NotPassed">Строки плана, которые не передаются, — с причиной.</param>
internal record ClaimRestoreInput(
    ClaimIdentification ClaimId,
    IReadOnlyList<RestoredFinanceOperation> Operations,
    IReadOnlyList<string> OperationKeys,
    IReadOnlyList<RestoredFinanceModeration> Moderations,
    IReadOnlyList<string> ModerationKeys,
    IReadOnlyDictionary<string, ApplyOutcome> NotPassed)
{
    public bool IsEmpty => Operations.Count == 0 && Moderations.Count == 0;
}

/// <summary>Сопоставление проверенного CSV с заново построенным планом и сборка входа доменного сервиса.</summary>
internal static class ApplyBuilder
{
    public const string NotInCsv = "нет в CSV, прогоните dry-run заново";
    public const string AmbiguousKey = "неоднозначный ключ — разобрать вручную";

    /// <summary>Ключи, которые встречаются в плане проекта больше одного раза: по ключу строку CSV не сопоставить.</summary>
    public static IReadOnlySet<string> DuplicateKeys(ProjectFinancePlan plan)
        => plan.Keys.GroupBy(k => k).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

    /// <summary>Берёт из CSV тип оплаты восстанавливаемых операций и проверяет его по типам проекта.</summary>
    public static ProjectCsvDecisions Match(ProjectFinancePlan plan, IEnumerable<ManualFinanceCsvRow> csvRows)
    {
        var rows = csvRows.Where(r => r.ProjectId == plan.ProjectId).ToList();
        var planKeys = plan.Keys.ToHashSet();
        var restoredOperations = plan.Rows
            .SelectMany(r => r.Operations)
            .Where(o => o.Status == FinanceRestoreStatus.Restore)
            .GroupBy(o => o.Key)
            .ToDictionary(g => g.Key, g => g.First());
        var paymentTypes = plan.PaymentTypes.ToDictionary(p => p.PaymentTypeId.PaymentTypeId);

        var notApplicable = new Dictionary<string, ApplyOutcome>();
        // Повтор ключа в плане или в CSV: какая строка какая — не понять, не применяется ни одна.
        var ambiguous = DuplicateKeys(plan)
            .Concat(rows.GroupBy(r => r.Key).Where(g => g.Count() > 1).Select(g => g.Key))
            .ToHashSet();
        foreach (var key in ambiguous.Where(planKeys.Contains))
        {
            notApplicable[key] = ApplyOutcome.Skipped(AmbiguousKey);
        }

        var choices = new Dictionary<string, PaymentTypeChoice>();
        var warnings = new List<string>();
        var unapplied = new List<int>();
        foreach (var row in rows)
        {
            if (!planKeys.Contains(row.Key))
            {
                warnings.Add($"Строка {row.Line.ToString(CultureInfo.InvariantCulture)}: ключа «{row.Key}» нет в плане проекта {plan.ProjectId.Value.ToString(CultureInfo.InvariantCulture)} — строка не применяется.");
                if (row.PaymentType is not null)
                {
                    unapplied.Add(row.Line);
                }
                continue;
            }
            // Тип оплаты у остальных строк (модерации, уже имеющиеся операции) ни на что не влияет.
            if (ambiguous.Contains(row.Key)
                || row.PaymentType is not { } choice
                || !restoredOperations.TryGetValue(row.Key, out var operation))
            {
                continue;
            }

            if (choice.PaymentTypeId is int id)
            {
                if (!paymentTypes.TryGetValue(id, out var paymentType))
                {
                    notApplicable[row.Key] = ApplyOutcome.Error($"типа оплаты {id.ToString(CultureInfo.InvariantCulture)} нет в проекте");
                    continue;
                }
                if (paymentType.Kind is not (PaymentTypeKind.Cash or PaymentTypeKind.Custom))
                {
                    notApplicable[row.Key] = ApplyOutcome.Error($"тип оплаты {id.ToString(CultureInfo.InvariantCulture)} онлайновый — ручные операции на него не восстанавливаются");
                    continue;
                }
            }
            // Доменный возврат бывает только подтверждённым — заявленный игроком возврат разбирать вручную.
            if (operation.Type == FinanceOperationType.Refund && operation.State != FinanceOperationState.Approved)
            {
                notApplicable[row.Key] = ApplyOutcome.Skipped("возврат не подтверждён мастером — разобрать вручную");
                continue;
            }
            choices[row.Key] = choice;
        }

        return new ProjectCsvDecisions(
            choices,
            rows.Select(r => r.Key).Where(planKeys.Contains).ToHashSet(),
            notApplicable,
            warnings,
            unapplied);
    }

    /// <summary>
    /// Вход доменного сервиса по заявке. Передаются только операции со статусом
    /// <see cref="FinanceRestoreStatus.Restore"/> и выбранным типом и модерации со статусом
    /// <see cref="FinanceRestoreStatus.Restore"/>, кроме решений по операциям, которые не вставляются или
    /// не восстанавливаются в этом прогоне.
    /// </summary>
    /// <param name="cashTypes">Созданные (или найденные) типы «наличные» по владельцу.</param>
    public static ClaimRestoreInput BuildClaim(
        FinanceRestorePlanRow row,
        ProjectCsvDecisions decisions,
        IReadOnlyDictionary<UserIdentification, PaymentTypeIdentification> cashTypes)
    {
        var notPassed = new Dictionary<string, ApplyOutcome>();
        var operations = new List<RestoredFinanceOperation>();
        var operationKeys = new List<string>();
        var passedIndex = new Dictionary<string, int>();

        foreach (var operation in row.Operations)
        {
            if (operation.Status != FinanceRestoreStatus.Restore)
            {
                notPassed[operation.Key] = NotRestored(operation.Status);
                continue;
            }
            if (!decisions.KeysInCsv.Contains(operation.Key))
            {
                notPassed[operation.Key] = ApplyOutcome.Skipped(NotInCsv);
                continue;
            }
            if (decisions.NotApplicable.TryGetValue(operation.Key, out var notApplicable))
            {
                notPassed[operation.Key] = notApplicable;
                continue;
            }
            if (!decisions.Choices.TryGetValue(operation.Key, out var choice))
            {
                notPassed[operation.Key] = ApplyOutcome.Skipped("не задан тип оплаты");
                continue;
            }

            PaymentTypeIdentification paymentType;
            if (choice.PaymentTypeId is int id)
            {
                paymentType = new PaymentTypeIdentification(row.ClaimId.ProjectId, id);
            }
            else if (cashTypes.TryGetValue(choice.CashFor!, out var cash))
            {
                paymentType = cash;
            }
            else
            {
                notPassed[operation.Key] = ApplyOutcome.Error($"не создан тип «наличные» на {choice.Format()}");
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
                paymentType,
                operation.State,
                operation.CreatedAt,
                ChangedAt: operation.ModeratedAt ?? operation.CreatedAt,
                operation.Text));
        }

        var moderations = new List<RestoredFinanceModeration>();
        var moderationKeys = new List<string>();
        foreach (var moderation in row.Moderations)
        {
            if (moderation.Status != FinanceRestoreStatus.Restore)
            {
                notPassed[moderation.Key] = NotRestored(moderation.Status);
                continue;
            }
            if (!decisions.KeysInCsv.Contains(moderation.Key))
            {
                notPassed[moderation.Key] = ApplyOutcome.Skipped(NotInCsv);
                continue;
            }
            if (decisions.NotApplicable.TryGetValue(moderation.Key, out var notApplicable))
            {
                notPassed[moderation.Key] = notApplicable;
                continue;
            }

            var (parentId, parentIndex, skip) = Parent(moderation, operations, passedIndex);
            if (skip is not null)
            {
                notPassed[moderation.Key] = ApplyOutcome.Skipped(skip);
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

        return new ClaimRestoreInput(row.ClaimId, operations, operationKeys, moderations, moderationKeys, notPassed);
    }

    /// <summary>
    /// Родитель модерации — не больше чем одним способом (как требует доменный сервис), либо причина пропуска.
    /// Решение, которое не связалось с операцией (не найдена, неоднозначно, запрос льготы только из
    /// уведомления), передаётся без родителя: вставится комментарий, состояние ничьё не изменится.
    /// Решение по невставляемой или пропущенной в этом прогоне операции откладывается.
    /// </summary>
    private static (int? ParentId, int? ParentIndex, string? Skip) Parent(
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
                return (null, null, $"операция {moderation.TargetOperationKey} в этом прогоне не восстанавливается — решение вставится вместе с ней");
            case FinanceModerationLink.ExistingOperation or FinanceModerationLink.Preferential when moderation.TargetOperationId is int existingId:
                return (existingId, null, null);
            case FinanceModerationLink.Preferential or FinanceModerationLink.NotFound or FinanceModerationLink.Ambiguous:
                return (null, null, null);
            case FinanceModerationLink.NotRestoredOperation:
                return (null, null, $"операция {moderation.TargetOperationKey} не вставляется — разобрать вручную");
            default:
                return (null, null, "операция решения не определена — разобрать вручную");
        }
    }

    private static ApplyOutcome NotRestored(FinanceRestoreStatus status) => status switch
    {
        FinanceRestoreStatus.AlreadyExists => new ApplyOutcome("уже есть"),
        FinanceRestoreStatus.Conflict => ApplyOutcome.Skipped("id занят в базе другим"),
        FinanceRestoreStatus.InvalidMoney => ApplyOutcome.Skipped("некорректная сумма"),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    /// <summary>Исход каждой переданной строки по ответу доменного сервиса.</summary>
    public static IReadOnlyDictionary<string, ApplyOutcome> Outcomes(ClaimRestoreInput input, RestoredFinanceResult result)
    {
        var outcomes = new Dictionary<string, ApplyOutcome>();
        for (var i = 0; i < input.OperationKeys.Count; i++)
        {
            outcomes[input.OperationKeys[i]] = Outcome(result.Operations[i]);
        }
        for (var i = 0; i < input.ModerationKeys.Count; i++)
        {
            outcomes[input.ModerationKeys[i]] = Outcome(result.Moderations[i]);
        }
        return outcomes;
    }

    private static ApplyOutcome Outcome(RestoredFinanceEntry entry)
    {
        var id = entry.Id?.ToString(CultureInfo.InvariantCulture);
        return entry.Status switch
        {
            RestoredFinanceEntryStatus.Inserted => new ApplyOutcome($"вставлено (id {id})"),
            RestoredFinanceEntryStatus.AlreadyExisted => new ApplyOutcome($"уже было (id {id})"),
            RestoredFinanceEntryStatus.TakenElsewhere => ApplyOutcome.Skipped($"id {id} занят другим"),
            RestoredFinanceEntryStatus.NotLostId => ApplyOutcome.Skipped($"id {id} не ниже счётчика — не потерянный"),
            RestoredFinanceEntryStatus.ParentNotRestored => ApplyOutcome.Skipped("операция решения не вставлена"),
            _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Status, null),
        };
    }
}
