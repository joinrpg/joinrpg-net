using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Services.Interfaces.Projects;

public interface IProjectService
{
    Task EditProject(EditProjectRequest request);

    Task<CharacterGroupIdentification> AddCharacterGroup(ProjectIdentification projectId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description);

    Task EditCharacterGroup(CharacterGroupIdentification characterGroupId,
        string name,
        bool isPublic,
        IReadOnlyCollection<CharacterGroupIdentification> parentCharacterGroupIds,
        string description);

    Task DeleteCharacterGroup(int projectId, int characterGroupId);

    Task MoveCharacterGroup(int currentUserId,
        int projectId,
        int charactergroupId,
        int parentCharacterGroupId,
        short direction);

    Task CloseProject(ProjectIdentification projectId, bool publishPlot);

    Task CloseProjectAsStale(ProjectIdentification projectId, DateOnly lastActiveDate);

    Task SetCheckInSettings(ProjectIdentification projectId,
        bool checkInProgress,
        bool enableCheckInModule,
        bool modelAllowSecondRoles);

    Task GrantAccessAsAdmin(int projectId);
    Task SetPublishSettings(ProjectIdentification projectId, ProjectCloneSettings cloneSettings, bool publishEnabled);
    Task SetContactSettings(ProjectIdentification projectId, ProjectProfileRequirementSettings settings);
    Task SetClaimSettings(ProjectIdentification projectId, ProjectClaimSettings settings);
    Task SetAccommodationSettings(ProjectIdentification projectId, bool enableAccommodation);
}
