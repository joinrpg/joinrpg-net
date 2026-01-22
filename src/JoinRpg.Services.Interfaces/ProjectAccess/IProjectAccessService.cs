namespace JoinRpg.Services.Interfaces.ProjectAccess;

public interface IProjectAccessService
{
    Task GrantAccess(GrantAccessRequest grantAccessRequest);

    Task RemoveAccess(int projectId, int userId, int? newResponsibleMasterId);

    Task ChangeAccess(ChangeAccessRequest changeAccessRequest);
}
