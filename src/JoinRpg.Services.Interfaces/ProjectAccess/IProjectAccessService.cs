namespace JoinRpg.Services.Interfaces.ProjectAccess;

public interface IProjectAccessService
{
    Task GrantAccess(GrantAccessRequest grantAccessRequest);

    Task RemoveAccess(ProjectIdentification projectId, UserIdentification userId, UserIdentification? newResponsibleMasterId);

    Task ChangeAccess(ChangeAccessRequest changeAccessRequest);

    Task GrantFullAccess(ProjectIdentification projectId);

    Task ChangeMasterProfile(ChangeMasterProfileRequest request);

    /// <summary>
    /// Переставить мастера в списке сразу после <paramref name="afterUserId"/> (null — в начало), ADR019, §5.
    /// </summary>
    Task MoveMasterAfter(ProjectIdentification projectId, UserIdentification userId, UserIdentification? afterUserId);
}
