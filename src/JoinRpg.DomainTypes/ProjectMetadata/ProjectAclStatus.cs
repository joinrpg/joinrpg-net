namespace JoinRpg.DomainTypes.ProjectMetadata;

/// <summary>
/// Статус мастера в проекте (ADR019). Доступ даёт только <see cref="Active"/>.
/// </summary>
public enum ProjectAclStatus
{
    Active = 0,

    /// <summary>
    /// Мастера сняли с проекта. Запись хранится, чтобы не терять историю, но доступа не даёт.
    /// </summary>
    Removed = 1,
}
