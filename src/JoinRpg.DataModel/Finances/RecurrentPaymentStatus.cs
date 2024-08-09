namespace JoinRpg.DataModel.Finances;

public enum RecurrentPaymentStatus
{
    /// <summary>
    /// Только что создано, еще не прокинуто в банк
    /// </summary>
    Created = 1,
    /// <summary>
    /// Прокинуто в банк, но еще не подтверджено пользователем
    /// </summary>
    PendingUserConfirmation = 2,
    /// <summary>
    /// Подтерждено пользователем и активно
    /// </summary>
    Active = 3,
    /// <summary>
    /// Выключено на нашей стороне, но еще не прокинуто в банк
    /// </summary>
    PendingCancellation = 4,
    /// <summary>
    /// Отменено
    /// </summary>
    Cancelled = 5,
    /// <summary>
    /// Замещено операц
    /// </summary>
    Superseded = 6,
}
