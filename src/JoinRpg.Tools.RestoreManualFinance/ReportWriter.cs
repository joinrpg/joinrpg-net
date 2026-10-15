using System.Globalization;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Tools.Drp.Common;

namespace JoinRpg.Tools.RestoreManualFinance;

/// <summary>
/// CSV-отчёт (<see cref="ManualFinanceCsv"/>): строка на каждую операцию и модерацию плана и строка на
/// заявку с предупреждениями и счётчиками. Тексты комментариев не пишутся. Хранить только локально.
/// </summary>
internal static class ReportWriter
{
    public const string OperationKind = "оплата";
    public const string RefundKind = "возврат";
    public const string ClaimKind = "заявка";

    public static IEnumerable<IReadOnlyList<string?>> Rows(IEnumerable<ProjectFinancePlan> plans, ApplyResults results)
    {
        yield return ManualFinanceCsv.Columns;
        foreach (var plan in plans)
        {
            var paymentTypes = plan.PaymentTypes.ToDictionary(p => p.PaymentTypeId.PaymentTypeId);
            var project = Number(plan.ProjectId.Value);
            var duplicates = ApplyBuilder.DuplicateKeys(plan);
            foreach (var row in plan.Rows)
            {
                var claim = Number(row.ClaimId.ClaimId);
                yield return ClaimRow(project, claim, row, results.Claims.GetValueOrDefault(row.ClaimId));
                foreach (var operation in row.Operations)
                {
                    yield return MarkAmbiguous(
                        OperationRow(project, claim, operation, paymentTypes, results.Entries.GetValueOrDefault((plan.ProjectId, operation.Key))),
                        operation.Key,
                        duplicates);
                }
                foreach (var moderation in row.Moderations)
                {
                    yield return MarkAmbiguous(
                        ModerationRow(project, claim, moderation, results.Entries.GetValueOrDefault((plan.ProjectId, moderation.Key))),
                        moderation.Key,
                        duplicates);
                }
            }
        }
    }

    /// <summary>Ключ повторяется в плане проекта — строку не сопоставить, она не применится.</summary>
    private static string[] MarkAmbiguous(string[] row, string key, IReadOnlySet<string> duplicates)
    {
        if (duplicates.Contains(key))
        {
            var index = ManualFinanceCsv.Columns.ToList().IndexOf(ManualFinanceCsv.Flags);
            row[index] = string.IsNullOrEmpty(row[index]) ? ApplyBuilder.AmbiguousKey : ApplyBuilder.AmbiguousKey + "; " + row[index];
        }
        return row;
    }

    public static Task Write(string path, IEnumerable<ProjectFinancePlan> plans, ApplyResults results, CancellationToken ct)
        => DrpCsv.WriteFile(path, Rows(plans, results), ct);

    /// <summary>
    /// Предзаполнение «Тип оплаты»: только однозначно подобранный существующий тип. «Создать наличные» —
    /// пусто: тип создаётся только по явному подтверждению в CSV.
    /// </summary>
    public static string? Prefill(FinanceOperationToRestore operation)
        => operation.Status == FinanceRestoreStatus.Restore && operation.PaymentType.PaymentTypeId is int id
            ? PaymentTypeChoice.Existing(id).Format()
            : null;

    private static string[] Row(
        string project,
        string claim,
        string? key = null,
        string? kind = null,
        string? id = null,
        string? time = null,
        string? initiator = null,
        string? markedBy = null,
        string? money = null,
        string? state = null,
        string? link = null,
        string? status = null,
        string? suggested = null,
        string? paymentType = null,
        string? flags = null,
        string? result = null)
        => [project, claim, key ?? "", kind ?? "", id ?? "", time ?? "", initiator ?? "", markedBy ?? "", money ?? "", state ?? "",
            link ?? "", status ?? "", suggested ?? "", paymentType ?? "", flags ?? "", result ?? ""];

    private static string[] ClaimRow(string project, string claim, FinanceRestorePlanRow row, ApplyOutcome? result)
    {
        List<string> flags = [.. row.Warnings.Select(Describe)];
        void Count(int value, string what)
        {
            if (value > 0)
            {
                flags.Add($"{what}: {Number(value)}");
            }
        }
        Count(row.SkippedForMissingClaim, "не восстановить без заявки");
        Count(row.Transfers, "переводов");
        Count(row.PreferentialRequests, "запросов льготного взноса");
        Count(row.OnlinePayments, "онлайн-оплат");
        Count(row.Unparsed, "не разобрано уведомлений");
        return Row(project, claim,
            kind: ClaimKind,
            status: row.ClaimExists ? null : "заявки нет",
            flags: string.Join("; ", flags),
            result: result?.Text);
    }

    private static string[] OperationRow(
        string project,
        string claim,
        FinanceOperationToRestore operation,
        IReadOnlyDictionary<int, PaymentTypeForRestore> paymentTypes,
        ApplyOutcome? result)
    {
        var state = Describe(operation.State);
        if (operation.ModeratedAt is { } moderatedAt)
        {
            state += $" (решение {Time(moderatedAt)})";
        }
        return Row(project, claim,
            key: operation.Key,
            kind: operation.Type == FinanceOperationType.Refund ? RefundKind : OperationKind,
            id: operation.OperationId is int id ? Number(id) : null,
            time: Time(operation.CreatedAt),
            initiator: Number(operation.Initiator.Value),
            markedBy: operation.IsByPlayer ? "игрок" : "мастер",
            money: operation.Money is int money ? Number(money) : null,
            state: state,
            status: Describe(operation.Status),
            suggested: DrpCsv.UserText(Describe(operation.PaymentType, paymentTypes)),
            paymentType: Prefill(operation),
            flags: OperationFlags(operation),
            result: result?.Text);
    }

    private static string? OperationFlags(FinanceOperationToRestore operation)
    {
        if (operation.Status != FinanceRestoreStatus.Restore)
        {
            return null;
        }
        return operation.PaymentType.How switch
        {
            PaymentTypeGuess.CreateCash => $"подтвердите создание: впишите в «{ManualFinanceCsv.PaymentType}» {PaymentTypeChoice.Cash(operation.PaymentType.CreateCashFor!).Format()} или id другого типа",
            PaymentTypeGuess.Ambiguous or PaymentTypeGuess.NotFound => $"выберите тип оплаты — без него операция не восстанавливается",
            _ => null,
        };
    }

    private static string[] ModerationRow(string project, string claim, FinanceModerationToRestore moderation, ApplyOutcome? result)
        => Row(project, claim,
            key: moderation.Key,
            kind: moderation.Approved ? "модерация: подтверждена" : "модерация: отклонена",
            id: moderation.CommentId is int id ? Number(id) : null,
            time: Time(moderation.CreatedAt),
            initiator: Number(moderation.Initiator.Value),
            markedBy: "мастер",
            link: Describe(moderation),
            status: Describe(moderation.Status),
            result: result?.Text);

    public static string Describe(FinanceModerationToRestore moderation) => moderation.Link switch
    {
        FinanceModerationLink.RestoredOperation => $"с восстанавливаемой операцией {moderation.TargetOperationKey}",
        FinanceModerationLink.ExistingOperation => $"с операцией в базе {Number(moderation.TargetOperationId)}"
            + (moderation.TargetOperationKey is { } key ? $" ({key})" : ""),
        FinanceModerationLink.NotRestoredOperation => $"с невосстанавливаемой операцией {moderation.TargetOperationKey}",
        FinanceModerationLink.Preferential => moderation.TargetOperationId is int id
            ? $"с запросом льготного взноса {Number(id)}"
            : "с запросом льготного взноса из уведомления",
        FinanceModerationLink.NotFound => "не найдена",
        FinanceModerationLink.Ambiguous => "неоднозначно",
        _ => moderation.Link.ToString(),
    };

    public static string Describe(PaymentTypeSuggestion suggestion, IReadOnlyDictionary<int, PaymentTypeForRestore> paymentTypes)
    {
        string Named(int id) => paymentTypes.TryGetValue(id, out var p) ? $"{Number(id)} «{p.Name}»" : Number(id);
        return suggestion.How switch
        {
            PaymentTypeGuess.Initiator => $"{Named(suggestion.PaymentTypeId!.Value)} (владелец — инициатор)",
            PaymentTypeGuess.Recipient => $"{Named(suggestion.PaymentTypeId!.Value)} (владелец среди получателей)",
            PaymentTypeGuess.SingleInProject => $"{Named(suggestion.PaymentTypeId!.Value)} (единственный в проекте)",
            PaymentTypeGuess.CreateCash => $"создать наличные на {Number(suggestion.CreateCashFor!.Value)}",
            PaymentTypeGuess.Ambiguous => "неоднозначно: " + string.Join(", ", suggestion.Candidates.Select(Named)),
            PaymentTypeGuess.NotFound => "не определён",
            _ => suggestion.How.ToString(),
        };
    }

    private static string Describe(FinanceRestoreStatus status) => status switch
    {
        FinanceRestoreStatus.Restore => "восстановить",
        FinanceRestoreStatus.AlreadyExists => "уже есть",
        FinanceRestoreStatus.Conflict => "конфликт id",
        FinanceRestoreStatus.InvalidMoney => "некорректная сумма",
        _ => status.ToString(),
    };

    private static string Describe(FinanceOperationState state) => state switch
    {
        FinanceOperationState.Approved => "подтверждена",
        FinanceOperationState.Proposed => "заявлена",
        FinanceOperationState.Declined => "отклонена",
        _ => state.ToString(),
    };

    private static string Describe(FinanceRestoreWarning warning) => warning switch
    {
        FinanceRestoreWarning.ClaimNotFound => "заявки нет в базе — сначала восстановите заявку",
        FinanceRestoreWarning.IdConflict => "id занят в базе другим",
        FinanceRestoreWarning.InvalidMoney => "сумма не разобрана или не сходится с действием",
        FinanceRestoreWarning.PaymentTypeNotDetermined => "тип оплаты не определён — выберите вручную",
        FinanceRestoreWarning.ModerationTargetNotFound => "решение не связалось с операцией",
        FinanceRestoreWarning.ModerationAmbiguous => "решение подходит к нескольким операциям",
        FinanceRestoreWarning.ModerationOfNotRestoredOperation => "решение по невосстанавливаемой операции",
        FinanceRestoreWarning.PreferentialModeration => "решение по запросу льготного взноса (льгота не меняется)",
        FinanceRestoreWarning.TransfersNotRestored => "переводы между заявками не восстанавливаются",
        FinanceRestoreWarning.UnparsedEvents => "есть неразобранные уведомления",
        _ => warning.ToString(),
    };

    private static string Time(DateTimeOffset value) => value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Number(int? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "";
}
