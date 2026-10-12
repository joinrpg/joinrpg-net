namespace JoinRpg.Data.Interfaces;

/// <summary>
/// Чем кончилась вставка «оболочки» потерянной сущности с прежним идентификатором (DRP, #5529).
/// </summary>
public enum RestoredShellInsertResult
{
    /// <summary>Строка вставлена с заданным идентификатором.</summary>
    Inserted = 0,

    /// <summary>Строка с таким идентификатором уже есть в этом проекте — ничего не сделано.</summary>
    AlreadyExists = 1,

    /// <summary>Идентификатор занят строкой другого проекта — ничего не сделано.</summary>
    TakenInOtherProject = 2,

    /// <summary>
    /// Идентификатор не ниже текущего значения счётчика IDENTITY (или счётчик не прочитан): это не
    /// потерянная строка — потерянные лежат строго ниже счётчика. Ничего не сделано.
    /// </summary>
    NotLostId = 3,
}
