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
    /// <returns>Действующие мастера в новом порядке — чтобы интерактивный список переставил строки без перезагрузки.</returns>
    Task<IReadOnlyList<UserIdentification>> MoveMasterAfter(ProjectIdentification projectId, UserIdentification userId, UserIdentification? afterUserId);

    /// <summary>
    /// Записать как бывших мастеров (статус Removed, без прав) тех, у кого в проекте нет записи ACL.
    /// Пользователи, у которых запись уже есть в любом статусе, пропускаются. Бэкфилл ADR019, §6.
    /// </summary>
    Task RegisterFormerMasters(ProjectIdentification projectId, IReadOnlyCollection<UserIdentification> userIds);
}
