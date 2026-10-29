using System.Text;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.AdminTools.PscbPayments;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Восстановление потерянных платежей ПСКБ из списка банка (ADR027): заново скачивает период,
/// находит в нём одну строку по <c>orderId</c> и, если сверка вынесла ей вердикт «можно
/// восстановить автоматически», пишет её доменным сервисом <see cref="ILostPaymentsRestoreService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Один запрос — одна строка — своя область DI: упавшая строка оставляет следующей чистый
/// <c>DbContext</c>, а общий трекер не несёт грязную заявку, отравляющую каждый последующий
/// <c>SaveChangesAsync</c> — причина именно такой формы. Прогон по всем строкам собирает остров
/// (<c>PscbPaymentsPanel</c>), как <c>FinanceRestorePanel.ApplySelected</c>.
/// </para>
/// <para>
/// Период скачивается заново на каждую строку — сознательно: надёжность важнее скорости, а суммы
/// и даты сервер берёт только у банка, никогда из клиентского JSON.
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
    private const string FeeWarning = "; взнос заявки не пересчитан — операция записана, зафиксируйте взнос вручную";

    /// <inheritdoc />
    public Task<PscbRestoreRowResult> RestoreRow(PscbRestoreRowRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return RestoreRow(request.Query, request.OrderId);
    }

    /// <summary>
    /// Восстанавливает одну строку скачанного периода: отказ всего прогона (банк не вернул список,
    /// сверка с базой не удалась) бросает исключением, а исход строки возвращает результатом.
    /// Отсутствие админских прав — тоже отказ прогона: <c>MustBeAdminException</c> пробрасывается
    /// контроллеру, а не превращается в исход строки.
    /// </summary>
    public async Task<PscbRestoreRowResult> RestoreRow(PscbPaymentsQuery query, string orderId)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentOutOfRangeException(nameof(orderId), "Не указан orderId строки");
        }

        var (list, plans) = await paymentsViewService.GetPaymentsWithRestorePlan(query);
        if (!list.IsSuccess)
        {
            throw new JoinValidationException(BankFailure(list));
        }

        if (list.ReconciliationFailed)
        {
            throw new JoinValidationException("Сверка списка с базой не удалась — восстановление невозможно. Обновите список и попробуйте ещё раз.");
        }

        var row = list.Payments.FirstOrDefault(payment => string.Equals(payment.OrderId, orderId, StringComparison.Ordinal));
        PscbRestoreRowResult result;
        if (row is null)
        {
            result = Failed(orderId, "строка не найдена в списке банка — обновите список");
        }
        else
        {
            result = await RestoreRowAsync(row, plans);
        }

        // Одна строка на запрос — аудит по строке: кто, за какой период и чем строка кончилась.
        // По записанным операциям доменный сервис логирует сам.
        logger.LogWarning(
            "Восстановление потерянного платежа ПСКБ, строка {OrderId} за период {DateFrom}–{DateTo} админом {AdminUserId}: {Outcome}",
            orderId,
            query.DateFrom,
            query.DateTo,
            currentUserAccessor.UserId,
            result.Outcome);

        return result;
    }

    private async Task<PscbRestoreRowResult> RestoreRowAsync(
        PscbPaymentListRow row,
        IReadOnlyDictionary<PscbPaymentListRow, RowRestorePlan> plans)
    {
        try
        {
            // Строка перестала быть кандидатом между скачиванием списка и запросом — нормально:
            // прогон идёт по строке, и каждая заново сверяется со свежими данными банка.
            if (row.MatchState is not (PscbPaymentMatchState.Lost or PscbPaymentMatchState.RefundMissing)
                || row.RestoreBlockReason is not null)
            {
                return NotACandidate(row);
            }

            if (!plans.TryGetValue(row, out var plan))
            {
                // Вердикт разрешил, а плана нет — рассогласование обогащения; строка уходит человеку.
                return Failed(row, "план восстановления не построен — обновите список");
            }

            return row.MatchState == PscbPaymentMatchState.RefundMissing
                ? await RestoreRefundRowAsync(row, plan)
                : await RestorePaymentRowAsync(row, plan);
        }
        // Нет админских прав — отказ всего прогона, а не строки: контроллер вернёт его текстом.
        catch (Exception ex) when (ex is not MustBeAdminException)
        {
            logger.LogError(ex, "Строка {OrderId} списка ПСКБ не восстановлена", row.OrderId);
            return Failed(row, ex.Message);
        }
    }

    /// <summary>Исход строки, которая к моменту запроса не подлежит автоматическому восстановлению.</summary>
    private static PscbRestoreRowResult NotACandidate(PscbPaymentListRow row)
    {
        if (row.MatchState == PscbPaymentMatchState.Settled)
        {
            return new PscbRestoreRowResult
            {
                OrderId = row.OrderId,
                Outcome = PscbRestoreRowOutcome.AlreadySettled,
                Message = PaymentOutcomeText(LostOperationRestoreStatus.AlreadyRestored),
            };
        }

        if (row.RestoreBlockReason is { } reason)
        {
            return Failed(row, reason);
        }

        return Failed(row, $"строка не подлежит автоматическому восстановлению ({row.MatchStateText ?? "сверка не определила исход"})");
    }

    private async Task<PscbRestoreRowResult> RestorePaymentRowAsync(
        PscbPaymentListRow row,
        RowRestorePlan plan)
    {
        var result = await lostPaymentsRestoreService.RestorePaymentAsync(PscbLostPaymentInputBuilder.BuildPayment(row, plan));

        var message = new StringBuilder(PaymentOutcomeText(result.Status));
        if (result.Refund is { } refund)
        {
            _ = message.Append("; ").Append(RefundOutcomeText(refund.Status));
            // Сбой возврата не меняет исход строки — её диктует платёж, а сообщение объясняет,
            // что случилось с возвратом и почему его придётся доделать.
            if (refund.FailureMessage is { } failure)
            {
                _ = message.Append(": ").Append(failure);
            }
        }

        if (!result.FeeRecalculated)
        {
            _ = message.Append(FeeWarning);
        }

        return new PscbRestoreRowResult
        {
            OrderId = row.OrderId,
            Outcome = ToOutcome(result.Status),
            Message = message.ToString(),
            RestoredPaymentsCount = result.Status is LostOperationRestoreStatus.Restored or LostOperationRestoreStatus.Attached ? 1 : 0,
            RestoredRefundsCount = result.Refund?.Status == LostOperationRestoreStatus.Restored ? 1 : 0,
        };
    }

    private async Task<PscbRestoreRowResult> RestoreRefundRowAsync(
        PscbPaymentListRow row,
        RowRestorePlan plan)
    {
        var result = await lostPaymentsRestoreService.RestoreRefundAsync(PscbLostPaymentInputBuilder.BuildRefund(row, plan));

        var message = RefundOutcomeText(result.Status) + (result.FeeRecalculated ? "" : FeeWarning);
        return new PscbRestoreRowResult
        {
            OrderId = row.OrderId,
            Outcome = ToOutcome(result.Status),
            Message = message,
            RestoredRefundsCount = result.Status == LostOperationRestoreStatus.Restored ? 1 : 0,
        };
    }

    private static PscbRestoreRowResult Failed(PscbPaymentListRow row, string message) => Failed(row.OrderId, message);

    private static PscbRestoreRowResult Failed(string? orderId, string message)
        => new() { OrderId = orderId, Outcome = PscbRestoreRowOutcome.Failed, Message = message };

    private static PscbRestoreRowOutcome ToOutcome(LostOperationRestoreStatus status) => status switch
    {
        LostOperationRestoreStatus.Restored => PscbRestoreRowOutcome.Restored,
        // Операция дописана к существующему комментарию заявки — запись в базе есть, исход успешный.
        LostOperationRestoreStatus.Attached => PscbRestoreRowOutcome.Restored,
        LostOperationRestoreStatus.AlreadyRestored => PscbRestoreRowOutcome.AlreadySettled,
        LostOperationRestoreStatus.Failed => PscbRestoreRowOutcome.Failed,
        _ => PscbRestoreRowOutcome.Failed,
    };

    private static string PaymentOutcomeText(LostOperationRestoreStatus status) => status switch
    {
        LostOperationRestoreStatus.Restored => "платёж восстановлен",
        LostOperationRestoreStatus.Attached => "платёж записан в уже существующий комментарий заявки",
        LostOperationRestoreStatus.AlreadyRestored => "операция уже в базе — повторный прогон ничего не записал",
        LostOperationRestoreStatus.NumberTaken => "номер операции занят другой записью — восстанавливать вручную",
        LostOperationRestoreStatus.NumberNotLost => "номер операции не ниже текущего счётчика — восстанавливать вручную",
        // Платёж с таким исходом не возвращается: сбой вставки платежа пробрасывается исключением,
        // а сбой возврата строку не проваливает. Ветка — ради полноты перебора.
        LostOperationRestoreStatus.Failed => "платёж не записан",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static string RefundOutcomeText(LostOperationRestoreStatus status) => status switch
    {
        LostOperationRestoreStatus.Restored => "возврат восстановлен",
        // Возврат всегда вставляется новой записью, поэтому Attached у него не случается;
        // ветка — ради полноты перебора.
        LostOperationRestoreStatus.Attached => "возврат дописан к существующему комментарию",
        LostOperationRestoreStatus.AlreadyRestored => "возврат уже в базе — повторный прогон ничего не записал",
        LostOperationRestoreStatus.NumberTaken => "возврат не восстановлен: номер операции занят",
        LostOperationRestoreStatus.NumberNotLost => "возврат не восстановлен: номер операции не ниже счётчика",
        LostOperationRestoreStatus.Failed => "возврат не записан — восстановите вручную",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    private static string BankFailure(PscbPaymentsListResult list)
    {
        var description = $"Банк не вернул список платежей: {list.ErrorCode} {list.ErrorDescription}".TrimEnd();
        return list.RequestId is { } requestId ? $"{description} (requestId: {requestId})" : description;
    }
}
