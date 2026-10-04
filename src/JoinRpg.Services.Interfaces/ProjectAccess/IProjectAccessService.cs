namespace JoinRpg.Services.Interfaces.ProjectAccess;

public interface IProjectAccessService
{
    Task GrantAccess(GrantAccessRequest grantAccessRequest);

    Task RemoveAccess(ProjectIdentification projectId, UserIdentification userId, UserIdentification? newResponsibleMasterId);

    Task ChangeAccess(ChangeAccessRequest changeAccessRequest);

    Task GrantFullAccess(ProjectIdentification projectId);

    /// <summary>
    /// Записать как бывших мастеров (статус Removed, без прав) тех, у кого в проекте нет записи ACL.
    /// Пользователи, у которых запись уже есть в любом статусе, пропускаются. Бэкфилл ADR019, §6.
    /// </summary>
    Task RegisterFormerMasters(ProjectIdentification projectId, IReadOnlyCollection<UserIdentification> userIds);
}
