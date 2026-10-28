using JoinRpg.Services.Interfaces;
using JoinRpg.XGameApi.Contract.Pscb;

namespace JoinRpg.WebPortal.Managers.AdminTools;

/// <summary>
/// Преобразование строки списка ПСКБ и её плана восстановления во вход доменного сервиса
/// <c>ILostPaymentsRestoreService</c> (ADR026).
/// </summary>
/// <remarks>
/// <para>
/// Чистая функция без банка и без репозиториев — по паттерну Builders из ADR025: всё, что нужно,
/// уже лежит в обогащённой строке и плане, который построил вердикт восстановления.
/// </para>
/// <para>
/// Вход построен только для строк, которым вердикт разрешил восстановление, поэтому нарушения
/// его допущений (сумма с копейками, несколько возвратов, <c>orderId</c> не число) — не штатная
/// ситуация, а рассогласование между вердиктом и списком. Такое бросает
/// <see cref="ArgumentException"/>: оркестратор показывает отказ строкой отчёта и продолжает
/// остальные.
/// </para>
/// </remarks>
internal static class PscbLostPaymentInputBuilder
{
    /// <summary>
    /// Вход восстановления потерянного платежа. Если банк зафиксировал возврат — он входит
    /// вторым записыванием: <see cref="LostPaymentToRestore.Refund"/>.
    /// </summary>
    public static LostPaymentToRestore BuildPayment(PscbPaymentListRow row, RowRestorePlan plan)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(plan);

        var operationId = OperationId(row);
        var operationDate = row.StatusDate.UtcDateTime;
        return new LostPaymentToRestore(
            plan.ClaimId,
            operationId,
            plan.PaymentTypeId,
            Money(row.Amount, row.OrderId),
            operationDate,
            BankPaymentId(row),
            row.PayerComment,
            // Строка прошла вердикт: возврат у неё либо отсутствует, либо один и на всю сумму.
            row.RefundedByBank
                ? Refund(row, plan, operationId, refundedOperationDate: operationDate)
                : null);
    }

    /// <summary>
    /// Вход восстановления пропущенного возврата: сам платёж в базе есть и проведён.
    /// </summary>
    public static LostRefundToRestore BuildRefund(PscbPaymentListRow row, RowRestorePlan plan)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(plan);

        return Refund(
            row,
            plan,
            refundedOperationId: OperationId(row),
            refundedOperationDate: plan.RefundedOperationDate
                ?? throw new ArgumentException(
                    $"В плане возврата платежа {row.OrderId} нет даты исходной операции",
                    nameof(plan)));
    }

    private static LostRefundToRestore Refund(
        PscbPaymentListRow row,
        RowRestorePlan plan,
        int refundedOperationId,
        DateTime refundedOperationDate)
    {
        if (row.Refunds.Count != 1)
        {
            throw new ArgumentException(
                $"У платежа {row.OrderId} нет ровно одного возврата в данных банка",
                nameof(row));
        }

        var refund = row.Refunds.First();

        return new LostRefundToRestore(
            plan.ClaimId,
            refundedOperationId,
            plan.PaymentTypeId,
            Money(refund.Amount, row.OrderId),
            row.StatusDate.UtcDateTime,
            refundedOperationDate,
            BankPaymentId(row),
            refund.RefundId is { Length: > 0 } refundId
                ? refundId
                : throw new ArgumentException($"Банк не прислал номер возврата платежа {row.OrderId}", nameof(row)));
    }

    private static int OperationId(PscbPaymentListRow row)
        => PscbPaymentViewModelBuilder.OperationIdFromOrderId(row.OrderId)
            ?? throw new ArgumentException($"orderId {row.OrderId} не является номером операции", nameof(row));

    /// <summary>
    /// Сумма в целых рублях: <c>FinanceOperation.MoneyAmount</c> — <c>int</c>, а округление
    /// исказило бы деньги. Вердикт такие строки отсеивает, поэтому здесь копейки — ошибка входа.
    /// </summary>
    private static int Money(decimal amount, string? orderId)
    {
        if (amount <= 0 || amount != decimal.Truncate(amount) || amount > int.MaxValue)
        {
            throw new ArgumentException($"Сумма {amount} платежа {orderId} не является целым положительным числом рублей", nameof(amount));
        }

        return decimal.ToInt32(amount);
    }

    private static string BankPaymentId(PscbPaymentListRow row)
        => !string.IsNullOrWhiteSpace(row.BankPaymentId)
            ? row.BankPaymentId
            : throw new ArgumentException($"Банк не прислал номер платежа для orderId {row.OrderId}", nameof(row));
}
