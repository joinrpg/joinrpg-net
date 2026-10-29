using System.Text;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl;

/// <inheritdoc />
/// <remarks>
/// <para>
/// Не наследуется от <c>DbServiceImplBase</c>: тот помечен устаревшим (ADR014), а здесь нужны
/// ровно четыре зависимости.
/// </para>
/// <para>
/// Порядок внутри одной записи жёсткий: сначала снимок заявки, потом вставка, потом пересчёт взноса
/// и только потом уведомление. Снимок читается из базы, и суммы новой операции в нём нет — её
/// добавляем сами, как делает боевое проведение. Уведомление ставится в очередь после коммита:
/// откатившаяся запись не должна породить письмо.
/// </para>
/// </remarks>
internal class LostPaymentsRestoreService(
    IUnitOfWork unitOfWork,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    IClaimInfoRepository claimInfoRepository,
    IVirtualUsersService virtualUsersService,
    Lazy<IClaimNotificationService> claimNotificationService,
    ILogger<LostPaymentsRestoreService> logger) : ILostPaymentsRestoreService
{
    /// <summary>
    /// Значок восстановленной записи: требование заказчика, по нему запись отличают от обычной
    /// в обсуждении заявки.
    /// </summary>
    private const string RestoredBadge = "[восстановлено]";

    public async Task<LostPaymentRestoreResult> RestorePaymentAsync(LostPaymentToRestore payment)
    {
        ArgumentNullException.ThrowIfNull(payment);

        var claim = await LoadClaimAsync(payment.ClaimId);
        var snapshot = await LoadSnapshotAsync(claim);

        var insert = await InsertAsync(
            payment.ClaimId,
            FinanceOperationType.Online,
            operationId: payment.OperationId,
            refundedOperationId: null,
            payment.MoneyAmount,
            payment.PaymentTypeId,
            payment.OperationDate,
            PaymentCommentText(payment),
            payment.BankPaymentId,
            bankRefundKey: null);

        var status = ToStatus(insert.Result);
        if (status != LostOperationRestoreStatus.Restored)
        {
            logger.LogWarning(
                "Потерянный платёж {OrderId} заявки {ClaimId} не восстановлен: {Status}. Админ {AdminUserId}",
                payment.OperationId,
                payment.ClaimId,
                status,
                currentUserAccessor.UserId);
            return new LostPaymentRestoreResult(status, insert.OperationId, Refund: null, FeeRecalculated: true);
        }

        // Полный возврат обнуляет вклад платежа во взнос: деньги пришли и ушли.
        var paymentAdded = payment.MoneyAmount;
        LostRefundRestoreResult? refund = null;
        if (payment.Refund is { } lostRefund)
        {
            // Сбой возврата не уносит уже записанный платёж: взнос пересчитывается на полную сумму,
            // письмо о платеже отправляется, а возврат строка отчёта помечает отдельным исходом;
            // повторный прогон восстановит его как RefundMissing.
            try
            {
                refund = await InsertRefundAsync(lostRefund);
                if (refund.Status == LostOperationRestoreStatus.Restored)
                {
                    paymentAdded -= lostRefund.MoneyAmount;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Возврат платежа {OrderId} заявки {ClaimId} не записан, хотя сам платёж восстановлен. Админ {AdminUserId}",
                    payment.OperationId,
                    payment.ClaimId,
                    currentUserAccessor.UserId);
                refund = new LostRefundRestoreResult(
                    LostOperationRestoreStatus.Failed,
                    OperationId: null,
                    FeeRecalculated: true,
                    ex.Message);
            }
        }

        var feeRecalculated = await TrySettleAsync(claim, snapshot, payment.OperationDate, paymentAdded, insert.OperationId);
        LogRestored(payment.ClaimId, payment.OperationId, payment.MoneyAmount, payment.BankPaymentId, isRefund: false);
        await NotifyAsync(claim, payment.MoneyAmount, payment.OperationId, isRefund: false);

        if (refund is { Status: LostOperationRestoreStatus.Restored, OperationId: { } refundOperationId } && payment.Refund is { } notified)
        {
            LogRestored(payment.ClaimId, refundOperationId, -notified.MoneyAmount, notified.BankRefundId, isRefund: true);
            await NotifyAsync(claim, notified.MoneyAmount, refundOperationId, isRefund: true);
        }

        // Взнос пересчитывается один раз на пару платёж+возврат, поэтому признак у возврата тот же.
        return new LostPaymentRestoreResult(
            status,
            insert.OperationId,
            refund is null ? null : refund with { FeeRecalculated = feeRecalculated },
            feeRecalculated);
    }

    public async Task<LostRefundRestoreResult> RestoreRefundAsync(LostRefundToRestore refund)
    {
        ArgumentNullException.ThrowIfNull(refund);

        var claim = await LoadClaimAsync(refund.ClaimId);
        var snapshot = await LoadSnapshotAsync(claim);

        var result = await InsertRefundAsync(refund);
        if (result is not { Status: LostOperationRestoreStatus.Restored, OperationId: { } operationId })
        {
            logger.LogWarning(
                "Возврат платежа {RefundedOperationId} заявки {ClaimId} не восстановлен: {Status}. Админ {AdminUserId}",
                refund.RefundedOperationId,
                refund.ClaimId,
                result.Status,
                currentUserAccessor.UserId);
            return result;
        }

        var feeRecalculated = await TrySettleAsync(claim, snapshot, refund.OperationDate, paymentAdded: -refund.MoneyAmount, operationId);
        LogRestored(refund.ClaimId, operationId, -refund.MoneyAmount, refund.BankRefundId, isRefund: true);
        await NotifyAsync(claim, refund.MoneyAmount, operationId, isRefund: true);

        return result with { FeeRecalculated = feeRecalculated };
    }

    private async Task<LostRefundRestoreResult> InsertRefundAsync(LostRefundToRestore refund)
    {
        var insert = await InsertAsync(
            refund.ClaimId,
            FinanceOperationType.Refund,
            operationId: null,
            refund.RefundedOperationId,
            -refund.MoneyAmount,
            refund.PaymentTypeId,
            refund.OperationDate,
            RefundCommentText(refund),
            refund.BankPaymentId,
            refund.BankRefundId);

        return new LostRefundRestoreResult(ToStatus(insert.Result), insert.OperationId, FeeRecalculated: true);
    }

    /// <summary>
    /// Одна вставка: комментарий, его текст, операция и банковские реквизиты — одним батчем
    /// репозитория, своей транзакцией.
    /// </summary>
    private async Task<RestoredFinanceOperationInsert> InsertAsync(
        ClaimIdentification claimId,
        FinanceOperationType operationType,
        int? operationId,
        int? refundedOperationId,
        int moneyAmount,
        PaymentTypeIdentification paymentTypeId,
        DateTime operationDate,
        string text,
        string bankOperationKey,
        string? bankRefundKey)
    {
        // Время создания и правки записи операции — момент восстановления (единственный след того,
        // когда запись появилась в базе), а время комментария батч берёт из OperationDate — момента
        // денег, чтобы комментарий встал в историю обсуждения заявки на своё место.
        var now = DateTime.UtcNow;
        return await unitOfWork.GetCharacterAggregateWriteRepository().InsertLostPayment(new LostPaymentRow(
            claimId,
            operationType,
            operationId,
            refundedOperationId,
            // Автор — служебный пользователь онлайн-платежей: он же автор уведомления об оплате
            // в боевом пути, поэтому запись и письмо говорят одним голосом. Админ, запустивший
            // восстановление, остаётся в логе.
            virtualUsersService.PaymentsUser.GetId(),
            // Комментарий со стороны игрока: у служебного пользователя нет мастерского доступа,
            // а при IsCommentByPlayer = false его автор попал бы в кандидаты в бывшие мастера
            // (ADR019, §6 — там любой такой комментарий считается следом мастерства).
            IsCommentByPlayer: true,
            moneyAmount,
            paymentTypeId,
            CreatedAt: now,
            ChangedAt: now,
            operationDate,
            text,
            bankOperationKey,
            bankRefundKey));
    }

    /// <summary>
    /// Пересчёт взноса, который не уносит уже созданную операцию: она вставлена своей транзакцией
    /// и в базе есть, поэтому падение сохранения возвращается признаком, а не исключением. Иначе
    /// админ увидел бы «строка не восстановлена» при созданной операции, а повторный прогон
    /// ответил бы «уже восстановлена» — и незафиксированный взнос потерялся бы молча.
    /// </summary>
    private async Task<bool> TrySettleAsync(Claim claim, ClaimInCharacter snapshot, DateTime operationDate, int paymentAdded, int operationId)
    {
        try
        {
            await SettleAsync(claim, snapshot, operationDate, paymentAdded);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Операция {OperationId} заявки {ClaimId} восстановлена, но взнос не пересчитан: сохранение не прошло",
                operationId,
                claim.GetId());
            return false;
        }
    }

    /// <summary>
    /// Фиксирует взнос и сдвигает отметки заявки — то же, что делает боевое проведение платежа.
    /// </summary>
    private async Task SettleAsync(Claim claim, ClaimInCharacter snapshot, DateTime operationDate, int paymentAdded)
    {
        // Дата денег, а не момент восстановления: GetFeeToFix берёт расписание взносов на дату
        // операции, поэтому взнос зафиксируется по цене на день платежа.
        claim.UpdateClaimFeeIfRequired(snapshot, operationDate, paymentAdded);

        var now = DateTime.UtcNow;
        claim.LastUpdateDateTime = now;
        // Отметка о последнем комментарии игрока не должна уезжать в прошлое: комментарий
        // восстановлен задним числом, а заявка изменилась сейчас.
        if (claim.LastPlayerCommentAt < operationDate)
        {
            claim.LastPlayerCommentAt = operationDate;
        }

        await unitOfWork.SaveChangesAsync();
    }

    private async Task NotifyAsync(Claim claim, int money, int operationId, bool isRefund)
    {
        try
        {
            await claimNotificationService.Value.SendNotification(new ClaimOnlinePaymentNotification(
                claim.GetId(),
                Player: claim.Player.ToUserInfoHeader(),
                new NotificationEventTemplate(NotificationText(money, isRefund)),
                new FinanceOperationIdentification(claim.GetId(), operationId)));
        }
        catch (Exception ex)
        {
            // Письмо — не часть восстановления: операция уже записана, и откатывать её из-за
            // очереди уведомлений нельзя. Ровно так же поступает боевое проведение платежа.
            logger.LogError(ex, "Failed to queue the restored payment notification of claim {ClaimId}", claim.ClaimId);
        }
    }

    private void LogRestored(ClaimIdentification claimId, int operationId, int moneyAmount, string bankKey, bool isRefund)
        => logger.LogWarning(
            "Восстановлен {Kind} {OperationId} заявки {ClaimId} на сумму {MoneyAmount} (ключ в банке {BankKey}) админом {AdminUserId}",
            isRefund ? "возврат" : "потерянный платёж",
            operationId,
            claimId,
            moneyAmount,
            bankKey,
            currentUserAccessor.UserId);

    private async Task<Claim> LoadClaimAsync(ClaimIdentification claimId)
    {
        if (!currentUserAccessor.IsAdmin)
        {
            throw new MustBeAdminException();
        }

        // Заблокированный проект восстанавливают инструменты DRP, а не админская страница (ADR023).
        _ = (await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId)).EnsureNotBlocked();

        return await unitOfWork.GetClaimsRepository().GetClaim(claimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, nameof(Claim));
    }

    private async Task<ClaimInCharacter> LoadSnapshotAsync(Claim claim)
        => await claimInfoRepository.GetClaimInCharacterOrDefault(claim.GetId())
            ?? throw new JoinRpgEntityNotFoundException(claim.ClaimId, nameof(Claim));

    private static LostOperationRestoreStatus ToStatus(RestoredCommentInsertResult result) => result switch
    {
        RestoredCommentInsertResult.Inserted => LostOperationRestoreStatus.Restored,
        RestoredCommentInsertResult.AlreadyExists => LostOperationRestoreStatus.AlreadyRestored,
        RestoredCommentInsertResult.TakenElsewhere => LostOperationRestoreStatus.NumberTaken,
        RestoredCommentInsertResult.NotLostId => LostOperationRestoreStatus.NumberNotLost,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
    };

    private static string PaymentCommentText(LostPaymentToRestore payment)
    {
        var text = new StringBuilder($"{RestoredBadge} Онлайн-оплата на сумму {payment.MoneyAmount:F2}₽ подтверждена.");
        if (!string.IsNullOrWhiteSpace(payment.PayerComment))
        {
            _ = text.AppendLine().AppendLine().Append(payment.PayerComment.Trim());
        }

        return text.ToString();
    }

    private static string RefundCommentText(LostRefundToRestore refund)
        => $"{RestoredBadge} Оформлен возврат платежа от {refund.RefundedOperationDate:d} на сумму {refund.MoneyAmount}";

    private static string NotificationText(int money, bool isRefund)
        => isRefund
            ? $"Возврат на сумму {money:F2}₽ вновь отражён в вашей заявке."
            : $"Онлайн-оплата на сумму {money:F2}₽ вновь отражена в вашей заявке.";
}
