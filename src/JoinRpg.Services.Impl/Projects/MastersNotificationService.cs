using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces.Notifications;

namespace JoinRpg.Services.Impl.Projects;

/// <summary>
/// Рассылки мастерам проекта (состав получателей — из <see cref="ProjectInfo.Masters"/>, там уже только активные мастера).
/// Шаблон писем единый: «Добрый день, %recepient.name%!» + тело.
/// </summary>
internal class MastersNotificationService(
    INotificationService notificationService,
    IUserRepository userRepository
    )
{
    public Task NotifyAllMasters(ProjectInfo project, string subject, string body, UserIdentification initiator)
        => Send(project, body, subject, initiator, [.. project.Masters.Select(master => new NotificationRecepient(master))]);

    public Task NotifyMastersWithPermission(ProjectInfo project, Permission permission, string subject, string body, UserIdentification initiator)
        => Send(project, body, subject, initiator,
            [.. project.Masters.Where(master => master.Permissions.Contains(permission)).Select(master => new NotificationRecepient(master))]);

    /// <summary>
    /// Уведомить всех мастеров проекта и всех админов сайта.
    /// Админ, который сам мастер проекта, получает одно уведомление — как мастер.
    /// </summary>
    public async Task NotifyMastersAndAdmins(ProjectInfo project, string subject, string body, UserIdentification initiator)
    {
        var admins = await userRepository.GetAdminUserInfoHeaders();

        var masters = project.Masters.Select(master => new NotificationRecepient(master)).ToArray();
        var masterIds = masters.Select(m => m.UserId).ToHashSet();
        var otherAdmins = admins.Where(a => !masterIds.Contains(a.UserId)).Select(a => NotificationRecepient.Admin(a));

        await Send(project, body, subject, initiator, [.. masters, .. otherAdmins]);
    }

    private async Task Send(ProjectInfo project, string body, string subject, UserIdentification initiator, NotificationRecepient[] recipients)
    {
        var notification = new NotificationEvent(NotificationClass.MasterProject, project.ProjectId, subject,
            new NotificationEventTemplate("Добрый день, %recepient.name%!\n\n" + body), recipients, initiator);
        await notificationService.QueueNotification(notification);
    }
}
