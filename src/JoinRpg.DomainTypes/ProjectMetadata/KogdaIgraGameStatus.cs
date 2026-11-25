namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Код статуса игры на КогдаИгре (поле <c>status</c> в их API).
/// В БД хранится сырой int, поэтому неизвестные КогдаИгре коды приводятся к этому enum как есть.
/// </summary>
public enum KogdaIgraGameStatus
{
    /// <summary>Обычная игра: анонсирована или идёт набор.</summary>
    Ok = 0,

    /// <summary>Игра прошла.</summary>
    Passed = 1,

    /// <summary>Сама КогдаИгра не знает статус игры (их status_name для этого кода — «???»).</summary>
    Unknown = 2,

    /// <summary>Игра отложена.</summary>
    Postponed = 3,

    /// <summary>Дата игры уточняется.</summary>
    DateUncertain = 4,

    /// <summary>Игра отменена.</summary>
    Cancelled = 5,
}
