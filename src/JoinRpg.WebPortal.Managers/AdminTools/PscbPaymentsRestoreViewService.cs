using System.Text;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Восстановление потерянных платежей ПСКБ из списка банка (ADR026): заново скачивает период,
/// берёт строки, которым сверка вынесла вердикт «можно восстановить автоматически», и пишет их
/// по одной доменным сервисом <see cref="ILostPaymentsRestoreService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Обработка по одной строке с продолжением после ошибки — как в <c>ManualFinanceApplier</c>
/// и <c>FinanceRestorePanel</c>: отказ одной строки не уносит остальные, а исход каждой попадает
/// в отчёт. Исключение — отсутствие админских прав: это отказ всего прогона, а не строки,
/// поэтому <c>MustBeAdminException</c> пробрасывается контроллеру.
/// </para>
/// <para>
/// Ограничение на прогон: не больше <see cref="MaxRecordsPerRun"/> записей. При превышении прогон
/// отказывается целиком, до первой записи — частичное восстановление молча оставило бы вопрос
/// «доделано ли».
/// </para>
/// <para>
/// В банк при восстановлении не ходим: первоисточник — скачанный список. Повторный прогон
/// идемпотентен силой самой сверки — восстановленная строка становится проведённой.
/// </para>
/// </remarks>
internal class PscbPaymentsRestoreViewService(
    PscbPaymentsViewService paymentsViewService,
    ILostPaymentsRestoreService lostPaymentsRestoreService,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<PscbPaymentsRestoreViewService> logger) : IPscbPaymentsRestoreClient
{
    /// <summary>Максимум записей (платежей и возвратов вместе) за один прогон.</summary>
    private const int MaxRecordsPerRun = 100;

    private const string FeeWarning = "; взнос заявки не пересчитан — операция записана, зафиксируйте взнос вручную";

    public async Task<PscbRestoreResult> Restore(PscbPaymentsQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (list, plans) = await paymentsViewService.GetPaymentsWithRestorePlan(query);
        if (!list.IsSuccess)
        {
            throw new JoinValidationException(BankFailure(list));
        }

        var candidates = list.Payments
            .Where(row => row.MatchState is PscbPaymentMatchState.Lost or PscbPaymentMatchState.RefundMissing
                && row.RestoreBlockReason is null)
            .ToList();

        var recordCount = candidates.Sum(RecordsIn);
        if (recordCount > MaxRecordsPerRun)
        {
            throw new JoinValidationException(
                $"За один прогон восстанавливается не больше {MaxRecordsPerRun} записей, а в списке их {recordCount}. Сузьте диапазон.");
        }

        var rows = new List<PscbRestoreRowResult>(candidates.Count);
        var payments = 0;
        var refunds = 0;
        foreach (var row in candidates)
        {
            var (result, restoredPayments, restoredRefunds) = await RestoreRowAsync(row, plans);
            rows.Add(result);
            payments += restoredPayments;
            refunds += restoredRefunds;
        }

        // Одна строка на прогон — как аудит слияния пользователей (ADR024): кто, за какой период
        // и чем прогон кончился. По записям и отказам доменный сервис логирует сам.
        logger.LogWarning(
            "Восстановление потерянных платежей ПСКБ за период {DateFrom}–{DateTo} админом {AdminUserId}: "
            + "кандидатов {Candidates}, восстановлено платежей {Payments} и возвратов {Refunds}, отказов {Failed}",
            query.DateFrom,
            query.DateTo,
            currentUserAccessor.UserId,
            candidates.Count,
            payments,
            refunds,
            rows.Count(row => row.Outcome == PscbRestoreRowOutcome.Failed));

        return new PscbRestoreResult
        {
            RestoredPaymentsCount = payments,
            RestoredRefundsCount = refunds,
            Rows = rows,
        };
    }

    /// <summary>Сколько записей напишет строка: платёж и, если банк зафиксировал, его возврат.</summary>
    private static int RecordsIn(PscbPaymentListRow row)
        => row.MatchState == PscbPaymentMatchState.RefundMissing || !row.RefundedByBank ? 1 : 2;

    private async Task<(PscbRestoreRowResult Row, int Payments, int Refunds)> RestoreRowAsync(
        PscbPaymentListRow row,
        IReadOnlyDictionary<PscbPaymentListRow, RowRestorePlan> plans)
    {
        try
        {
            if (!plans.TryGetValue(row, out var plan))
            {
                // Вердикт разрешил, а плана нет — рассогласование обогащения; строка уходит человеку.
                return (Failed(row, "план восстановления не построен — обновите список"), 0, 0);
            }

            return row.MatchState == PscbPaymentMatchState.RefundMissing
                ? await RestoreRefundRowAsync(row, plan)
                : await RestorePaymentRowAsync(row, plan);
        }
        // Нет админских прав — отказ всего прогона, а не строки: контроллер вернёт его текстом.
        catch (Exception ex) when (ex is not MustBeAdminException)
        {
            logger.LogError(ex, "Строка {OrderId} списка ПСКБ не восстановлена", row.OrderId);
            return (Failed(row, ex.Message), 0, 0);
        }
    }

    private async Task<(PscbRestoreRowResult, int Payments, int Refunds)> RestorePaymentRowAsync(
        PscbPaymentListRow row,
        RowRestorePlan plan)
    {
        var result = await lostPaymentsRestoreService.RestorePaymentAsync(PscbLostPaymentInputBuilder.BuildPayment(row, plan));

        var message = new StringBuilder(PaymentOutcomeText(result.Status));
        if (result.Refund is { } refund)
        {
            _ = message.Append("; ").Append(RefundOutcomeText(refund.Status));
        }

        if (!result.FeeRecalculated)
        {
            _ = message.Append(FeeWarning);
        }

        return (
            new PscbRestoreRowResult { OrderId = row.OrderId, Outcome = ToOutcome(result.Status), Message = message.ToString() },
            result.Status == LostOperationRestoreStatus.Restored ? 1 : 0,
            result.Refund?.Status == LostOperationRestoreStatus.Restored ? 1 : 0);
    }

    private async Task<(PscbRestoreRowResult, int Payments, int Refunds)> RestoreRefundRowAsync(
        PscbPaymentListRow row,
        RowRestorePlan plan)
    {
        var result = await lostPaymentsRestoreService.RestoreRefundAsync(PscbLostPaymentInputBuilder.BuildRefund(row, plan));

        var message = RefundOutcomeText(result.Status) + (result.FeeRecalculated ? "" : FeeWarning);
        return (
            new PscbRestoreRowResult { OrderId = row.OrderId, Outcome = ToOutcome(result.Status), Message = message },
            0,
            result.Status == LostOperationRestoreStatus.Restored ? 1 : 0);
    }

    private static PscbRestoreRowResult Failed(PscbPaymentListRow row, string message)
        => new() { OrderId = row.OrderId, Outcome = PscbRestoreRowOutcome.Failed, Message = message };

    private static PscbRestoreRowOutcome ToOutcome(LostOperationRestoreStatus status) => status switch
    {
        LostOperationRestoreStatus.Restored => PscbRestoreRowOutcome.Restored,
        LostOperationRestoreStatus.AlreadyRestored => PscbRestoreRowOutcome.AlreadySettled,
        _ => PscbRestoreRowOutcome.Failed,
    };

    private static string PaymentOutcomeText(LostOperationRestoreStatus status) => status switch
    {
        LostOperationRestoreStatus.Restored => "платёж восстановлен",
        LostOperationRestoreStatus.AlreadyRestored => "операция уже в базе — повторный прогон ничего не записал",
        LostOperationRestoreStatus.NumberTaken => "номер операции занят другой записью — восстанавливать вручную",
        LostOperationRestoreStatus.NumberNotLost => "номер операции не ниже текущего счётчика — восстанавливать вручную",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static string RefundOutcomeText(LostOperationRestoreStatus status) => status switch
    {
        LostOperationRestoreStatus.Restored => "возврат восстановлен",
        LostOperationRestoreStatus.AlreadyRestored => "возврат уже в базе — повторный прогон ничего не записал",
        LostOperationRestoreStatus.NumberTaken => "возврат не восстановлен: номер операции занят",
        LostOperationRestoreStatus.NumberNotLost => "возврат не восстановлен: номер операции не ниже счётчика",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static string BankFailure(PscbPaymentsListResult list)
    {
        var description = $"Банк не вернул список платежей: {list.ErrorCode} {list.ErrorDescription}".TrimEnd();
        return list.RequestId is { } requestId ? $"{description} (requestId: {requestId})" : description;
    }
}
