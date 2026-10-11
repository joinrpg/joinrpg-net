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

    Task CloseProjectAsStale(ProjectIdentification projectId, DateOnly lastActiveDate);

    Task SetCheckInSettings(ProjectIdentification projectId,
        bool checkInProgress,
        bool enableCheckInModule,
        bool modelAllowSecondRoles);

    Task SetPublishSettings(ProjectIdentification projectId, ProjectCloneSettings cloneSettings, bool publishEnabled);
    Task SetContactSettings(ProjectIdentification projectId, ProjectProfileRequirementSettings settings);
    Task SetClaimSettings(ProjectIdentification projectId, ProjectClaimSettings settings);
    Task SetAccommodationSettings(ProjectIdentification projectId, bool enableAccommodation);

    /// <summary>Сменить часовой пояс проекта. Пояс должен иметь IANA-идентификатор.</summary>
    Task SetTimeZone(ProjectIdentification projectId, TimeZoneInfo timeZone);
}
