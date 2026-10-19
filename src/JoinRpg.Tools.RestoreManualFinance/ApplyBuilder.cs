using System.Globalization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.WebPortal.Managers.Drp;

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
/// Вход <see cref="IClaimRestoreService.RestoreFinanceOperations"/> по одной заявке
/// (<see cref="FinanceRestoreInputBuilder.BuildClaim"/>) и судьба каждой строки плана, которая в него не попала, —
/// с текстом для отчёта, включая причины, которые есть только у CSV.
/// </summary>
/// <param name="NotPassed">Строки плана, которые не передаются, — с причиной.</param>
internal record ClaimRestoreInput(ClaimFinanceRestoreInput Domain, IReadOnlyDictionary<string, ApplyOutcome> NotPassed)
{
    public ClaimIdentification ClaimId => Domain.ClaimId;

    public IReadOnlyList<RestoredFinanceOperation> Operations => Domain.Operations;

    /// <summary>Ключи операций — в порядке <see cref="Operations"/>.</summary>
    public IReadOnlyList<string> OperationKeys => Domain.OperationKeys;

    public IReadOnlyList<RestoredFinanceModeration> Moderations => Domain.Moderations;

    /// <summary>Ключи модераций — в порядке <see cref="Moderations"/>.</summary>
    public IReadOnlyList<string> ModerationKeys => Domain.ModerationKeys;

    public bool IsEmpty => Domain.IsEmpty;
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
            // Неприменимая операция (например, неподтверждённый возврат) не должна создать тип «наличные».
            if (operation.NotApplicableReason() is { } reason)
            {
                notApplicable[row.Key] = SkipOutcome(reason);
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
    /// Вход доменного сервиса по заявке (<see cref="FinanceRestoreInputBuilder.BuildClaim"/>). Строки, которые
    /// не проходят по CSV (нет в CSV, неоднозначный ключ, ошибка или отсутствие типа, не создан тип «наличные»),
    /// отсеиваются заранее — решения по отсеянным операциям откладываются так же, как по непереданным.
    /// </summary>
    /// <param name="cashTypes">Созданные (или найденные) типы «наличные» по владельцу.</param>
    public static ClaimRestoreInput BuildClaim(
        FinanceRestorePlanRow row,
        ProjectCsvDecisions decisions,
        IReadOnlyDictionary<UserIdentification, PaymentTypeIdentification> cashTypes)
    {
        // По индексу в плане: ключи могут повторяться.
        var csvOperations = row.Operations.Select(o => CsvOutcome(o, decisions, cashTypes)).ToList();
        var csvModerations = row.Moderations.Select(m => CsvOutcome(m.Status, m.Key, decisions)).ToList();

        var domain = FinanceRestoreInputBuilder.BuildClaim(
            row with
            {
                Operations = [.. row.Operations.Where((_, i) => csvOperations[i] is null)],
                Moderations = [.. row.Moderations.Where((_, i) => csvModerations[i] is null)],
            },
            operation => PaymentType(operation, row.ClaimId.ProjectId, decisions, cashTypes));

        // В порядке плана: при повторе ключа в отчёт попадает последняя строка.
        var notPassed = new Dictionary<string, ApplyOutcome>();
        for (var i = 0; i < row.Operations.Count; i++)
        {
            var key = row.Operations[i].Key;
            if (csvOperations[i] is { } outcome)
            {
                notPassed[key] = outcome;
            }
            else if (domain.NotPassed.TryGetValue(key, out var reason))
            {
                notPassed[key] = SkipOutcome(reason);
            }
        }
        for (var i = 0; i < row.Moderations.Count; i++)
        {
            var moderation = row.Moderations[i];
            if (csvModerations[i] is { } outcome)
            {
                notPassed[moderation.Key] = outcome;
            }
            else if (domain.NotPassed.TryGetValue(moderation.Key, out var reason))
            {
                notPassed[moderation.Key] = SkipOutcome(reason, moderation.TargetOperationKey);
            }
        }

        return new ClaimRestoreInput(domain, notPassed);
    }

    /// <summary>Причина не передавать строку, которая есть только у CSV; null — решает общая сборка.</summary>
    private static ApplyOutcome? CsvOutcome(FinanceRestoreStatus status, string key, ProjectCsvDecisions decisions)
    {
        if (status != FinanceRestoreStatus.Restore)
        {
            return null;
        }
        if (!decisions.KeysInCsv.Contains(key))
        {
            return ApplyOutcome.Skipped(NotInCsv);
        }
        return decisions.NotApplicable.GetValueOrDefault(key);
    }

    private static ApplyOutcome? CsvOutcome(
        FinanceOperationToRestore operation,
        ProjectCsvDecisions decisions,
        IReadOnlyDictionary<UserIdentification, PaymentTypeIdentification> cashTypes)
    {
        if (operation.Status != FinanceRestoreStatus.Restore)
        {
            return null;
        }
        if (CsvOutcome(operation.Status, operation.Key, decisions) is { } outcome)
        {
            return outcome;
        }
        if (!decisions.Choices.TryGetValue(operation.Key, out var choice))
        {
            return SkipOutcome(FinanceRestoreSkipReason.NoPaymentType);
        }
        if (choice.PaymentTypeId is null && !cashTypes.ContainsKey(choice.CashFor!))
        {
            return ApplyOutcome.Error($"не создан тип «наличные» на {choice.Format()}");
        }
        return null;
    }

    private static PaymentTypeIdentification? PaymentType(
        FinanceOperationToRestore operation,
        ProjectIdentification projectId,
        ProjectCsvDecisions decisions,
        IReadOnlyDictionary<UserIdentification, PaymentTypeIdentification> cashTypes)
    {
        if (!decisions.Choices.TryGetValue(operation.Key, out var choice))
        {
            return null;
        }
        return choice.PaymentTypeId is int id
            ? new PaymentTypeIdentification(projectId, id)
            : cashTypes.GetValueOrDefault(choice.CashFor!);
    }

    /// <param name="targetOperationKey">Ключ операции решения — для причин про родителя модерации.</param>
    private static ApplyOutcome SkipOutcome(FinanceRestoreSkipReason reason, string? targetOperationKey = null) => reason switch
    {
        FinanceRestoreSkipReason.AlreadyExists => new ApplyOutcome("уже есть"),
        FinanceRestoreSkipReason.Conflict => ApplyOutcome.Skipped("id занят в базе другим"),
        FinanceRestoreSkipReason.InvalidMoney => ApplyOutcome.Skipped("некорректная сумма"),
        FinanceRestoreSkipReason.RefundNotApproved => ApplyOutcome.Skipped("возврат не подтверждён мастером — разобрать вручную"),
        FinanceRestoreSkipReason.NoPaymentType => ApplyOutcome.Skipped("не задан тип оплаты"),
        FinanceRestoreSkipReason.ParentNotRestoredInThisRun => ApplyOutcome.Skipped($"операция {targetOperationKey} в этом прогоне не восстанавливается — решение вставится вместе с ней"),
        FinanceRestoreSkipReason.ParentNotInserted => ApplyOutcome.Skipped($"операция {targetOperationKey} не вставляется — разобрать вручную"),
        FinanceRestoreSkipReason.ParentUnknown => ApplyOutcome.Skipped("операция решения не определена — разобрать вручную"),
        // Повторы ключа консоль отсекает раньше (Match), сюда они не доходят — но подпись та же.
        FinanceRestoreSkipReason.AmbiguousKey => ApplyOutcome.Skipped(AmbiguousKey),
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null),
    };

    /// <summary>Исход каждой переданной строки по ответу доменного сервиса.</summary>
    public static IReadOnlyDictionary<string, ApplyOutcome> Outcomes(ClaimRestoreInput input, RestoredFinanceResult result)
        => FinanceRestoreInputBuilder.Outcomes(input.Domain, result).ToDictionary(e => e.Key, e => Outcome(e.Value));

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
