namespace JoinRpg.Services.Interfaces.ProjectAccess;

public interface IProjectAccessService
{
    Task GrantAccess(GrantAccessRequest grantAccessRequest);

    Task RemoveAccess(ProjectIdentification projectId, UserIdentification userId, UserIdentification? newResponsibleMasterId);

    Task ChangeAccess(ChangeAccessRequest changeAccessRequest);

    Task GrantFullAccess(ProjectIdentification projectId);
}
