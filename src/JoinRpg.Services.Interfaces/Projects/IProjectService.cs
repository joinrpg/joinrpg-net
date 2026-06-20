namespace JoinRpg.Services.Interfaces.Projects;

public interface IProjectService
{
    Task EditProject(EditProjectRequest request);

    Task CloseProject(ProjectIdentification projectId, bool publishPlot);

    Task CloseProjectAsStale(ProjectIdentification projectId, DateOnly lastActiveDate);

    Task SetCheckInSettings(ProjectIdentification projectId,
        bool checkInProgress,
        bool enableCheckInModule,
        bool modelAllowSecondRoles);

    Task SetPublishSettings(ProjectIdentification projectId, ProjectCloneSettings cloneSettings, bool publishEnabled);
    Task SetContactSettings(ProjectIdentification projectId, ProjectProfileRequirementSettings settings);
    Task SetClaimSettings(ProjectIdentification projectId, ProjectClaimSettings settings);
    Task SetAccommodationSettings(ProjectIdentification projectId, bool enableAccommodation);
}
