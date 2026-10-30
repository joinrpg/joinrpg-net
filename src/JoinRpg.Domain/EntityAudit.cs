namespace JoinRpg.Domain;

/// <summary>
/// Чистое ядро проставления аудита (created/updated). Время и пользователь передаются явно: единое
/// на операцию время подставляет контекст операции (ADR014, §7).
/// </summary>
public static class EntityAudit
{
    public static void MarkCreated(ICreatedUpdatedTrackedForEntity entity, DateTime now, int userId)
    {
        entity.UpdatedAt = entity.CreatedAt = now;
        entity.UpdatedById = entity.CreatedById = userId;
    }

    public static void MarkChanged(ICreatedUpdatedTrackedForEntity entity, DateTime now, int userId)
    {
        entity.UpdatedAt = now;
        entity.UpdatedById = userId;
    }
}
