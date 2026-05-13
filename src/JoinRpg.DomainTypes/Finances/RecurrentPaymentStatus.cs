namespace JoinRpg.DataModel.Finances;

public enum RecurrentPaymentStatus
{
    /// <summary>
    /// Не удалось оформить подписку
    /// </summary>
    Failed = -10,

    /// <summary>
    /// Подписка только что создана, пользователь еще ничего не сделал
    /// </summary>
    Created = 0,

    /// <summary>
    /// Пользователь подтвердил платеж, происходит инициализация
    /// </summary>
    Initialization = 5,

    /// <summary>
    /// Подписка подтверждена пользователем, банком и активна
    /// </summary>
    Active = 10,

    /// <summary>
    /// Выключено на нашей стороне, платежи не осуществляются, еще не подтверждено со стороны банка
    /// </summary>
    Cancelling = 15,

    /// <summary>
    /// Подписка отменена, в том числе на стороне банка, платежи не осуществляются
    /// </summary>
    Cancelled = 20,

    /// <summary>
    /// Замещено новой подпиской
    /// </summary>
    Superseded = 25,
}
