namespace JoinRpg.Services.Impl;

/// <summary>
/// Сторож реентерабельности: не даёт войти в операцию, пока предыдущая операция того же экземпляра
/// не завершилась.
/// </summary>
/// <remarks>
/// <para>
/// Props-сервисы (ADR009, ADR014) транзиентные, и операции, которые честно идут одна ЗА другой,
/// резолвят новый экземпляр — им сторож не мешает. Ловит он другое: вызов операции из середины
/// другой операции того же экземпляра. Так делал автоприём заявки, и это тихо ломало инварианты —
/// время операции у внешнего контекста уже зафиксировано, а текущий пользователь у вложенной может
/// оказаться другим.
/// </para>
/// <para>
/// Не потокобезопасен намеренно: он ловит вложенность в пределах одного логического потока
/// выполнения, а не конкурентный доступ. Экземпляр сервиса и так не предназначен для параллельного
/// использования — у него один <c>DbContext</c>.
/// </para>
/// </remarks>
internal sealed class NonReentrantOperation(string serviceName)
{
    private bool inOperation;

    /// <summary>
    /// Входит в операцию. Результат обязан быть освобождён — <c>using</c> в теле операции, чтобы
    /// сторож опускался и при исключении.
    /// </summary>
    /// <exception cref="InvalidOperationException">Операция уже идёт на этом экземпляре.</exception>
    public IDisposable Enter(string operationName)
    {
        if (inOperation)
        {
            throw new InvalidOperationException(
                $"Вложенная операция «{operationName}» на том же экземпляре {serviceName} запрещена. "
                + "Операция, которой нужна другая операция, обязана вызвать её ПОСЛЕ своего "
                + "завершения и через собственный экземпляр сервиса.");
        }

        inOperation = true;
        return new Scope(this);
    }

    private sealed class Scope(NonReentrantOperation owner) : IDisposable
    {
        public void Dispose() => owner.inOperation = false;
    }
}
