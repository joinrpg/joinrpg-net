namespace JoinRpg.Helpers;

/// <summary>
/// Приведение делегатов без результата к делегатам с результатом.
/// </summary>
/// <remarks>
/// Нужно там, где ядро операции одно и всегда возвращает результат, а наружу выставлены и
/// перегрузки без результата: чтобы не дублировать ядро, действие оборачивается в функцию с
/// фиктивным результатом.
/// </remarks>
public static class DelegateHelpers
{
    /// <summary>Выполняет действие и возвращает <c>true</c>.</summary>
    public static Func<TArg, bool> AsAlwaysTrueFunc<TArg>(this Action<TArg> action)
        => arg =>
        {
            action(arg);
            return true;
        };

    /// <summary>Выполняет действие и возвращает уже завершённый <c>true</c>.</summary>
    public static Func<TArg, Task<bool>> AsAlwaysTrueAsyncFunc<TArg>(this Action<TArg> action)
        => arg =>
        {
            action(arg);
            return Task.FromResult(true);
        };

    /// <summary>Дожидается асинхронного действия и возвращает <c>true</c>.</summary>
    public static Func<TArg, Task<bool>> AsAlwaysTrueAsyncFunc<TArg>(this Func<TArg, Task> action)
        => async arg =>
        {
            await action(arg);
            return true;
        };
}
