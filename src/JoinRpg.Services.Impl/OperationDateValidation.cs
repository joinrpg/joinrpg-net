using JoinRpg.Domain;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Запрещает операции, датированные будущим.
/// </summary>
/// <remarks>
/// Единственное, что пережило удаление <c>ClaimImplBase</c> (ADR014). Отдельный класс, а не метод
/// контекста мутации, потому что проверка нужна двум разным агрегатам: мутации заявки
/// (<see cref="Claims.ClaimMutationContext.CheckOperationDate"/>) и переводу денег между мастерами,
/// у которого заявки нет вовсе.
/// </remarks>
internal static class OperationDateValidation
{
    /// <summary>
    /// Время операции передаётся явно: мигрированные пути берут его из контекста, а не из поля
    /// сервиса, зафиксированного в конструкторе (ADR014 §7).
    /// </summary>
    /// <remarks>
    /// TODO[#4898]: проверять надо <c>DateOnly</c> относительно «сегодня» в таймзоне проекта, а не
    /// сравнивать моменты <c>DateTime</c>: в вечерних таймзонах законная локальная дата выглядит
    /// будущим, и костыль с +1 днём это лишь маскирует.
    /// </remarks>
    public static void CheckOperationDate(DateTime operationDate, DateTime now)
    {
        if (operationDate > now.AddDays(1)
        ) //TODO[UTC]: if everyone properly uses UTC, we don't have to do +1
        {
            throw new CannotPerformOperationInFuture();
        }
    }
}
