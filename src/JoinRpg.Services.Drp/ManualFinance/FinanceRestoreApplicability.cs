using JoinRpg.DomainTypes.Characters.Claims.Finances;

namespace JoinRpg.Services.Drp.ManualFinance;

/// <summary>Почему строка плана не передаётся в доменный сервис. Подписывает тот, кто показывает, — консоль или страница.</summary>
public enum FinanceRestoreSkipReason
{
    /// <summary>Уже есть в базе (<see cref="FinanceRestoreStatus.AlreadyExists"/>) — делать нечего.</summary>
    AlreadyExists,

    /// <summary>Id в базе занят другим (<see cref="FinanceRestoreStatus.Conflict"/>).</summary>
    Conflict,

    /// <summary>Сумма некорректна (<see cref="FinanceRestoreStatus.InvalidMoney"/>) или её нет.</summary>
    InvalidMoney,

    /// <summary>Возврат не подтверждён мастером: доменный сервис неподтверждённый возврат отвергает — разобрать вручную.</summary>
    RefundNotApproved,

    /// <summary>Для операции не выбран тип оплаты.</summary>
    NoPaymentType,

    /// <summary>
    /// Решение по восстанавливаемой операции, которая в этом прогоне не передаётся
    /// (<see cref="FinanceModerationToRestore.TargetOperationKey"/>), — вставится вместе с ней.
    /// </summary>
    ParentNotRestoredInThisRun,

    /// <summary>Решение по операции, которая не вставляется (<see cref="FinanceModerationLink.NotRestoredOperation"/>), — разобрать вручную.</summary>
    ParentNotInserted,

    /// <summary>Операция решения не определена — разобрать вручную.</summary>
    ParentUnknown,

    /// <summary>
    /// Ключ встречается в строке плана больше одного раза: какая строка какая — не понять, не передаётся ни одна —
    /// разобрать вручную.
    /// </summary>
    AmbiguousKey,
}

/// <summary>Какие строки плана вообще можно передать в доменный сервис восстановления.</summary>
public static class FinanceRestoreApplicability
{
    /// <summary>
    /// Операция применима, если её надо вставить (<see cref="FinanceRestoreStatus.Restore"/>), сумма есть и это не
    /// возврат в состоянии, отличном от <see cref="FinanceOperationState.Approved"/>: доменный возврат бывает
    /// только подтверждённым, заявленный игроком возврат разбирают вручную.
    /// </summary>
    /// <returns>null — применима; иначе причина.</returns>
    public static FinanceRestoreSkipReason? NotApplicableReason(this FinanceOperationToRestore operation)
    {
        if (StatusReason(operation.Status) is { } reason)
        {
            return reason;
        }
        if (operation.Money is null)
        {
            return FinanceRestoreSkipReason.InvalidMoney;
        }
        if (operation.Type == FinanceOperationType.Refund && operation.State != FinanceOperationState.Approved)
        {
            return FinanceRestoreSkipReason.RefundNotApproved;
        }
        return null;
    }

    /// <summary>
    /// Применимость модерации по её собственному статусу. Связь с операцией (передаётся ли она в этом же
    /// прогоне) проверяется при сборке входа.
    /// </summary>
    /// <returns>null — применима; иначе причина.</returns>
    public static FinanceRestoreSkipReason? NotApplicableReason(this FinanceModerationToRestore moderation)
        => StatusReason(moderation.Status);

    private static FinanceRestoreSkipReason? StatusReason(FinanceRestoreStatus status) => status switch
    {
        FinanceRestoreStatus.Restore => null,
        FinanceRestoreStatus.AlreadyExists => FinanceRestoreSkipReason.AlreadyExists,
        FinanceRestoreStatus.Conflict => FinanceRestoreSkipReason.Conflict,
        FinanceRestoreStatus.InvalidMoney => FinanceRestoreSkipReason.InvalidMoney,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
