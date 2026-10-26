namespace JoinRpg.Services.Interfaces.Projects;

public interface IProjectService
{
    Task EditProject(EditProjectRequest request);

    /// <summary>Закрыть проект (в архив). Заблокированный тоже можно — это конец восстановления (ADR023).</summary>
    Task CloseProject(ProjectIdentification projectId, bool publishPlot);

    /// <summary>
    /// Снять блокировку восстановления (ADR023): проект возвращается в состояние до блокировки.
    /// Для незаблокированного проекта ничего не делает.
    /// </summary>
    Task UnblockProject(ProjectIdentification projectId);

    /// <summary>
    /// Оболочка проекта, потерянного вместе с основной БД (DRP): проект, корневая группа и права
    /// текущего пользователя как создателя, сразу в статусе «Заблокирован» (ADR023). Без шаблона
    /// типа проекта и без уведомлений — содержимое восстанавливается отдельно.
    /// Прежний id проекта обеспечивает вызывающий, подготовив счётчик IDENTITY.
    /// </summary>
    Task<ProjectIdentification> AddRestoredProjectShell(ProjectName projectName);

    Task CloseProjectAsStale(ProjectIdentification projectId, DateOnly lastActiveDate);

    Task SetCheckInSettings(ProjectIdentification projectId,
        bool checkInProgress,
        bool enableCheckInModule,
        bool modelAllowSecondRoles);

    Task SetPublishSettings(ProjectIdentification projectId, ProjectCloneSettings cloneSettings, bool publishEnabled);
    Task SetContactSettings(ProjectIdentification projectId, ProjectProfileRequirementSettings settings);
    /// <param name="claimApplyRules">Правила подачи заявок (Markdown); <c>null</c> — не менять.</param>
    Task SetClaimSettings(ProjectIdentification projectId, ProjectClaimSettings settings, string? claimApplyRules = null);
    Task SetAccommodationSettings(ProjectIdentification projectId, bool enableAccommodation);

    /// <summary>Сменить часовой пояс проекта. Пояс должен иметь IANA-идентификатор.</summary>
    Task SetTimeZone(ProjectIdentification projectId, TimeZoneInfo timeZone);
}
