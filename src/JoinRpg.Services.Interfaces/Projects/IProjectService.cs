namespace JoinRpg.Services.Interfaces.Projects;

public interface IProjectService
{
    Task EditProject(EditProjectRequest request);

    Task AddCharacterGroup(int projectId,
        string name,
        bool isPublic,
        IReadOnlyCollection<int> parentCharacterGroupIds,
        string description,
        bool haveDirectSlotsForSave,
        int directSlotsForSave);

    Task EditCharacterGroup(int projectId,
        int currentUserId,
        int characterGroupId,
        string name,
        bool isPublic,
        IReadOnlyCollection<int> parentCharacterGroupIds,
        string description,
        bool haveDirectSlots,
        int directSlots);

    Task DeleteCharacterGroup(int projectId, int characterGroupId);

    Task GrantAccess(GrantAccessRequest grantAccessRequest);

    Task RemoveAccess(int projectId, int userId, int? newResponsibleMasterId);

    Task ChangeAccess(ChangeAccessRequest changeAccessRequest);

    Task MoveCharacterGroup(int currentUserId,
        int projectId,
        int charactergroupId,
        int parentCharacterGroupId,
        short direction);

    Task CloseProject(int projectId, int currentUserId, bool publishPlot);

    Task SetCheckInOptions(int projectId,
        bool checkInProgress,
        bool enableCheckInModule,
        bool modelAllowSecondRoles);

    Task GrantAccessAsAdmin(int projectId);
}
