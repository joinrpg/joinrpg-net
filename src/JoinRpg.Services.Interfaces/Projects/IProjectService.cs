using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Projects;

public interface IProjectService
{
    Task EditProject(EditProjectRequest request);

    Task<CharacterGroupIdentification> AddCharacterGroup(ProjectIdentification projectId,
        string name,
        bool isPublic,
        IReadOnlyCollection<int> parentCharacterGroupIds,
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

    Task SetCheckInOptions(int projectId,
        bool checkInProgress,
        bool enableCheckInModule,
        bool modelAllowSecondRoles);

    Task GrantAccessAsAdmin(int projectId);
}
